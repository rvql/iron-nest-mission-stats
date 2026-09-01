[CmdletBinding()]
param(
    [string]$GameDirectory = $env:IRON_NEST_GAME_DIR
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GameDirectory)) {
    $programFilesX86 = ${env:ProgramFiles(x86)}
    if ([string]::IsNullOrWhiteSpace($programFilesX86)) {
        throw 'Pass -GameDirectory or set the IRON_NEST_GAME_DIR environment variable.'
    }
    $GameDirectory = Join-Path $programFilesX86 'Steam\steamapps\common\Iron Nest Heavy Turret Simulator'
}
if (-not (Test-Path -LiteralPath $GameDirectory)) {
    throw 'Game directory not found. Pass -GameDirectory or set IRON_NEST_GAME_DIR.'
}
$melonLoader = Join-Path $GameDirectory 'MelonLoader'
$melonRuntime = Join-Path $melonLoader 'net6\MelonLoader.dll'
$interopDirectory = Join-Path $melonLoader 'Il2CppAssemblies'
$net6Root = 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App'

function Get-ManagedReferences([string]$directory) {
    $result = @()
    Get-ChildItem -LiteralPath $directory -Filter *.dll -File | ForEach-Object {
        try {
            [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
            $result += $_.FullName
        } catch { }
    }
    return $result
}

$sdkEntries = @(& dotnet --list-sdks | ForEach-Object {
    if ($_ -match '^(\d+\.\d+\.\d+)\s+\[(.+)\]$') {
        [pscustomobject]@{ Version = [Version]$Matches[1]; Root = $Matches[2] }
    }
})
$sdk = $sdkEntries | Where-Object { $_.Version.Major -ge 6 } | Sort-Object Version -Descending | Select-Object -First 1
if (-not $sdk) { throw 'A .NET SDK version 6 or newer is required.' }
$csc = Join-Path $sdk.Root ($sdk.Version.ToString() + '\Roslyn\bincore\csc.dll')
if (-not (Test-Path -LiteralPath $csc)) { throw "Roslyn compiler not found: $csc" }

$net6Runtime = Get-ChildItem -LiteralPath $net6Root -Directory | Where-Object { $_.Name -like '6.*' } |
    Sort-Object { [Version]$_.Name } -Descending | Select-Object -First 1
if (-not $net6Runtime) { throw 'The .NET 6 runtime is required for offline reference assemblies.' }
if (-not (Test-Path -LiteralPath $melonRuntime)) { throw "MelonLoader 0.7.x is not installed at: $melonLoader" }
if (-not (Test-Path -LiteralPath (Join-Path $interopDirectory 'UnityEngine.CoreModule.dll'))) {
    throw 'MelonLoader Il2CppAssemblies are missing. Launch the game once with MelonLoader, then build again.'
}

& (Join-Path $root 'verify.ps1')
if ($LASTEXITCODE -ne 0) { throw 'C# architecture verification failed.' }

$output = Join-Path $root 'artifacts\release'
$generated = Join-Path $root 'artifacts\generated'
New-Item -ItemType Directory -Force -Path $output, $generated | Out-Null

$coreTarget = Join-Path $generated 'CoreTargetFramework.cs'
$modTarget = Join-Path $generated 'ModTargetFramework.cs'
'[assembly: System.Runtime.Versioning.TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName = ".NET Standard 2.0")]' |
    Set-Content -LiteralPath $coreTarget -Encoding ASCII
'[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]' |
    Set-Content -LiteralPath $modTarget -Encoding ASCII

$runtimeReferences = @(Get-ManagedReferences $net6Runtime.FullName)
$runtimeArgs = $runtimeReferences | ForEach-Object { '/reference:' + $_ }
$coreSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\IronNestStats.Core') -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object FullName

& dotnet $csc /nologo /noconfig /target:library /langversion:latest /nostdlib+ /deterministic+ /optimize+ `
    /debug:portable ('/out:' + (Join-Path $output 'MissionStats.Core.dll')) `
    ('/pdb:' + (Join-Path $output 'MissionStats.Core.pdb')) @runtimeArgs @coreSources $coreTarget
if ($LASTEXITCODE -ne 0) { throw 'MissionStats.Core compilation failed.' }

$melonReferences = @(Get-ManagedReferences (Join-Path $melonLoader 'net6'))
$interopReferences = @()
foreach ($name in @(
    'Il2Cppmscorlib.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.IMGUIModule.dll',
    'Unity.InputSystem.dll',
    'UnityEngine.SharedInternalsModule.dll',
    'UnityEngine.TextRenderingModule.dll'
)) {
    $path = Join-Path $interopDirectory $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Required interop assembly missing: $name" }
    $interopReferences += $path
}
$pluginReferences = @($runtimeReferences + $melonReferences + $interopReferences + (Join-Path $output 'MissionStats.Core.dll')) |
    Sort-Object -Unique | ForEach-Object { '/reference:' + $_ }
$pluginSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\IronNestStats.Melon') -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object FullName

& dotnet $csc /nologo /noconfig /target:library /langversion:latest /nostdlib+ /deterministic+ /optimize+ `
    /debug:portable ('/out:' + (Join-Path $output 'MissionStats.dll')) `
    ('/pdb:' + (Join-Path $output 'MissionStats.pdb')) @pluginReferences @pluginSources $modTarget
if ($LASTEXITCODE -ne 0) { throw 'MissionStats MelonLoader compilation failed.' }

$hashes = Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $output 'MissionStats.dll'), `
    (Join-Path $output 'MissionStats.Core.dll')

# Verification executables and generated assembly attributes are build-time files only.
foreach ($temporaryDirectory in @((Join-Path $root 'artifacts\verify'), $generated)) {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}

$hashes
