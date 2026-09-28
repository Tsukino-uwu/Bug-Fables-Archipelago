using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace BugFablesAP
{
    // The plugin's dev half: the [Debug] settings, the console, probes and dumps. Only the dev build compiles it; in the
    // release build the partial calls to it in Plugin.cs vanish.
    public partial class Plugin
    {
        private ConfigEntry<bool> grantProbeEnabled;
        private ConfigEntry<bool> textProbeEnabled;
        private ConfigEntry<bool> scriptDumpEnabled;
        private ConfigEntry<bool> entityDumpEnabled;
        private ConfigEntry<bool> mapDumpEnabled;
        private ConfigEntry<bool> varDumpEnabled;
        private ConfigEntry<bool> questDumpEnabled;
        private ConfigEntry<bool> patchDumpEnabled;
        private ConfigEntry<bool> seedDumpEnabled;
        private ConfigEntry<bool> spriteDumpEnabled;
        private ConfigEntry<string> saveDiff;
        private ConfigEntry<bool> adoptSeed;
        private ConfigEntry<bool> devConsole;
        private ConfigEntry<string> devCommandFile;
        private ConfigEntry<int> giveMoney;
        private bool patchDumpDone, seedDumpDone, spriteDumpDone, varDumpDone, questDumpDone, scriptDumpDone, entityDumpDone, mapDumpDone;
        private bool saveDiffDone;
        private GrantProbe grantProbe;
        private bool devReloadChecked;
        private DevReload devReload;

        partial void DevAwakeEarly()
        {
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
            DevConsole.InfBerriesSetting = Config.Bind("Debug", "InfBerries", false,
                "Dev only, with DevConsole. Berries stay at 999, the game's cap, for test purchases. The console's infberries "
                + "flips it. Off by default.");
            devCommandFile = Config.Bind("Debug", "DevCommandFile", "",
                "Dev only, with DevConsole. A text file the console also reads: each line is run as a typed command, "
                + "then the file is emptied. Lets a developer outside the game drive a test. Empty = off.");
            DevConsole.CommandFile = devCommandFile.Value;
            DoorShuffle.TestDoors = Config.Bind("Debug", "TestDoors", "",
                "Dev only. Doors rewritten by hand: Map/Door=LikeMap/LikeDoor;... makes that door lead where the other one leads "
                + "(entity names). Empty = off.").Value;
            QualityOfLife.TestStart = Config.Bind("Debug", "TestStart", "",
                "Dev only. A map name (MainManager.Maps), optionally @ the map you arrive from, e.g. "
                + "BugariaMainPlaza@BugariaOutskirtsOutsideCity: a new file starts there, arriving through that map's door into "
                + "it (without @, the first door found). A stand-in for a random start. Empty = off.").Value;
            PartyMembers.DevStartMember = Config.Bind("Debug", "TestStartMember", -1,
                "Dev only. The one party member a randomizer file has (0 Vi, 1 Kabbu, 2 Leif): the story adds nobody else; "
                + "the console's addmember adds one. -1 = off.").Value;
            if (devConsole.Value)
            {
                DevConsole.EnableGuard(Log);
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
            patchDumpEnabled = Config.Bind("Debug", "PatchDump", false,
                "Dev only. Once per load, writes every method the mod patches (target, kind, patch method, priority) to "
                + "BepInEx/bugfablesap-patches.tsv, Uncap FPS's hooks included, to compare before and after a refactor. "
                + "Off by default.");
            seedDumpEnabled = Config.Bind("Debug", "SeedDump", false,
                "Dev only. Once per load, when a login brings the seed, writes everything the mod read from its slot_data "
                + "to BepInEx/bugfablesap-seed.tsv, to compare before and after a change to how it's read. Off by default.");
            questDumpEnabled = Config.Bind("Debug", "QuestDump", false,
                "Dev only. Once per launch, writes every board quest's name, BoardData numbers and QuestChecks row to "
                + "BepInEx/bugfablesap-questdump.tsv. Off by default.");
            if (textProbeEnabled.Value)
            {
                TextProbe.Enable(Log);
            }
        }

        partial void DevAwakeLate()
        {
            DevConsole.Init(Log, connection);
            Log.LogInfo($"[dev] dev build: GrantProbe={grantProbeEnabled.Value} TextProbe={textProbeEnabled.Value}");
        }

        partial void DevBeforeTick()
        {
            // On the first frame, not in Awake, so ScriptEngine's own object exists.
            if (!devReloadChecked)
            {
                devReloadChecked = true;
                devReload = DevReload.TryCreate(Log);
            }
            devReload?.Tick();
        }

        partial void AddDevSteps(List<(string Name, Action Step)> list)
        {
            list.Add(("cheats", () => DevCheats.Tick(Log, giveMoney)));
            list.Add(("console", () => DevConsole.Tick(devConsole.Value)));
        }

        partial void DevAfterTick()
        {
            if (!saveDiffDone && !string.IsNullOrEmpty(saveDiff.Value))
            {
                saveDiffDone = true;
                string[] pair = saveDiff.Value.Split('|');
                if (pair.Length == 2)
                {
                    SaveDiff.Run(Log, pair[0].Trim(), pair[1].Trim());
                }
            }

            if (patchDumpEnabled.Value && !patchDumpDone)
            {
                patchDumpDone = true;
                PatchDump.Run(Log, Guid);
            }

            if (seedDumpEnabled.Value && !seedDumpDone && connection.SeedKnown)
            {
                seedDumpDone = true;
                SeedDump.Run(Log, connection);
            }

            if (questDumpEnabled.Value && !questDumpDone && MainManager.boardquestdata != null)
            {
                questDumpDone = true;
                QuestDump.Run(Log);
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

        partial void DevDestroy()
        {
            DevConsole.Tick(false);
        }
    }
}
