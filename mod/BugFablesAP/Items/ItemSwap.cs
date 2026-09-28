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
    // At a location, the game's own item never reaches the inventory and the item-get shows the seed's item instead.
    // Giveitem runs inside SetText's coroutine, so a transpiler replaces its desc box, sprite, adds and flags[31] read;
    // if any isn't found exactly once, nothing is patched.
    internal static partial class ItemSwap
    {
        private const string TutorialText = "|tail,null||destroydescbox||blank||boxstyle,4|";

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        private static bool decided;
        private static bool decidedBadge;
        private static int decidedId;
        private static long location = -1;
        private const long DisplayOnly = -2;
        private static string pendingName;
        private static Sprite pendingSprite;
        private static Color? pendingColor;
        private static string shownName;
        private static bool shownForOther;
        private static Sprite shownSprite;
        private static Color? shownColor;
        // Giveitem always uses the default article (menutext[125]); a picked-up item has its own.
        private static string shownArticle;
        private static string pendingArticle;
        private static bool swapped;
        private static FieldInfo descWindowField;

        private const string FirstBerryTutorial = "|flag,108,true||tail,null||center,true||destroydescbox||goto,-88,break,end|";

        // A crystal berry is drawn as a spinning 3D model, not its sprite: hide the model and show the sprite.
        private static void ShowAsSprite(EntityControl entity, Sprite sprite)
        {
            if (entity == null || sprite == null || entity.sprite == null)
            {
                return;
            }
            // The game re-activates the model's object every frame while the sprite is enabled, never its renderers,
            // so the renderers are what get switched off.
            if (entity.spritetransform != null)
            {
                foreach (Renderer r in entity.spritetransform.GetComponentsInChildren<Renderer>(true))
                {
                    if (r != entity.sprite && r.enabled)
                    {
                        r.enabled = false;
                    }
                }
            }
            entity.sprite.enabled = true;
            entity.sprite.sprite = sprite;
            // Set up for the model: lift the sprite by half its height, as for items, and stop the spin.
            if (entity.spritetransform != null)
            {
                entity.spritetransform.localPosition = new Vector2(0f, sprite.bounds.extents.y);
                entity.spritetransform.localEulerAngles = Vector3.zero;
            }
            entity.spin = Vector3.zero;
        }

        private const string FirstMedalTutorial = "|flag,31,true||tail,null||center,true||destroydescbox||goto,-32,break,end|";

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (!Hooks.Install(typeof(ItemSwap), "swap", "vanilla items would be given at locations"))
            {
                return;
            }
            descWindowField = AccessTools.Field(typeof(NPCControl), "descwindow");
            // World pickups don't use |giveitem|: CheckItem hands SetText a text ending in |additemtoss,<kind>,var,0|.
            // Two groups, installed in this order: the pickup prefix runs before the berry prefix.
            Hooks.Install(typeof(Pickups), "swap", "a pickup in the world would give its vanilla item");
            Hooks.Install(typeof(Berries), "swap", "berries at a location would be given as berries");
            Hooks.Install(typeof(Redraws), "swap", "a pickup the game redraws shows its own item until the ground swap");
        }

        // The one place the game redraws an item entity's own sprite: put the seed's item, its lift and its backdrop back
        // in the same call, so no frame shows the vanilla look (houses redraw their pickups on the way in).
        private static class Redraws
        {
            [HarmonyPatch(typeof(EntityControl), nameof(EntityControl.UpdateItem))]
            [HarmonyPostfix]
            private static void AfterUpdateItem(EntityControl __instance)
            {
                NPCControl npc = __instance.npcdata;
                Dictionary<long, ApConnection.Pickup> pickups = connection?.LocationPickups;
                if (npc == null || npc.objecttype != NPCControl.ObjectTypes.Item || pickups == null || MainManager.map == null
                    || __instance.sprite == null || randomizerOn == null || !randomizerOn())
                {
                    return;
                }
                string mapName = MainManager.map.mapid.ToString();
                foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
                {
                    if (entry.Value.Map != mapName || (entry.Value.Regional >= 0 && connection.IsDone(entry.Key)) || !IsPickup(entry.Value, npc))
                    {
                        continue;
                    }
                    Describe(entry.Key, out _, out Sprite sprite, out _);
                    if (sprite == null)
                    {
                        return;
                    }
                    if (__instance.animid == 3)
                    {
                        ShowAsSprite(__instance, sprite);
                    }
                    else
                    {
                        __instance.sprite.sprite = sprite;
                    }
                    // The lift and backdrop in the same call too, or the item shows at the game's height, then jumps.
                    Mark(__instance, MarkColorOf(entry.Key));
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(MainManager), "SetText", typeof(string), typeof(int), typeof(float?), typeof(bool), typeof(bool), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl))]
        [HarmonyPatch(MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditGiveitem, "swap");

        private static IEnumerable<CodeInstruction> EditGiveitem(List<CodeInstruction> code)
        {
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
            // Same stack shapes as the originals. Labels stay on the instructions.
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

        // type is 2 for a medal, 0 for items and key items.
        public static void DescWindow(NPCControl caller, int type, int id)
        {
            Decide(type == 2, id);
            if (location < 0)
            {
                caller.CreateDescWindow(type, id);
                return;
            }
            // No description for another game's item: DestroyDescWindow null-checks, so a missing box is safe.
            ShowOwnDescription(caller, Scouted());
        }

        public static Sprite ItemSprite(bool badge, int id)
        {
            if (!decided || decidedBadge != badge || decidedId != id)
            {
                Decide(badge, id); // no NPC: no description box came first
            }
            decided = false; // the next Giveitem decides afresh
            return location == -1 ? MainManager.GetItemSprite(badge, id) : shownSprite ?? MainManager.GetItemSprite(badge, id);
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

        // A swapped medal wasn't given: skip the first-medal tutorial, which would also set flag 31.
        public static bool FirstMedalSeen(bool[] flags, int index)
        {
            bool skip = swapped;
            swapped = false;
            return flags[index] || skip;
        }

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
            shownArticle = null;
            shownForOther = false;
            if (location == -1 && pendingName != null && !badge && id == StandIn)
            {
                location = DisplayOnly;
                shownName = pendingName;
                shownSprite = pendingSprite;
                shownColor = pendingColor;
                shownArticle = pendingArticle;
                pendingName = null;
                return;
            }
            if (location < 0)
            {
                return;
            }
            ScoutedItemInfo info = Describe(location, out shownName, out shownSprite, out shownColor);
            shownForOther = ForOther(info, ref shownName);
            if (!connection.IsDone(location))
            {
                ShownInScene.Add(location); // a done check's item only comes back as a replay, which keeps its box
            }
            log.LogInfo($"[swap] location {location}: giveitem {(badge ? "medal" : "item")} {id} on {MapName()} is a location; showing '{shownName}'"
                + (info == null ? " (not scouted yet)" : ""));
        }

        private static ScoutedItemInfo Describe(long at, out string name, out Sprite sprite, out Color? color)
        {
            name = null;
            sprite = null;
            color = null;
            ScoutedItemInfo info = null;
            connection.Scouts?.TryGetValue(at, out info);
            if (info == null)
            {
                name = "an Archipelago item";
            }
            else if (IsOurs(info))
            {
                DescribeOurs(info.ItemId, KindOf(info), out name, out sprite, out color);
                // With Item colors on, the starburst at pickup matches the backdrop it had on the ground or shelf.
                if (QualityOfLife.ApColors)
                {
                    color = ClassColor(info.Flags);
                }
                shownArticle = ArticleOf(info.ItemId, KindOf(info));
                if (info.Player.Slot != connection.OwnSlot)
                {
                    name = info.Player.Name + "'s " + name;
                    if (QualityOfLife.IconMode == "AllPlayers")
                    {
                        sprite = ApIcon.Get();
                        color = ClassColor(info.Flags);
                    }
                }
            }
            else
            {
                // Another game's item: the drawn Archipelago icon, on Archipelago's classification colours (NetUtils.py):
                // progression, useful, trap, filler.
                name = info.Player.Name + "'s " + info.ItemDisplayName;
                sprite = QualityOfLife.IconMode == "Off" ? null : ApIcon.Get();
                color = ClassColor(info.Flags);
            }
            return info;
        }

        private static string ClassWord(ItemFlags flags) =>
            (flags & ItemFlags.Advancement) != 0 ? "A progression"
            : (flags & ItemFlags.NeverExclude) != 0 ? "A useful"
            : (flags & ItemFlags.Trap) != 0 ? "A trap"
            : "A filler";

        internal static void ShowHeldUp(string name, Sprite sprite, Color? color, string article)
        {
            pendingArticle = article;
            pendingName = name;
            pendingSprite = sprite;
            pendingColor = color;
            StartHoldUp();
        }

        private static void StartHoldUp()
        {
            MainManager.instance.StartCoroutine(MainManager.SetText($"|giveitem,1,{StandIn},{EmptyLine},-1|", dialogue: true, Vector3.zero, null, null));
        }

        // "You got |string,1| |color,1||string,0|...": with no article the space goes too, for this one line (Giveitem
        // reads menutext[106] right after this), then the game's text is put back.
        private const int GotLine = 106;
        private const string ArticleSlot = "|string,1| ";
        private const string NameThenRed = "|string,0||color,1|", NameThenBlack = "|string,0||color,0|";

        // Another player's item, found here: "You found <player>'s <item>!", so it isn't taken for your own.
        private const string GotWords = "You got " + ArticleSlot;
        private const string FoundWords = "You found ";

        private static void ChangeLineOnce(string from, string to)
        {
            string line = MainManager.menutext[GotLine];
            if (line == null || !line.Contains(from))
            {
                return;
            }
            MainManager.menutext[GotLine] = line.Replace(from, to);
            MainManager.instance.StartCoroutine(RestoreLine(line));
        }

        private static System.Collections.IEnumerator RestoreLine(string line)
        {
            yield return null;
            MainManager.menutext[GotLine] = line;
        }

        // The "You got" box reads flagstring[0], which the game just set to the vanilla name.
        private static bool TakeSwap(string what)
        {
            if (location == -1)
            {
                return false;
            }
            MainManager.instance.flagstring[GameStrings.ItemName] = shownName;
            if (shownArticle != null)
            {
                MainManager.instance.flagstring[GameStrings.ItemArticle] = shownArticle;
            }
            if (shownForOther)
            {
                ChangeLineOnce(GotWords, FoundWords);
            }
            else if (shownArticle == "")
            {
                ChangeLineOnce(ArticleSlot, "");
            }
            if (shownColor.HasValue)
            {
                Recolour(shownColor.Value);
            }
            log.LogInfo(location == DisplayOnly ? $"[swap] held up '{shownName}' (display only)" : $"[swap] location {location}: kept {what} out of the inventory");
            location = -1;
            swapped = true;
            return true;
        }

        // The starburst is the "back" child of the "tempitem" sprite the Giveitem just made.
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

        private static int KindOf(ScoutedItemInfo info)
        {
            int kind = 0;
            connection.ItemKinds?.TryGetValue(info.ItemId, out kind);
            return kind;
        }

        private static void ShowOwnDescription(NPCControl caller, ScoutedItemInfo info)
        {
            if (info == null || !IsOurs(info) || KindOf(info) == ItemIds.MoneyKind || KindOf(info) == ItemIds.CrystalKind
                || KindOf(info) == ItemIds.MemberKind || KindOf(info) == ItemIds.MoveKind)
            {
                return; // berries have no description box, as in the game's own money giveitem; a member or move has no item row
            }
            int kind = KindOf(info);
            bool medal = kind == ItemIds.MedalKind;
            caller.CreateDescWindow(medal ? 2 : 0, ItemIds.GameId(info.ItemId, kind));
        }

        private static ScoutedItemInfo Scouted()
        {
            ScoutedItemInfo info = null;
            connection.Scouts?.TryGetValue(location, out info);
            return info;
        }

        private static bool IsOurs(ScoutedItemInfo info)
        {
            return info.ItemGame == ApConnection.Game && info.ItemId >= ItemIds.Base;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }

        // Checks whose item a scene just showed on screen: that item's arrival gets no second box (ItemReceiver).
        internal static readonly HashSet<long> ShownInScene = new HashSet<long>();

        private static long FindLocation(bool badge, int id)
        {
            Dictionary<long, ApConnection.Give> gives = connection.LocationGives;
            string map = MapName();
            // A dropped connection keeps the rules in force: the tables stay from the last login.
            if (!randomizerOn() || gives == null || map == null)
            {
                return -1;
            }
            if (pendingBerries >= 0 && !badge && id == 0)
            {
                long berries = pendingBerries;
                pendingBerries = -1;
                return berries;
            }
            foreach (KeyValuePair<long, ApConnection.Give> entry in gives)
            {
                ApConnection.Give give = entry.Value;
                bool sameKind = badge ? give.Type == 2 : give.Type == 0 || give.Type == 1;
                if (sameKind && give.Item == id && give.Map == map)
                {
                    return connection.LocationShops != null && connection.LocationShops.ContainsKey(entry.Key) ? ShopSwap.Buy(entry.Key) : entry.Key;
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
