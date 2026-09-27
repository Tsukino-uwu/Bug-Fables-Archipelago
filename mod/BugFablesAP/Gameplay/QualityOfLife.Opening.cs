using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    internal static partial class QualityOfLife
    {
        // Always with Archipelago on: a seed's start ends the opening with a transfer, and its tutorial battle
        // was written for two, never played with one member. Skip cutscenes is for later scenes.
        // The opening: Event16 (Maki's talk, Vi joining, the tutorial battle, location 1) never starts; the mod does
        // what it would leave behind, the game's way, on a later frame. Flag 15 marks location 1 done.
        private const string OpeningMap = "BugariaOutskirtsOutsideCity";
        private const int OpeningEvent = 16;
        private const long OpeningLocation = 7_720_001; // Outskirts: Maki and Eetl's Gift (apworld id 1)
        private static bool openingPending;
        private static bool openingFailed; // one try per session: a failure is logged, never retried every frame
        // Dev only ([Debug] TestStart): a map the opening ends with a warp to, a stand-in for a random start.
        internal static string TestStart;
        // The seed's start (Starting Location): the opening ends with a transfer beside that save point instead.
        internal static Func<KeyValuePair<string, int>?> SeedStart;
        // A room start: the map whose door leads into the start map.
        internal static Func<string> SeedStartFrom;

        // A room start's door spots (appear, walk to), read from the door in the "from" map; null for a save-point start.
        internal static Vector3[] SeedStartDoor(MainManager.Maps map)
        {
            string from = SeedStartFrom?.Invoke();
            return from == null ? null : DoorInto(map, from);
        }
        internal static Func<bool> SeedKnown;
        private static KeyValuePair<string, int>? Seeded => SeedStart?.Invoke();
        // With Archipelago on, or off with Use on normal saves: fast text and the scene list, never the intro.
        internal static Func<bool> SettingsOn;
        private static bool startPending;
        // With a test start, the slides' backdrop stays up until the start map has loaded behind the transfer's fade.
        private static GameObject heldBack;
        private static bool transferring;
        private static float heldSince;

        private static bool TestStartSet => !string.IsNullOrEmpty(TestStart);
        // "Map" or "Map@FromMap": the start map, and optionally the map whose door into it the party arrives through.
        private static string StartMapName => TestStart.Split('@')[0].Trim();

        private static bool AtStart(string map) =>
            map != null && (TestStartSet ? string.Equals(map, StartMapName, StringComparison.OrdinalIgnoreCase) : map == OpeningMap);

        // Arriving as if through a door: data[0] the map, vectordata[1] where the party appears, [2] where it walks.
        // Read from the entity table of the map left behind; fields split by '}', data count at 60, vectordata at 71.
        internal static Vector3[] DoorInto(MainManager.Maps target, string fromMap)
        {
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                if (fromMap != null && !string.Equals(map.ToString(), fromMap, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                TextAsset table = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
                if (table == null)
                {
                    continue;
                }
                string[] lines = table.ToString().Split('\n');
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string[] f = lines[i].Split('}');
                    if (f.Length < 81 || f[1].Trim() != "DoorOtherMap" || f[60].Trim() == "0" || f[61].Trim() != ((int)target).ToString())
                    {
                        continue;
                    }
                    int count = int.Parse(f[71].Trim());
                    if (count < 3)
                    {
                        continue;
                    }
                    var v = new Vector3[count];
                    for (int k = 0; k < count; k++)
                    {
                        v[k] = new Vector3(float.Parse(f[72 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(f[73 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(f[74 + k * 3].Trim(), System.Globalization.CultureInfo.InvariantCulture));
                    }
                    log.LogInfo($"[qol] arriving in {target} through {map}'s door (entity {i}): appear at {v[1]}, walk to {v[2]}");
                    return v;
                }
            }
            return null;
        }

        // Event8's talk after the slides is cut: its first step is ChangeParty({1}) (Kabbu alone); refusing it stops the
        // scene, and the next frame the mod ends it as its own end does.
        private static bool event8Cut;

        private static bool BeforeChangeParty(int[] ids, bool fromscratch, bool destroyoldentity)
        {
            MainManager mm = MainManager.instance;
            if (randomizerOn == null || !randomizerOn() || mm == null || MainManager.map == null
                || MainManager.lastevent != 8 || !mm.inevent || MainManager.map.mapid.ToString() != OpeningMap || mm.flags[GameFlags.PermitEvent]
                || ids == null || ids.Length != 1 || ids[0] != 1 || !fromscratch || destroyoldentity || MainManager.events == null)
            {
                return true;
            }
            MainManager.events.StopCoroutine("Event8");
            event8Cut = true;
            log.LogInfo("[qol] Event8's talk after the slides cut: Kabbu-alone party refused, the scene stopped");
            return false;
        }

        // The cut before the slides: their first step is the black backdrop, NewSolidColor("back").
        private static void BeforeSolidColor(string name)
        {
            MainManager mm = MainManager.instance;
            if (name != "back" || randomizerOn == null || !randomizerOn() || mm == null || MainManager.map == null
                || MainManager.lastevent != 8 || !mm.inevent || MainManager.map.mapid.ToString() != OpeningMap || mm.flags[GameFlags.PermitEvent]
                || MainManager.events == null || event8Cut)
            {
                return;
            }
            MainManager.events.StopCoroutine("Event8");
            event8Cut = true;
            log.LogInfo("[qol] Event8 cut before its slides: the scene stopped");
        }

        private static void EndEvent8()
        {
            MainManager mm = MainManager.instance;
            Transform back = MainManager.GUICamera == null ? null : MainManager.GUICamera.transform.Find("back");
            // A start elsewhere keeps the slides' black backdrop until the new map has loaded, so the opening map isn't seen.
            if (back != null && (TestStartSet || Seeded.HasValue))
            {
                heldBack = back.gameObject;
                heldSince = Time.realtimeSinceStartup;
            }
            else if (back != null)
            {
                UnityEngine.Object.Destroy(back.gameObject);
            }
            mm.hud[0].transform.parent.gameObject.SetActive(true);
            MainManager.ResetCamera();
            // The party change waits for the next frame, behind the black screen: before EndEvent, FixEntities met a
            // character being replaced (NullReferenceException).
            partyThenFade = !TestStartSet && MainManager.map != null && MainManager.map.mapid.ToString() == OpeningMap;
            if (Seeded.HasValue && !TestStartSet)
            {
                // Leaving for the seed's start: silence until that map starts its own music.
                MainManager.FadeMusic(0.05f);
            }
            else if (!TestStartSet)
            {
                MainManager.ChangeMusic(Resources.Load<AudioClip>("Audio/Music/Inside0"));
                MainManager.music[0].clip = Resources.Load<AudioClip>("Audio/Musics/Field0");
                MainManager.music[0].volume = 0f;
                MainManager.music[0].Play();
            }
            endEvent?.Invoke(null, null);
            if (TestStartSet)
            {
                startPending = true; // at once, still behind the backdrop
            }
            else if (Seeded.HasValue)
            {
                startPending = !transferring; // after the party is set; the transfer fades in
            }
            else if (!partyThenFade)
            {
                MainManager.PlayTransition(1, 0, 0.02f, Color.black);
            }
            log.LogInfo($"[qol] Event8 ended the game's way; inevent={mm.inevent}");
        }

        private static bool partyThenFade;

        private static int partySetFrame = -10;

        private static void PartyThenFade()
        {
            partyThenFade = false;
            partySetFrame = Time.frameCount;
            MainManager mm = MainManager.instance;
            EntityControl four = MainManager.GetEntity(4);
            if (four != null && mm.playerdata != null)
            {
                SetOpeningParty(four.transform.position + Vector3.left * 2.5f);
                if (mm.camtarget != null)
                {
                    MainManager.MainCamera.transform.position = mm.camtarget.position + mm.camoffset;
                }
            }
            else
            {
                log.LogWarning("[qol] opening: entity 4 not found; the party stays where it is");
            }
            // A seed start's transfer fades in on arrival; fading in here would destroy the fade it waits on.
            if (startPending && Seeded.HasValue)
            {
                log.LogInfo("[qol] opening party set; the fade-in is left to the seed start's transfer");
                return;
            }
            MainManager.PlayTransition(1, 0, 0.02f, Color.black);
        }

        // The reshuffle choice (prompt target -199) moves to the front, in the map's dialogue table, once per map load.
        private static MapControl reorderedMap;

        private static void RerollFirst(MapControl map)
        {
            if (map.dialogues == null)
            {
                return;
            }
            for (int line = 0; line < map.dialogues.Length; line++)
            {
                string text = map.dialogues[line];
                if (text == null || !text.Contains(",-199,"))
                {
                    continue;
                }
                int at = text.IndexOf("|prompt,map,", StringComparison.Ordinal);
                while (at >= 0)
                {
                    int end = text.IndexOf('|', at + 1);
                    if (end < 0)
                    {
                        break;
                    }
                    string[] f = text.Substring(at + 1, end - at - 1).Split(',');
                    int n;
                    if (f.Length >= 4 && int.TryParse(f[3], out n) && f.Length >= 4 + 2 * n)
                    {
                        int k = Array.IndexOf(f, "-199", 4, n) - 4;
                        if (k > 0)
                        {
                            var targets = f.Skip(4).Take(n).ToList();
                            var texts = f.Skip(4 + n).Take(n).ToList();
                            string target = targets[k], label = texts[k];
                            targets.RemoveAt(k);
                            texts.RemoveAt(k);
                            targets.Insert(0, target);
                            texts.Insert(0, label);
                            string command = string.Join(",", f.Take(4).Concat(targets).Concat(texts).Concat(f.Skip(4 + 2 * n)).ToArray());
                            text = text.Substring(0, at + 1) + command + text.Substring(end);
                            end = at + 1 + command.Length;
                            log.LogInfo($"[qol] {map.mapid} line {line}: the reshuffle choice moved to the top of its prompt");
                        }
                    }
                    at = text.IndexOf("|prompt,map,", end, StringComparison.Ordinal);
                }
                map.dialogues[line] = text;
            }
        }

        // Vi and Kabbu (or the one starting member) set before the fade-in, so the first frame already has the right party.
        private static void SetOpeningParty(Vector3 at)
        {
            MainManager mm = MainManager.instance;
            MainManager.ChangeParty(new[] { 0, 1 }, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            MainManager.ResetCamera();
            // ResetCamera aims at MainManager.player, which may be the old leader Unity destroys only at frame end.
            if (mm.playerdata.Length > 0 && mm.playerdata[0].entity != null)
            {
                mm.camtarget = mm.playerdata[0].entity.transform;
            }
        }

        private static void RunOpening()
        {
            MainManager mm = MainManager.instance;
            // Where the player already is: placing the party anywhere else snapped back a player walking during the fade-in.
            SetOpeningParty(MainManager.player.transform.position);
            mm.items[0].Add(0);
            foreach (string name in new[] { "Beee", "blockingbox" })
            {
                GameObject thing = GameObject.Find(name);
                if (thing != null)
                {
                    UnityEngine.Object.Destroy(thing);
                }
                else
                {
                    log.LogInfo($"[qol] opening: no {name} (Event8 cut before making it)");
                }
            }
            // The building's own entity numbers, only there: elsewhere they're other things.
            bool inBuilding = MainManager.map.mapid.ToString() == OpeningMap;
            EntityControl exit = inBuilding ? MainManager.GetEntity(2) : null;
            if (exit != null && exit.npcdata != null)
            {
                exit.gameObject.SetActive(true);
                exit.npcdata.vectordata[4] = MainManager.defaultcamoffset;
                exit.npcdata.vectordata[5] = MainManager.defaultcamangle;
            }
            EntityControl eleven = inBuilding ? MainManager.GetEntity(11) : null;
            if (eleven != null)
            {
                eleven.animstate = 0;
            }
            EntityControl trigger = inBuilding ? MainManager.GetEntity(9) : null;
            if (trigger != null && trigger.name == "EventTrigger")
            {
                trigger.gameObject.SetActive(false); // gone as on a reload with flag 15 (its limit)
            }
            // The scene walks Maki out and destroys him; flag 15 (his limit) only hides him from the next map load.
            EntityControl maki = inBuilding ? MainManager.GetEntity(4) : null;
            if (maki != null && maki.name == "Maki")
            {
                UnityEngine.Object.Destroy(maki.gameObject);
            }
            mm.flags[GameFlags.PermitEvent] = true;
            mm.boardquests[1].Insert(0, 11);
            HoldUps.FoundAt(OpeningLocation, "the opening's gift (location 1)");
            log.LogInfo($"[qol] opening done without Event16 on {MainManager.map.mapid}: party {string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())}, "
                + $"characters {mm.playerdata.Count(p => p.entity != null)}, exit {(exit != null ? "active " + exit.gameObject.activeSelf : inBuilding ? "NOT found" : "not here")}, "
                + $"Maki {(maki != null && maki.name == "Maki" ? "removed" : inBuilding ? "NOT found" : "not here")}, flag 15 {mm.flags[GameFlags.PermitEvent]}");
        }
    }
}
