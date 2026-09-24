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
        private ConfigEntry<bool> randomizerEnabled;
        private ApConnection connection;
        private bool connectRequested;
        private bool scriptDumpDone;
        private ConfigEntry<string> saveDiff;
        private ConfigEntry<int> giveMoney;
        private bool saveDiffDone;
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
            giveMoney = Config.Bind("Debug", "GiveMoney", 0,
                "Dev only. Berries to add once (the game caps at 999), then this resets to 0.");
            saveDiff = Config.Bind("Debug", "SaveDiff", "",
                "Dev only. Two save file names separated by |, e.g. 'save2backup.dat|save2.dat'. Once per load, logs "
                + "what changed between them (read-only). Empty = off.");
            scriptDumpEnabled = Config.Bind("Debug", "ScriptDump", false,
                "Dev only. Once per launch, writes the item and flag command tokens of every map's dialogue lines to "
                + "BepInEx/bugfablesap-scriptdump.tsv. Off by default.");
            if (textProbeEnabled.Value)
            {
                TextProbe.Enable(Log, Guid);
            }
            // The default is a local server. It needs the ws:// prefix: a bare "localhost:38281" timed out
            // against a local server on 2026-09-24, and ws://127.0.0.1:38281 logged in.
            server = Config.Bind("Connection", "Server", "ws://127.0.0.1:38281",
                "Archipelago server address and port, e.g. archipelago.gg:38281 for a hosted room, or "
                + "ws://127.0.0.1:38281 for a server on this computer.");
            slot = Config.Bind("Connection", "Slot", "", "Your slot name in the room.");
            password = Config.Bind("Connection", "Password", "", "The room password, if it has one.");
            connectOnStart = Config.Bind("Connection", "ConnectOnStart", false,
                "Connect as soon as the game starts. Needs a slot name.");
            connection = new ApConnection(Log);

            randomizerEnabled = Config.Bind("Archipelago", "RandomizerEnabled", false,
                "Archipelago mode: the game uses its own saves in the 'archipelago' folder, apart from your normal "
                + "saves. Switch it with 'Archipelago: On/Off' on the main menu.");
            SaveRedirect.On = randomizerEnabled.Value;
            SaveRedirect.Enable(Log, Guid);
            MenuToggle.Enable(Log, Guid, randomizerEnabled, server, slot, password,
                () => connection.Connect(server.Value, slot.Value, password.Value),
                () => connection.Status);
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

            DevCheats.Tick(Log, giveMoney);

            if (!saveDiffDone && !string.IsNullOrEmpty(saveDiff.Value))
            {
                saveDiffDone = true;
                string[] pair = saveDiff.Value.Split('|');
                if (pair.Length == 2)
                {
                    SaveDiff.Run(Log, pair[0].Trim(), pair[1].Trim());
                }
            }

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
            MenuToggle.Disable();
            SaveRedirect.Disable();
            // A hot reload must not leave the old instance's socket open next to the new one.
            connection?.Disconnect();
            // ScriptEngine destroys the old instance on reload. Say so, so a reload shows up in the log.
            Log?.LogInfo($"{Name} {Version} unloaded.");
        }
    }
}
