[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw 'Visual Studio 2022 Roslyn compiler not found.' }

$out = Join-Path ([IO.Path]::GetTempPath()) ('MissionStats-verify-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $out | Out-Null
try {
    $coreSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\IronNestStats.Core') -Recurse -Filter *.cs -File |
        ForEach-Object FullName
    $pluginSources = Get-ChildItem -LiteralPath (Join-Path $root 'src\IronNestStats.Melon') -Recurse -Filter *.cs -File |
        ForEach-Object FullName

    & $csc /nologo /langversion:latest /target:exe ('/out:' + (Join-Path $out 'CoreTests.exe')) `
        @coreSources (Join-Path $root 'tests\IronNestStats.Core.Tests\Program.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Core test compilation failed.' }
    & (Join-Path $out 'CoreTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }

    & $csc /nologo /langversion:latest /target:library ('/out:' + (Join-Path $out 'MissionStats.SyntaxCheck.dll')) `
        @coreSources @pluginSources (Join-Path $root 'tests\Stubs\ExternalStubs.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Plugin architecture syntax check failed.' }
    Write-Host 'PASS: MissionStats plugin architecture syntax check'
}
finally {
    if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Recurse -Force }
}
