using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BugFablesAP
{
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BaseUnityPlugin
    {
        public const string Guid = "bugfables.archipelago";
        public const string Name = "Bug Fables Archipelago";
        public const string Version = "0.2.0";

        internal static ManualLogSource Log;
        // Set as the game starts closing: a closing game needs nothing put back for a hot reload.
        internal static bool Quitting { get; private set; }

        private ConfigEntry<string> server;
        private ConfigEntry<string> port;
        private ConfigEntry<string> slot;
        private ConfigEntry<string> password;
        private ConfigEntry<bool> compression;
        private ConfigEntry<bool> randomizerEnabled;
        private ApConnection connection;
        private LocationChecks checks;
        private ItemReceiver receiver;
        // The details last tried automatically: the same details only retry after an unreachable server or a drop.
        private string lastAttempt;
        private bool wasEnabled;
        private ConfigEntry<string> difficulty;
        private ConfigEntry<bool> detector;

        // The dev build's tools (Dev/Plugin.Dev.cs). The release build compiles without them, and these calls vanish.
        partial void DevAwakeEarly();
        partial void DevAwakeLate();
        partial void DevBeforeTick();
        partial void AddDevSteps(List<(string Name, Action Step)> list);
        partial void DevAfterTick();
        partial void DevDestroy();

        private void Awake()
        {
            Log = Logger;
            Hooks.Init(Log);
            UnityEngine.Application.quitting += MarkQuitting;
            DevAwakeEarly();
            // A server on this computer needs the ws:// prefix; a bare localhost:38281 times out.
            server = Config.Bind("Connection", "Address", "archipelago.gg",
                "The Archipelago server's address: archipelago.gg for a hosted room, or ws://127.0.0.1 for a server "
                + "on this computer.");
            port = Config.Bind("Connection", "Port", "",
                "The room's port, e.g. 38281. Rooms on archipelago.gg show it.");
            slot = Config.Bind("Connection", "Slot", "", "Your slot name in the room.");
            password = Config.Bind("Connection", "Password", "", "The room password, if it has one.");
            connection = new ApConnection(Log);
            compression = Config.Bind("Connection", "Compression", true,
                "Compress the connection (permessage-deflate), as the Archipelago server asks. Turn off only if "
                + "connecting fails with it on.");
            WebSocketCompression.Enable(connection.Post, () => compression.Value);
            CachePaths.Enable(Log);
            checks = new LocationChecks(Log, connection);
            receiver = new ItemReceiver(Log, connection);
            DevAwakeLate();

            randomizerEnabled = Config.Bind("Archipelago", "RandomizerEnabled", false,
                "Archipelago mod enabled: the game uses its own saves in the 'archipelago' folder, apart from your "
                + "normal saves. Switch it in the Archipelago panel on the main menu.");
            SaveRedirect.On = randomizerEnabled.Value;
            SaveRedirect.Enable(Log);
            ItemSwap.Enable(Log, connection, () => randomizerEnabled.Value);
            KeptOpen.Enable(Log, connection, () => randomizerEnabled.Value);
            EnemyShuffle.Enable(Log, connection, () => randomizerEnabled.Value);
            MusicShuffle.Enable(Log, connection, () => randomizerEnabled.Value);
            difficulty = Config.Bind("Archipelago", "Difficulty", "Normal", new ConfigDescription(
                "Normal leaves it to the game; Hard acts as if the Hard Mode medal were equipped; Hardest as if the "
                + "save had the HARDEST code, never written into the save. In a seed, boss prize medals are paid out "
                + "on every setting; on a normal save, as in "
                + "the game. Switch it on the Gameplay page.", new AcceptableValueList<string>(ApMenu.Difficulties)));
            detector = Config.Bind("Archipelago", "Detector", true,
                "On acts as if the Detector medal were equipped; in a seed it beeps for any check left in the room. "
                + "Off leaves it to the medal. "
                + "Switch it on the Quality of life page.");
            ApMenu.Difficulty = difficulty;
            ApMenu.Detector = detector;
            ApMenu.Achievements = Config.Bind("Archipelago", "Achievements", false,
                "On lets Steam achievements unlock while Archipelago is enabled; off (the default) holds them back, as "
                + "normal saves are kept apart. It only concerns Steam, never Archipelago. Switch it in the "
                + "Archipelago panel.");
            AchievementGuard.Enable(Log, () => randomizerEnabled.Value, () => ApMenu.Achievements.Value);
            ApMenu.NormalSaves = Config.Bind("Archipelago", "NormalSaves", false,
                "On: the Quality of life and Gameplay settings also apply with Archipelago off, on normal saves. "
                + "Nothing tied to a seed does (items, checks, the shuffles, the intro skip). Off (the default) keeps "
                + "normal saves vanilla. Switch it in the Archipelago panel.");
            Func<bool> settingsOn = () => randomizerEnabled.Value || ApMenu.NormalSaves.Value;
            AnimGuard.Enable(Log, settingsOn);
            GlowGuard.Enable(Log, settingsOn);
            RespawnLoop.Enable(Log, settingsOn);
            MedalAssist.Enable(Log, () => randomizerEnabled.Value, settingsOn, () => difficulty.Value == "Hard",
                () => difficulty.Value == "Hardest", () => detector.Value, () => QualityOfLife.SpyHp,
                () => QualityOfLife.SpyFree);
            QualityOfLife.Enable(Log, Config, () => randomizerEnabled.Value);
            QualityOfLife.SettingsOn = settingsOn;
            QualityOfLife.SeedStart = () => randomizerEnabled.Value ? connection?.Start : null;
            QualityOfLife.SeedStartFrom = () => randomizerEnabled.Value ? connection?.StartFrom : null;
            QualityOfLife.SeedAdded = () => randomizerEnabled.Value ? connection?.LocationAdded : null;
            QualityOfLife.SeedQuiet = () => randomizerEnabled.Value ? connection?.QuietLocations : null;
            QualityOfLife.SeedKnown = () => connection != null && connection.SeedKnown;
            QualityOfLife.EntrancesShuffled = () => randomizerEnabled.Value && connection?.DoorTargets != null
                && connection.DoorTargets.Count > 0;
            QualityOfLife.PointsOfNoReturn = () => randomizerEnabled.Value && (connection?.Seed?.PointsOfNoReturn ?? false);
            Multipliers.Enable(Log, Config, settingsOn);
            EnemyScaling.Enable(Log, settingsOn, () => QualityOfLife.EnemyScalingMode?.Value);
            AttackBoost.Enable(Log, Config, settingsOn);
            FrameRate.Enable(Log, settingsOn);
            StartCoroutine(AfterEachPhysicsStep());
            ClockCleanup.Enable(Log, settingsOn);
            InGameSettings.Enable(Log, settingsOn);
            CustomItems.Enable(Log, () => randomizerEnabled.Value);
            BoatTicket.Enable(Log, () => randomizerEnabled.Value);
            HoldUps.Init(Log, () => randomizerEnabled.Value);
            PartyFit.Enable(Log, () => randomizerEnabled.Value);
            PartySlots.Enable(Log, () => randomizerEnabled.Value);
            PartyMembers.Enable(Log, () => connection.Seed, () => randomizerEnabled.Value);
            FieldMoves.Enable(Log, () => connection.Seed, () => randomizerEnabled.Value);
            SaveCrystals.Enable(Log, Config, () => randomizerEnabled.Value, settingsOn);
            DeathLinkGame.Enable(Log, Config, connection, () => randomizerEnabled.Value);
            AutoSave.Enable(Log, Config, settingsOn);
            Abilities.Enable(Log, () => connection.Seed, () => randomizerEnabled.Value);
            Submarine.Enable(Log, () => connection.Seed, () => randomizerEnabled.Value);
            CheckDetector.Enable(Log, connection, () => randomizerEnabled.Value);
            CrystalBerryTotal.Enable(Log, connection, () => randomizerEnabled.Value);
            QuestBoards.Enable(Log, () => randomizerEnabled.Value);
            ShopSwap.Enable(Log, connection, () => randomizerEnabled.Value);
            ItemShops.Enable(Log, connection, () => randomizerEnabled.Value);
            ShopInventories.Enable(Log, connection, () => randomizerEnabled.Value);
            DoorShuffle.Enable(Log, connection, () => randomizerEnabled.Value);
            WarpButton.Enable(Log, () => settingsOn() && QualityOfLife.WarpOn,
                () => settingsOn() && QualityOfLife.MapOn,
                () => QualityOfLife.SkipWarpConfirm, () => QualityOfLife.SkipMapConfirm);
            MenuToggle.Enable(Log, randomizerEnabled, server, port, slot, password,
                () => connection.Status,
                () => connection.SeedKnown);
            Log.LogInfo($"{Name} {Version} loaded.");
        }

        // Connects on its own while enabled with details filled in; a refusal waits for new details. Disabling
        // disconnects.
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

        private string Target()
        {
            string address = server.Value.Trim().TrimEnd('/');
            string p = port.Value.Trim();
            return p.Length == 0 ? address : address + ":" + p;
        }

        private readonly Dictionary<string, string> lastErrors = new Dictionary<string, string>();
        private (string Name, Action Step)[] steps;

        private void Update()
        {
            Guarded("tick", Tick);
        }

        private void LateUpdate()
        {
            Guarded("music", MusicShuffle.LateTick);
        }

        private void FixedUpdate()
        {
            Guarded("fps", FrameRate.BeforePhysics);
        }

        // Resumes after each physics step and its trigger messages (Unity's order of execution).
        private IEnumerator AfterEachPhysicsStep()
        {
            var wait = new UnityEngine.WaitForFixedUpdate();
            while (true)
            {
                yield return wait;
                Guarded("fps-step", FrameRate.AfterPhysics);
            }
        }

        // Each system runs in its own guard, so one that throws every frame doesn't stop the ones after it.
        // Unity's log isn't written by this game, so exceptions are logged here, once per distinct message per system.
        private void Guarded(string name, Action step)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                string text = e.ToString();
                if (!lastErrors.TryGetValue(name, out string last) || last != text)
                {
                    lastErrors[name] = text;
                    Log.LogError($"[{name}] threw: {text}");
                }
            }
        }

        private void Tick()
        {
            DevBeforeTick();
            if (steps == null)
            {
                var list = new List<(string Name, Action Step)>
                {
                    ("connect", AutoConnect),
                    ("watchdog", () => connection.Watchdog(DateTime.UtcNow)),
                    ("connection", connection.Tick),
                    ("checks", () => checks.Tick(randomizerEnabled.Value)),
                    ("recv", () => receiver.Tick(randomizerEnabled.Value)),
                    ("itemswap", ItemSwap.TickGround),
                    ("medals", MedalAssist.Tick),
                    ("customitems", CustomItems.Tick),
                    ("prizes", MedalAssist.PayPrizes),
                    ("qol", QualityOfLife.Tick),
                    ("keptopen", KeptOpen.Tick),
                    ("holdups", HoldUps.Tick),
                    ("shops", ShopSwap.Tick),
                    ("inventories", ShopInventories.Tick),
                    ("itemshops", ItemShops.Tick),
                    ("party", PartyFit.Tick),
                    ("members", PartyMembers.Tick),
                    ("moves", FieldMoves.Tick),
                    ("crystals", SaveCrystals.Tick),
                    ("deathlink", DeathLinkGame.Tick),
                    ("autosave", AutoSave.Tick),
                    ("respawn", RespawnLoop.Tick),
                    ("fps", FrameRate.Tick),
                };
                AddDevSteps(list);
                steps = list.ToArray();
            }
            foreach ((string name, Action step) in steps)
            {
                Guarded(name, step);
            }
            DevAfterTick();
        }

        private static void MarkQuitting()
        {
            Quitting = true;
            Log?.LogInfo("[quit] the game is closing: nothing is put back for a hot reload");
        }

        private void OnDestroy()
        {
            UnityEngine.Application.quitting -= MarkQuitting;
            // Each step on its own: one that throws must not keep the rest, the hooks above all, from coming off.
            Guarded("unload", () => DevDestroy());
            Guarded("unload", MenuToggle.Disable);
            // A hot reload must not leave the old instance's socket open next to the new one.
            Guarded("unload", () => connection?.Disconnect());
            Guarded("unload", MedalAssist.Disable);
            Guarded("unload", FrameRate.Disable);
            Guarded("unload", SaveCrystals.Disable);
            Guarded("unload", QualityOfLife.Disable);
            Guarded("unload", WarpButton.Disable);
            Guarded("unload", HoldUps.Clear);
            Guarded("unload", PartyFit.Disable);
            Guarded("unload", ShopSwap.Disable);
            Guarded("unload", MusicShuffle.Disable);
            Guarded("unload", Hooks.UninstallAll);
            Log?.LogInfo($"{Name} {Version} unloaded.");
        }
    }
}
