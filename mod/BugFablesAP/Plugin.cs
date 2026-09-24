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
        private ConfigEntry<string> port;
        private ConfigEntry<string> slot;
        private ConfigEntry<string> password;
        private ConfigEntry<bool> randomizerEnabled;
        private ApConnection connection;
        // The details last tried automatically; a failure isn't retried until something changes or Reconnect.
        private string lastAttempt;
        private bool wasEnabled;
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
            // Address and port are separate so a player usually edits only the port (the user, 2026-09-24).
            // A server on this computer needs the ws:// prefix: a bare "localhost:38281" timed out on
            // 2026-09-24, and ws://127.0.0.1:38281 logged in.
            server = Config.Bind("Connection", "Address", "archipelago.gg",
                "The Archipelago server's address: archipelago.gg for a hosted room, or ws://127.0.0.1 for a server "
                + "on this computer.");
            port = Config.Bind("Connection", "Port", "", "The room's port, e.g. 38281. Rooms on archipelago.gg show it.");
            slot = Config.Bind("Connection", "Slot", "", "Your slot name in the room.");
            password = Config.Bind("Connection", "Password", "", "The room password, if it has one.");
            connection = new ApConnection(Log);

            randomizerEnabled = Config.Bind("Archipelago", "RandomizerEnabled", false,
                "Archipelago mod enabled: the game uses its own saves in the 'archipelago' folder, apart from your normal "
                + "saves. Switch it in the Archipelago panel on the main menu.");
            SaveRedirect.On = randomizerEnabled.Value;
            SaveRedirect.Enable(Log, Guid);
            MenuToggle.Enable(Log, Guid, randomizerEnabled, server, port, slot, password,
                Reconnect,
                () => connection.Status);
            Log.LogInfo($"{Name} {Version} loaded. GrantProbe={grantProbeEnabled.Value} TextProbe={textProbeEnabled.Value}");
        }

        // While the Archipelago mod is enabled and the details are filled in, connect on its own (the user,
        // 2026-09-24: less friction than a Connect button). Each set of details is tried once; after a failure
        // it waits for a change or Reconnect instead of retrying in a loop. Disabling disconnects.
        private void AutoConnect()
        {
            bool enabled = randomizerEnabled.Value;
            if (!enabled)
            {
                if (wasEnabled)
                {
                    connection.Disconnect();
                    connection.SetStatus("Archipelago mod disabled.");
                    lastAttempt = null;
                }
                wasEnabled = false;
                return;
            }
            wasEnabled = true;
            if (!DetailsFilled())
            {
                connection.SetStatus("Fill in the address, port and slot to connect.");
                lastAttempt = null;
                return;
            }
            string key = Target() + "|" + slot.Value + "|" + password.Value;
            if (key != lastAttempt && !connection.Busy)
            {
                lastAttempt = key;
                connection.Connect(Target(), slot.Value, password.Value);
            }
        }

        private bool DetailsFilled()
        {
            string address = server.Value.Trim();
            bool hasPort = port.Value.Trim().Length > 0
                || System.Text.RegularExpressions.Regex.IsMatch(address, @":\d{1,5}$");
            return address.Length > 0 && hasPort && slot.Value.Trim().Length > 0;
        }

        // The panel's Reconnect row: forget the last attempt, so the next frame tries again.
        private void Reconnect()
        {
            if (!randomizerEnabled.Value)
            {
                connection.SetStatus("Enable the Archipelago mod first.");
                return;
            }
            lastAttempt = null;
        }

        // "address:port", or the address alone when no port is set (it may carry one already).
        private string Target()
        {
            string address = server.Value.Trim().TrimEnd('/');
            string p = port.Value.Trim();
            return p.Length == 0 ? address : address + ":" + p;
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

            AutoConnect();
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
