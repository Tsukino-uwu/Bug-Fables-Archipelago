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
        private ConfigEntry<bool> textProbeEnabled;
        private ConfigEntry<bool> scriptDumpEnabled;
        private ConfigEntry<string> server;
        private ConfigEntry<string> slot;
        private ConfigEntry<string> password;
        private ConfigEntry<bool> connectOnStart;
        private ApConnection connection;
        private bool connectRequested;
        private bool scriptDumpDone;
        private GrantProbe grantProbe;

        private void Awake()
        {
            Log = Logger;
            grantProbeEnabled = Config.Bind("Debug", "GrantProbe", false,
                "Dev only. Logs every key item added to the inventory and every flag that flips, with the map, "
                + "to measure how locations can be identified. Off by default.");
            textProbeEnabled = Config.Bind("Debug", "TextProbe", false,
                "Dev only. Logs every dialogue script that carries an item command, with the map and calling NPC. "
                + "Off by default.");
            scriptDumpEnabled = Config.Bind("Debug", "ScriptDump", false,
                "Dev only. Once per launch, writes the item and flag command tokens of every map's dialogue lines to "
                + "BepInEx/bugfablesap-scriptdump.tsv. Off by default.");
            if (textProbeEnabled.Value)
            {
                TextProbe.Enable(Log, Guid);
            }
            server = Config.Bind("Connection", "Server", "localhost:38281", "Archipelago server address and port.");
            slot = Config.Bind("Connection", "Slot", "", "Your slot name in the room.");
            password = Config.Bind("Connection", "Password", "", "The room password, if it has one.");
            connectOnStart = Config.Bind("Connection", "ConnectOnStart", false,
                "Connect as soon as the game starts. Needs a slot name.");
            connection = new ApConnection(Log);
            Log.LogInfo($"{Name} {Version} loaded. GrantProbe={grantProbeEnabled.Value} TextProbe={textProbeEnabled.Value}");
        }

        private bool devReloadChecked;
        private DevReload devReload;

        private string lastError;

        private void Update()
        {
            // An exception thrown from Update goes to Unity's log, which this game doesn't write and BepInEx
            // doesn't copy by default. So catch and log it here, once per distinct message.
            try
            {
                Tick();
            }
            catch (System.Exception e)
            {
                string text = e.ToString();
                if (text != lastError)
                {
                    lastError = text;
                    Log.LogError($"Update threw: {text}");
                }
            }
        }

        private void Tick()
        {
            // Looked up on the first frame rather than in Awake, so ScriptEngine's own object exists by then.
            if (!devReloadChecked)
            {
                devReloadChecked = true;
                devReload = DevReload.TryCreate(Log);
            }
            devReload?.Tick();

            if (connectOnStart.Value && !connectRequested)
            {
                connectRequested = true;
                if (string.IsNullOrEmpty(slot.Value))
                {
                    Log.LogWarning("[ap] ConnectOnStart is on but no Slot is set; not connecting.");
                }
                else
                {
                    connection.Connect(server.Value, slot.Value, password.Value);
                }
            }
            connection.Tick();

            if (scriptDumpEnabled.Value && !scriptDumpDone)
            {
                scriptDumpDone = ScriptDump.TryRun(Log);
            }

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
            TextProbe.Disable();
            // A hot reload must not leave the old instance's socket open next to the new one.
            connection?.Disconnect();
            // ScriptEngine destroys the old instance on reload. Say so, so a reload shows up in the log.
            Log?.LogInfo($"{Name} {Version} unloaded.");
        }
    }
}
