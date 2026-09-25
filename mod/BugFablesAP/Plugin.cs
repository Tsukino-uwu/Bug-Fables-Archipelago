using System;
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
        private ConfigEntry<bool> entityDumpEnabled;
        private ConfigEntry<bool> mapDumpEnabled;
        private ConfigEntry<bool> varDumpEnabled;
        private ConfigEntry<bool> spriteDumpEnabled;
        private bool spriteDumpDone;
        private bool varDumpDone;
        private ConfigEntry<string> server;
        private ConfigEntry<string> port;
        private ConfigEntry<string> slot;
        private ConfigEntry<string> password;
        private ConfigEntry<bool> compression;
        private ConfigEntry<bool> randomizerEnabled;
        private ApConnection connection;
        private LocationChecks checks;
        private ItemReceiver receiver;
        // The details last tried automatically: new details connect at once; the same details only retry after an
        // unreachable server or a dropped connection (ApConnection.ShouldRetry).
        private string lastAttempt;
        private bool wasEnabled;
        private bool scriptDumpDone;
        private bool entityDumpDone;
        private bool mapDumpDone;
        private ConfigEntry<string> saveDiff;
        private ConfigEntry<bool> adoptSeed;
        private ConfigEntry<bool> devConsole;
        private ConfigEntry<string> difficulty;
        private ConfigEntry<bool> detector;
        private ConfigEntry<string> devCommandFile;
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
            adoptSeed = Config.Bind("Debug", "AdoptSeed", false,
                "Dev only. A save tied to another seed is re-tied to the connected one, its received count back to 0 "
                + "so the new seed replays every item. The old seed's items and flags stay in the save: test files only, "
                + "never a real game. Off by default.");
            ItemReceiver.AdoptOtherSeed = () => adoptSeed.Value;
            devConsole = Config.Bind("Debug", "DevConsole", false,
                "Dev only. F9 opens a command line: loc <n> (go to a pickup location), warp <map> [flag], "
                + "spawn <item|key|medal> <id> [flag], flag <n> [on|off]. Can put a save in states the story never "
                + "makes: test files only. Off by default.");
            DevConsole.InfJumpSetting = Config.Bind("Debug", "InfJump", false,
                "Dev only, with DevConsole. Each press of jump in mid-air jumps again, to reach high places. The console's "
                + "infjump flips it. Off by default.");
            DevConsole.OneHitSetting = Config.Bind("Debug", "OneHit", false,
                "Dev only, with DevConsole. Every hit on an enemy does at least 99, to get through test fights. The console's "
                + "onehit flips it. Off by default.");
            devCommandFile = Config.Bind("Debug", "DevCommandFile", "",
                "Dev only, with DevConsole. A text file the console also reads: each line is run as a typed command, "
                + "then the file is emptied. Lets a developer outside the game drive a test. Empty = off.");
            DevConsole.CommandFile = devCommandFile.Value;
            // Dev only: doors rewritten by hand, a proof of concept of the entrance randomizer (2026-09-25).
            DoorShuffle.TestDoors = Config.Bind("Debug", "TestDoors", "",
                "Dev only. Doors rewritten by hand: Map/Door=LikeMap/LikeDoor;... makes that door lead where the other one leads "
                + "(entity names). Empty = off.").Value;
            // Dev only: a stand-in for a random start (the user, 2026-09-25), until the seed chooses one.
            QualityOfLife.TestStart = Config.Bind("Debug", "TestStart", "",
                "Dev only. A map name (MainManager.Maps), optionally @ the map you arrive from, e.g. "
                + "BugariaMainPlaza@BugariaOutskirtsOutsideCity: a new file starts there, arriving through that map's door into "
                + "it (without @, the first door found). A stand-in for a random start. Empty = off.").Value;
            // Dev only: one starting party member (the user, 2026-09-25), a rehearsal before the yaml option.
            PartyMembers.StartMember = Config.Bind("Debug", "TestStartMember", -1,
                "Dev only. The one party member a randomizer file has (0 Vi, 1 Kabbu, 2 Leif): the story adds nobody else; "
                + "the console's addmember adds one. -1 = off.").Value;
            if (devConsole.Value)
            {
                DevConsole.EnableGuard(Guid);
            }
            saveDiff = Config.Bind("Debug", "SaveDiff", "",
                "Dev only. Two save file names separated by |, e.g. 'save2backup.dat|save2.dat'. Once per load, logs "
                + "what changed between them (read-only). Empty = off.");
            scriptDumpEnabled = Config.Bind("Debug", "ScriptDump", false,
                "Dev only. Once per launch, writes the item and flag command tokens of every map's dialogue lines to "
                + "BepInEx/bugfablesap-scriptdump.tsv. Off by default.");
            entityDumpEnabled = Config.Bind("Debug", "EntityDump", false,
                "Dev only. Once per launch, writes every map's entities (type, item, required and hiding flags) to "
                + "BepInEx/bugfablesap-entitydump.tsv, and item and medal names to bugfablesap-names.tsv. Off by default.");
            mapDumpEnabled = Config.Bind("Debug", "MapDump", false,
                "Dev only. Once per launch, writes every map prefab's auto-start events, hazards and electric triggers to "
                + "BepInEx/bugfablesap-mapdump.tsv. Off by default.");
            spriteDumpEnabled = Config.Bind("Debug", "SpriteDump", false,
                "Dev only. Once per load, saves the game's GUI sprite sheets as PNGs and a table of guisprites indexes to "
                + "the BepInEx folder, to pick art for the mod's own UI. Off by default.");
            varDumpEnabled = Config.Bind("Debug", "VarDump", false,
                "Dev only. Once per load, writes every flagvar/flagstring slot the game's text uses to "
                + "BepInEx/bugfablesap-vardump.tsv. Off by default.");
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
            compression = Config.Bind("Connection", "Compression", true,
                "Compress the connection (permessage-deflate), as the Archipelago server asks. Turn off only if "
                + "connecting fails with it on.");
            WebSocketCompression.Enable(Guid, connection.Post, () => compression.Value);
            checks = new LocationChecks(Log, connection);
            receiver = new ItemReceiver(Log, connection);
            DevConsole.Init(Log, connection);

            randomizerEnabled = Config.Bind("Archipelago", "RandomizerEnabled", false,
                "Archipelago mod enabled: the game uses its own saves in the 'archipelago' folder, apart from your normal "
                + "saves. Switch it in the Archipelago panel on the main menu.");
            SaveRedirect.On = randomizerEnabled.Value;
            SaveRedirect.Enable(Log, Guid);
            ItemSwap.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            KeptOpen.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            // The panel's Difficulty and Detector rows (the user, 2026-09-24): defaults Normal and On.
            difficulty = Config.Bind("Archipelago", "Difficulty", "Normal", new ConfigDescription(
                "Normal leaves it to the game; Hard acts as if the Hard Mode medal were equipped; Hardest as if the save had "
                + "the HARDEST code, never written into the save. Boss prize medals are paid out on every setting. "
                + "Switch it in the Archipelago panel.", new AcceptableValueList<string>(ApMenu.Difficulties)));
            detector = Config.Bind("Archipelago", "Detector", true,
                "On acts as if the Detector medal were equipped, to help find items. Off leaves it to the medal. "
                + "Switch it in the Archipelago panel.");
            ApMenu.Difficulty = difficulty;
            ApMenu.Detector = detector;
            MedalAssist.Enable(Log, Guid, () => randomizerEnabled.Value, () => difficulty.Value == "Hard",
                () => difficulty.Value == "Hardest", () => detector.Value);
            QualityOfLife.Enable(Log, Config, () => randomizerEnabled.Value);
            HoldUps.Init(Log, () => randomizerEnabled.Value);
            PartyFit.Enable(Log, Guid, () => randomizerEnabled.Value);
            PartyMembers.Enable(Log, Guid, () => randomizerEnabled.Value);
            CheckDetector.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            ShopSwap.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            ItemShops.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            DoorShuffle.Enable(Log, Guid, connection, () => randomizerEnabled.Value);
            WarpButton.Enable(Log, Guid, () => randomizerEnabled.Value && QualityOfLife.WarpButton.Value);
            MenuToggle.Enable(Log, Guid, randomizerEnabled, server, port, slot, password,
                () => { },
                () => connection.Status,
                () => connection.SeedKnown);
            Log.LogInfo($"{Name} {Version} loaded. GrantProbe={grantProbeEnabled.Value} TextProbe={textProbeEnabled.Value}");
        }

        // While the Archipelago mod is enabled and the details are filled in, connect on its own (the user,
        // 2026-09-24: less friction than a Connect button). A refusal waits for the details to change; an
        // unreachable server or a dropped connection retries on its own (ApConnection). Disabling disconnects.
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
            if (key != lastAttempt)
            {
                if (connection.Busy)
                {
                    return; // try the new details once the running attempt ends
                }
                lastAttempt = key;
                connection.ResetForNewDetails();
                connection.Connect(Target(), slot.Value, password.Value);
            }
            else if (connection.ShouldRetry(DateTime.UtcNow))
            {
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
            connection.Watchdog(DateTime.UtcNow);
            connection.Tick();
            checks.Tick(randomizerEnabled.Value);
            receiver.Tick(randomizerEnabled.Value);
            ItemSwap.TickGround();
            MedalAssist.Tick();
            MedalAssist.PayPrizes();
            QualityOfLife.Tick();
            KeptOpen.Tick();
            HoldUps.Tick();
            ShopSwap.Tick();
            ItemShops.Tick();
            PartyFit.Tick();

            DevCheats.Tick(Log, giveMoney);
            DevConsole.Tick(devConsole.Value);

            if (!saveDiffDone && !string.IsNullOrEmpty(saveDiff.Value))
            {
                saveDiffDone = true;
                string[] pair = saveDiff.Value.Split('|');
                if (pair.Length == 2)
                {
                    SaveDiff.Run(Log, pair[0].Trim(), pair[1].Trim());
                }
            }

            if (varDumpEnabled.Value && !varDumpDone && MainManager.instance != null && MainManager.instance.prizeflags != null)
            {
                varDumpDone = true;
                VarDump.Run(Log);
            }

            if (scriptDumpEnabled.Value && !scriptDumpDone)
            {
                scriptDumpDone = ScriptDump.TryRun(Log);
            }

            if (entityDumpEnabled.Value && !entityDumpDone)
            {
                entityDumpDone = EntityDump.TryRun(Log);
            }

            if (spriteDumpEnabled.Value && !spriteDumpDone)
            {
                spriteDumpDone = SpriteDump.TryRun(Log);
            }

            if (mapDumpEnabled.Value && !mapDumpDone)
            {
                mapDumpDone = MapDump.TryRun(Log);
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

        private void OnGUI()
        {
            DevConsole.Draw(devConsole != null && devConsole.Value);
        }

        private void OnDestroy()
        {
            DevConsole.Tick(false);
            DevConsole.DisableGuard();
            TextProbe.Disable();
            MenuToggle.Disable();
            SaveRedirect.Disable();
            // A hot reload must not leave the old instance's socket open next to the new one.
            connection?.Disconnect();
            WebSocketCompression.Disable();
            ItemSwap.Disable();
            MedalAssist.Disable();
            KeptOpen.Disable();
            QualityOfLife.Disable();
            WarpButton.Disable();
            HoldUps.Clear();
            PartyFit.Disable();
            PartyMembers.Disable();
            CheckDetector.Disable();
            ShopSwap.Disable();
            ItemShops.Disable();
            DoorShuffle.Disable();
            // ScriptEngine destroys the old instance on reload. Say so, so a reload shows up in the log.
            Log?.LogInfo($"{Name} {Version} unloaded.");
        }
    }
}
