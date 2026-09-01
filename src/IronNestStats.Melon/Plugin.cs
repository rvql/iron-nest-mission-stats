using IronNestStats.Melon.Configuration;
using IronNestStats.Melon.Runtime;
using MelonLoader;

[assembly: MelonInfo(typeof(IronNestStats.Melon.Plugin), "MissionStats", "0.1.0", "rvql")]
[assembly: MelonGame("Iron Nest", "Iron Nest Heavy Turret Simulator")]

namespace IronNestStats.Melon
{
    public sealed class Plugin : MelonMod
    {
        private ModRuntime _runtime;

        public override void OnInitializeMelon()
        {
            var settings = new ModSettings();
            _runtime = new ModRuntime(settings, message => LoggerInstance.Msg(message),
                message => LoggerInstance.Warning(message));
            LoggerInstance.Msg("MissionStats loaded. No native game-method hooks are used.");
        }

        public override void OnUpdate()
        {
            if (_runtime != null) _runtime.Update();
        }

        public override void OnGUI()
        {
            if (_runtime != null) _runtime.Draw();
        }

        public override void OnDeinitializeMelon()
        {
            if (_runtime != null) _runtime.Shutdown();
            _runtime = null;
        }
    }
}
