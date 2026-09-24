using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BugFablesAP
{
    // Items are remote only: at a location, the game's own item must not reach the inventory, and the item-get
    // shows what the seed put there instead. The check itself is still sent from the location's flag
    // (LocationChecks), which the game sets as before.
    //
    // Every |giveitem| runs inside one long coroutine, MainManager.SetText (the 10-argument overload), so the
    // swap is a transpiler on its MoveNext. Measured in the IL on 2026-09-24 (ilspycmd, '<SetText>d__731'), the
    // Giveitem block runs, in order:
    //   caller.CreateDescWindow(int, int)   the description box, only when an NPC is involved (once in the method)
    //   GetItemSprite(bool, int)            the item's sprite (the only call in the method: the anchor)
    //   the starburst behind the sprite, coloured by item kind
    //   flagstring[0] = the item's name     read by the "You got" box
    //   List<int>.Add / List<int[]>.Add     into the inventory (items and key items / medals)
    //   "ItemGet"                           the sound
    //   flags[31] read                      whether to follow with the first-medal tutorial, which sets flag 31
    // Those calls are replaced by the static methods below. If any isn't found exactly once where expected,
    // nothing is patched and the log says so: the patch never guesses.
    internal static class ItemSwap
    {
        // Archipelago item ids are this plus the game's item id (apworld data_tables.py, ITEM_ID_BASE).
        private const long ItemIdBase = 7_710_000;
        private const string TutorialText = "|tail,null||destroydescbox||blank||boxstyle,4|";

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // One Giveitem at a time, on the game thread. Decide() fills these; the stand-ins read them.
        private static bool decided;
        private static bool decidedBadge;
        private static int decidedId;
        private static long location = -1;
        private static string shownName;
        private static Sprite shownSprite;
        private static Color? shownColor;
        private static bool swapped;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo setText = AccessTools.Method(typeof(MainManager), "SetText", new[]
            {
                typeof(string), typeof(int), typeof(float?), typeof(bool), typeof(bool), typeof(Vector3),
                typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl),
            });
            MethodInfo moveNext = setText == null ? null : AccessTools.EnumeratorMoveNext(setText);
            if (moveNext == null)
            {
                log.LogError("[swap] NOT installed: MainManager.SetText's coroutine wasn't found. Vanilla items would be given at locations.");
                return;
            }
            harmony = new Harmony(guid + ".swap." + DateTime.UtcNow.Ticks);
            harmony.Patch(moveNext, transpiler: new HarmonyMethod(typeof(ItemSwap), nameof(Transpile)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            MethodInfo descWindow = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), new[] { typeof(int), typeof(int) });
            MethodInfo getSprite = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetItemSprite));
            MethodInfo addItem = AccessTools.Method(typeof(List<int>), nameof(List<int>.Add));
            MethodInfo addBadge = AccessTools.Method(typeof(List<int[]>), nameof(List<int[]>.Add));
            FieldInfo flags = AccessTools.Field(typeof(MainManager), nameof(MainManager.flags));

            int[] descs = All(code, 0, code.Count, c => c.Calls(descWindow));
            int[] anchors = All(code, 0, code.Count, c => c.Calls(getSprite));
            int start = anchors.Length == 1 ? anchors[0] : -1;
            int end = start < 0 ? -1 : code.FindIndex(start, c => c.opcode == OpCodes.Ldstr && c.operand as string == "ItemGet");
            int[] itemAdds = end < 0 ? new int[0] : All(code, start, end, c => c.Calls(addItem));
            int[] badgeAdds = end < 0 ? new int[0] : All(code, start, end, c => c.Calls(addBadge));
            int tutorial = end < 0 ? -1 : code.FindIndex(end, c => c.opcode == OpCodes.Ldstr && c.operand as string == TutorialText);
            // flags[31]: ldfld flags, ldc.i4.s 31, ldelem.u1, between the sound and the tutorial text.
            int[] reads = tutorial < 0 ? new int[0] : All(code, end, tutorial, c => c.opcode == OpCodes.Ldelem_U1)
                .Where(i => i >= 2 && code[i - 2].LoadsField(flags) && code[i - 1].LoadsConstant(31)).ToArray();

            if (descs.Length != 1 || start < 0 || descs[0] > start || end < 0 || itemAdds.Length != 1
                || badgeAdds.Length != 1 || reads.Length != 1)
            {
                log.LogError($"[swap] NOT installed: expected one CreateDescWindow before one GetItemSprite (found {descs.Length} and "
                    + $"{anchors.Length}), \"ItemGet\" after it ({(end >= 0 ? "found" : "missing")}), one item add ({itemAdds.Length}), "
                    + $"one medal add ({badgeAdds.Length}) and one flags[31] read ({reads.Length}). The game's code differs from "
                    + "what was measured; vanilla items would be given at locations.");
                return code;
            }
            // Same stack shapes as the originals, so nothing around them changes. Labels stay on the instructions.
            Replace(code[descs[0]], nameof(DescWindow));
            Replace(code[start], nameof(ItemSprite));
            Replace(code[itemAdds[0]], nameof(AddItem));
            Replace(code[badgeAdds[0]], nameof(AddBadge));
            Replace(code[reads[0]], nameof(FirstMedalSeen));
            log.LogInfo($"[swap] installed in MainManager.SetText's Giveitem (instructions {descs[0]}, {start}, {itemAdds[0]}, "
                + $"{badgeAdds[0]}, {reads[0]})");
            return code;
        }

        private static int[] All(List<CodeInstruction> code, int from, int to, Func<CodeInstruction, bool> match)
        {
            return Enumerable.Range(from, Math.Max(0, to - from)).Where(i => match(code[i])).ToArray();
        }

        private static void Replace(CodeInstruction instruction, string method)
        {
            instruction.opcode = OpCodes.Call;
            instruction.operand = AccessTools.Method(typeof(ItemSwap), method);
        }

        // Stands in for caller.CreateDescWindow(type, id): type is 2 for a medal, 0 for items and key items.
        public static void DescWindow(NPCControl caller, int type, int id)
        {
            Decide(type == 2, id);
            if (location < 0)
            {
                caller.CreateDescWindow(type, id);
                return;
            }
            ScoutedItemInfo info = Scouted();
            if (info != null && IsOurs(info))
            {
                caller.CreateDescWindow(0, (int)(info.ItemId - ItemIdBase));
            }
            // Another game's item, or not scouted: no description. The game closes the box with a null check
            // (NPCControl.DestroyDescWindow), so a missing one is safe.
        }

        // Stands in for MainManager.GetItemSprite at the start of a Giveitem.
        public static Sprite ItemSprite(bool badge, int id)
        {
            if (!decided || decidedBadge != badge || decidedId != id)
            {
                Decide(badge, id); // no NPC: no description box came first
            }
            decided = false; // the next Giveitem decides afresh
            return location < 0 ? MainManager.GetItemSprite(badge, id) : shownSprite ?? MainManager.GetItemSprite(badge, id);
        }

        public static void AddItem(List<int> list, int id)
        {
            if (!TakeSwap("item " + id))
            {
                list.Add(id);
            }
        }

        public static void AddBadge(List<int[]> list, int[] entry)
        {
            if (!TakeSwap("medal " + (entry != null && entry.Length > 0 ? entry[0] : -1)))
            {
                list.Add(entry);
            }
        }

        // Stands in for reading flags[31] after the item-get. A swapped medal wasn't given, so the first-medal
        // tutorial mustn't run (it would also set flag 31, and skip the tutorial for the real first medal).
        public static bool FirstMedalSeen(bool[] flags, int index)
        {
            bool skip = swapped;
            swapped = false;
            return flags[index] || skip;
        }

        // Is this Giveitem a location's, and what's really there? Game thread.
        private static void Decide(bool badge, int id)
        {
            decided = true;
            decidedBadge = badge;
            decidedId = id;
            swapped = false;
            location = FindLocation(badge, id);
            shownSprite = null;
            shownColor = null;
            shownName = null;
            if (location < 0)
            {
                return;
            }
            ScoutedItemInfo info = Scouted();
            if (info == null)
            {
                shownName = "an Archipelago item";
            }
            else if (IsOurs(info))
            {
                int gameId = (int)(info.ItemId - ItemIdBase);
                shownSprite = MainManager.GetItemSprite(false, gameId);
                shownName = MainManager.itemdata[0, gameId, 0];
                if (info.Player.Slot != connection.OwnSlot)
                {
                    shownName = info.Player.Name + "'s " + shownName;
                }
                // The game's own starburst colours (the Giveitem switch): key item, item.
                int kind = 0;
                connection.ItemKinds?.TryGetValue(info.ItemId, out kind);
                shownColor = kind == 1 ? new Color(1f, 0.3f, 0.4f) : new Color(0f, 0.7f, 0.7f);
            }
            else
            {
                // Another game's item. The Archipelago icon replaces this sprite once it's in the mod; the colour is
                // Archipelago's for its classification (NetUtils.py): progression, useful, trap, filler.
                shownName = info.Player.Name + "'s " + info.ItemDisplayName;
                shownColor = (info.Flags & ItemFlags.Advancement) != 0 ? Hex(0xAF99EF)
                    : (info.Flags & ItemFlags.NeverExclude) != 0 ? Hex(0x6D8BE8)
                    : (info.Flags & ItemFlags.Trap) != 0 ? Hex(0xFA8072)
                    : Hex(0x00EEEE);
            }
            log.LogInfo($"[swap] location {location}: giveitem {(badge ? "medal" : "item")} {id} on {MapName()} is a location; showing '{shownName}'"
                + (info == null ? " (not scouted yet)" : ""));
        }

        // The Add stand-ins: at a location, keep the vanilla item out, name what's really there in the "You got" box
        // (it reads flagstring[0], which the game set to the vanilla name just before), and recolour the starburst.
        private static bool TakeSwap(string what)
        {
            if (location < 0)
            {
                return false;
            }
            MainManager.instance.flagstring[0] = shownName;
            if (shownColor.HasValue)
            {
                Recolour(shownColor.Value);
            }
            log.LogInfo($"[swap] location {location}: kept {what} out of the inventory");
            location = -1;
            swapped = true;
            return true;
        }

        // The starburst is the "back" child of the "tempitem" sprite the Giveitem just made (MainManager.cs, Giveitem).
        private static void Recolour(Color color)
        {
            Sprite sprite = shownSprite;
            foreach (SpriteRenderer item in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
            {
                if (item.name != "tempitem" || (sprite != null && item.sprite != sprite))
                {
                    continue;
                }
                Transform back = item.transform.Find("back");
                SpriteRenderer backRenderer = back == null ? null : back.GetComponent<SpriteRenderer>();
                if (backRenderer != null)
                {
                    backRenderer.material.color = color;
                    return;
                }
            }
            log.LogWarning("[swap] the item-get's starburst wasn't found; its colour stays the game's");
        }

        private static ScoutedItemInfo Scouted()
        {
            ScoutedItemInfo info = null;
            connection.Scouts?.TryGetValue(location, out info);
            return info;
        }

        private static bool IsOurs(ScoutedItemInfo info)
        {
            return info.ItemGame == ApConnection.Game && info.ItemId >= ItemIdBase;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }

        private static long FindLocation(bool badge, int id)
        {
            Dictionary<long, ApConnection.Give> gives = connection.LocationGives;
            string map = MapName();
            // A dropped connection keeps the rules in force: the tables stay from the last login.
            if (!randomizerOn() || gives == null || map == null)
            {
                return -1;
            }
            foreach (KeyValuePair<long, ApConnection.Give> entry in gives)
            {
                ApConnection.Give give = entry.Value;
                bool sameKind = badge ? give.Type == 2 : give.Type == 0 || give.Type == 1;
                if (sameKind && give.Item == id && give.Map == map)
                {
                    return entry.Key;
                }
            }
            return -1;
        }

        private static string MapName()
        {
            MapControl map = MainManager.map;
            return map == null ? null : map.mapid.ToString();
        }
    }
}
