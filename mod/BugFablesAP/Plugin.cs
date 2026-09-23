using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BugFablesAP
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bugfables.archipelago";
        public const string Name = "Bug Fables Archipelago";
        public const string Version = "0.0.1";

        internal static ManualLogSource Log;

        private ConfigEntry<bool> grantProbeEnabled;
        private GrantProbe grantProbe;

        private void Awake()
        {
            Log = Logger;
            grantProbeEnabled = Config.Bind("Debug", "GrantProbe", false,
                "Dev only. Logs every key item added to the inventory and every flag that flips, with the map, "
                + "to measure how locations can be identified. Off by default.");
            Log.LogInfo($"{Name} {Version} loaded. GrantProbe={grantProbeEnabled.Value}");
        }

        private void Update()
        {
            if (!grantProbeEnabled.Value)
            {
                return;
            }
            if (grantProbe == null)
            {
                grantProbe = new GrantProbe(Log);
            }
            grantProbe.Tick();
        }

        private void OnDestroy()
        {
            // ScriptEngine destroys the old instance on reload. Say so, so a reload shows up in the log.
            Log?.LogInfo($"{Name} {Version} unloaded.");
        }
    }
}
