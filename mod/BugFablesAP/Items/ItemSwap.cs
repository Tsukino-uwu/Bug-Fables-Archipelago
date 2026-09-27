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
    internal static class ItemSwap
    {
        private const string TutorialText = "|tail,null||destroydescbox||blank||boxstyle,4|";

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

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
            descWindowField = AccessTools.Field(typeof(NPCControl), "descwindow");
            // World pickups don't use |giveitem|: CheckItem hands SetText a text ending in |additemtoss,<kind>,var,0|.
            harmony.Patch(setText, prefix: new HarmonyMethod(typeof(ItemSwap), nameof(PickupPrefix)));
            harmony.Patch(setText, prefix: new HarmonyMethod(typeof(ItemSwap), nameof(BerryPrefix)));
            MethodInfo updateItem = AccessTools.Method(typeof(EntityControl), nameof(EntityControl.UpdateItem));
            if (updateItem != null)
            {
                harmony.Patch(updateItem, postfix: new HarmonyMethod(typeof(ItemSwap), nameof(AfterUpdateItem)));
            }
            else
            {
                log.LogWarning("[swap] EntityControl.UpdateItem not found: a pickup the game redraws shows its own item until the ground swap.");
            }
        }

        // The one place the game redraws an item entity's own sprite: put the seed's item, its lift and its backdrop back
        // in the same call, so no frame shows the vanilla look (houses redraw their pickups on the way in).
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

        // Behind a location's item, on the ground or a shelf: the pickup's own starburst, smaller, in its class
        // colour, so its importance shows before it's taken. Null clears it.
        private const string MarkName = "apback";
        // Big enough to show round the item, the item and it raised together so its bottom stays near the item's old base
        // (items are lifted half their height; lower, the counter hid it), and close behind (the hold-up's 0.2 slid
        // sideways seen at an angle). Dev `mark` tunes them.
        internal static float MarkScale = 0.6f, MarkRaise = 0.3f;
        private const float MarkBehind = 0.05f;

        internal static void Mark(EntityControl entity, Color? color)
        {
            Transform shown = entity?.sprite?.transform;
            if (shown == null)
            {
                return;
            }
            if (color != null && entity.npcdata != null && DevClasses.TryGetValue(entity.npcdata.name, out int devClass))
            {
                color = Hex(ClassColors[devClass]);
            }
            Transform mark = shown.Find(MarkName);
            // The item's own lift, as the game sets it (half its height), plus the raise while marked.
            Sprite item = entity.sprite.sprite;
            if (entity.spritetransform != null && item != null)
            {
                var lift = new Vector3(0f, item.bounds.extents.y + (color == null ? 0f : MarkRaise), entity.spritetransform.localPosition.z);
                if (entity.spritetransform.localPosition != lift)
                {
                    entity.spritetransform.localPosition = lift;
                }
            }
            if (color == null)
            {
                if (mark != null)
                {
                    UnityEngine.Object.Destroy(mark.gameObject);
                }
                return;
            }
            if (mark == null)
            {
                SpriteRenderer back = MainManager.NewSpriteObject(MarkName, new Vector3(0f, 0f, MarkBehind), Vector3.zero, shown,
                    MainManager.guisprites[85], entity.sprite.material);
                back.transform.localScale = Vector3.one * MarkScale;
                back.gameObject.layer = shown.gameObject.layer;
                mark = back.transform;
            }
            // Kept in step after a hot reload, which leaves the old plugin's marks at their old size.
            var at = new Vector3(0f, 0f, MarkBehind);
            if (mark.localScale != Vector3.one * MarkScale || mark.localPosition != at)
            {
                mark.localScale = Vector3.one * MarkScale;
                mark.localPosition = at;
            }
            SpriteRenderer renderer = mark.GetComponent<SpriteRenderer>();
            if (renderer.material.color != color.Value)
            {
                renderer.material.color = color.Value;
            }
        }

        // The class colour of the item at this location, yours included, else null.
        internal static Color? MarkColorOf(long at)
        {
            ScoutedItemInfo info = null;
            connection?.Scouts?.TryGetValue(at, out info);
            return info != null && (QualityOfLife.ItemBackgrounds?.Value ?? true) ? MarkColor(info) : (Color?)null;
        }

        // The starburst colours by class: progression, useful, trap, filler. Archipelago's own, and Rarity's (a loot game's
        // ladder, which tells all four apart side by side). Dev `markcolor` changes the one in use.
        internal static readonly int[] ArchipelagoColors = { 0xAF99EF, 0x6D8BE8, 0xFA8072, 0x00EEEE };
        internal static readonly int[] RarityColors = { 0xB36BE8, 0x4A90E8, 0xE03C3C, 0x4CC94C };
        internal static int[] ClassColors => QualityOfLife.RarityColors ? RarityColors : ArchipelagoColors;
        // Item colors off: the game's own starburst colours by kind (NPCControl's pickup): an item, a key item, a medal.
        private static readonly Color GameItem = new Color(0f, 0.7f, 0.7f), GameKey = new Color(1f, 0.3f, 0.4f), GameMedal = new Color(1f, 0.5f, 0f);
        // Dev (console `markclass`): a marked entity, by name, drawn as a class (index in ClassColors) to compare them.
        internal static readonly Dictionary<string, int> DevClasses = new Dictionary<string, int>();

        private static Color ClassColor(ItemFlags flags) =>
            !QualityOfLife.ApColors ? GameItem
            : Hex(ClassColors[(flags & ItemFlags.Advancement) != 0 ? 0 : (flags & ItemFlags.NeverExclude) != 0 ? 1
                : (flags & ItemFlags.Trap) != 0 ? 2 : 3]);

        // A received item's starburst: its class colour while Item colors is on, else null (the game's own by kind).
        internal static Color? StarburstColor(ItemFlags flags) => QualityOfLife.ApColors ? ClassColor(flags) : (Color?)null;

        // A location's starburst: its class colour, or with Item colors off the game's colour for a Bug Fables item's kind.
        private static Color MarkColor(ScoutedItemInfo info)
        {
            if (QualityOfLife.ApColors || !IsOurs(info))
            {
                return ClassColor(info.Flags);
            }
            int kind = KindOf(info);
            return kind == ItemIds.MedalKind ? GameMedal : kind == ItemIds.KeyItemKind ? GameKey : GameItem;
        }

        // Dev (console `shelflook`): a location drawn with another sprite, to compare looks where they'll be seen.
        internal static readonly Dictionary<long, Sprite> DevLooks = new Dictionary<long, Sprite>();

        internal static void LookOf(long at, out string name, out Sprite sprite, out string description)
        {
            ScoutedItemInfo info = Describe(at, out name, out sprite, out _);
            if (DevLooks.TryGetValue(at, out Sprite look))
            {
                sprite = look;
            }
            // A shop's lines paste the name in after wrapping them, so a long one runs off the box: another player's item
            // is named alone, in its class colour (whose it is, the description says).
            if (info != null && info.Player.Slot != connection.OwnSlot)
            {
                name = ClassText(IsOurs(info) ? name.Substring(info.Player.Name.Length + 3) : info.ItemDisplayName, info.Flags) + Black;
            }
            description = "An Archipelago item.";
            if (info == null)
            {
                return;
            }
            if (IsOurs(info))
            {
                int kind = KindOf(info);
                int gameId = ItemIds.GameId(info.ItemId, kind);
                try
                {
                    description = kind == ItemIds.MedalKind ? MainManager.badgedata[gameId, 1]
                        : kind == ItemIds.MoneyKind ? gameId + " berries."
                        : kind == ItemIds.CrystalKind ? MainManager.menutext[112] + "."
                        : kind == ItemIds.MemberKind ? PartyMembers.Name(gameId) + " joins the party."
                        : kind == ItemIds.MoveKind ? MoveDescription(gameId)
                        : MainManager.itemdata[0, gameId, 2];
                }
                catch (IndexOutOfRangeException)
                {
                }
                if (info.Player.Slot != connection.OwnSlot)
                {
                    description = $"For {info.Player.Name}: " + description;
                }
            }
            else
            {
                description = $"{ClassWord(info.Flags)} item for {info.Player.Name} ({info.ItemGame}).";
            }
        }

        internal static void DescribeOurs(long itemId, int kind, out string name, out Sprite sprite, out Color? color)
        {
            {
                int gameId = ItemIds.GameId(itemId, kind);
                if (kind == ItemIds.MemberKind)
                {
                    // The pause menu's party icon and the member's own colour.
                    name = PartyMembers.Name(gameId);
                    sprite = MemberSprite(gameId);
                    color = MainManager.instance.charcolor[gameId];
                    return;
                }
                if (kind == ItemIds.MoveKind)
                {
                    // An attack shows its member's icon and colour; Jump is the whole party's, so the Archipelago icon.
                    name = FieldMoves.Name(gameId);
                    bool attack = gameId >= 0 && gameId <= 2;
                    sprite = attack ? MemberSprite(gameId) : ApIcon.Get();
                    color = attack ? MainManager.instance.charcolor[gameId] : (Color?)null;
                    return;
                }
                bool medal = kind == ItemIds.MedalKind;
                bool money = kind == ItemIds.MoneyKind;
                bool crystal = kind == ItemIds.CrystalKind;
                sprite = crystal ? MainManager.guisprites[83] : money ? ItemIds.BerrySprite(gameId) : MainManager.GetItemSprite(medal, gameId);
                name = crystal ? MainManager.menutext[112] : money ? gameId + " Berries"
                    : medal ? MainManager.GetBadgeName(gameId) : MainManager.itemdata[0, gameId, 0];
                color = medal ? new Color(1f, 0.5f, 0f)
                    : kind == ItemIds.KeyItemKind ? new Color(1f, 0.3f, 0.4f)
                    : new Color(0f, 0.7f, 0.7f);
            }
        }

        internal static string MoveDescription(int id) =>
            id == FieldMoves.Jump ? "The whole party can jump." : PartyMembers.Name(id) + " can use " + FieldMoves.Name(id) + ".";

        private static readonly Sprite[] memberSprites = new Sprite[3];

        // The pause menu's party icon is drawn far larger than an item: a copy scaled to an item sprite's size.
        internal static Sprite MemberSprite(int id)
        {
            Sprite icon = MainManager.guisprites[94 + id];
            if (id < 0 || id >= memberSprites.Length || icon == null)
            {
                return icon;
            }
            if (memberSprites[id] == null)
            {
                Vector3 item = MainManager.itemsprites[0, 0].bounds.size;
                Vector3 own = icon.bounds.size;
                float scale = Mathf.Max(own.x, own.y) / Mathf.Max(item.x, item.y);
                Rect rect = icon.packed ? icon.textureRect : icon.rect;
                memberSprites[id] = Sprite.Create(icon.texture, rect, new Vector2(icon.pivot.x / icon.rect.width, icon.pivot.y / icon.rect.height),
                    icon.pixelsPerUnit * scale);
                log.LogInfo($"[swap] {PartyMembers.Name(id)}'s icon ({own.x:0.00} x {own.y:0.00}) scaled by 1/{scale:0.00} to an item's size");
            }
            return memberSprites[id];
        }

        // A hold-up without a pickup runs Giveitem on a key item stand-in (key items have no bag limit), and the
        // stand-ins show the chosen item instead. Its follow-up line is the empty one QualityOfLife answers.
        private const int StandIn = 0;
        internal const int EmptyLine = -90000;

        // An item's itemdata[0, id, 3], a medal's badgedata[id, 6]; null for berries, which keep the default; none for a member.
        internal static string ArticleOf(long itemId, int kind)
        {
            int gameId = ItemIds.GameId(itemId, kind);
            try
            {
                return kind == ItemIds.MedalKind ? MainManager.badgedata[gameId, 6]
                    : kind == ItemIds.MemberKind || kind == ItemIds.MoveKind ? ""
                    : kind == ItemIds.MoneyKind || kind == ItemIds.CrystalKind ? null
                    : MainManager.itemdata[0, gameId, 3];
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        internal static void ShowFoundAt(long at)
        {
            pendingBerries = at;
            StartHoldUp();
        }

        // Another player's item: "<player>'s <item>", in Archipelago's colours when Item colors is on.
        private static bool ForOther(ScoutedItemInfo info, ref string name)
        {
            if (info == null || info.Player.Slot == connection.OwnSlot)
            {
                return false;
            }
            string item = IsOurs(info) ? name.Substring(info.Player.Name.Length + 3) : info.ItemDisplayName;
            name = PlayerText(info.Player.Name) + Black + "'s " + ClassText(item, info.Flags);
            return true;
        }

        // For the "You got" line, which wraps the name in |color,1|...|color,0|: another player's name, and an item by class.
        internal static string PlayerText(string player)
        {
            if (!QualityOfLife.ApColors)
            {
                return player;
            }
            HoldUps.AddApColors();
            return $"|color,{HoldUps.ApBase + HoldUps.Player}|{player}";
        }

        internal static string ClassText(string item, ItemFlags flags)
        {
            if (!QualityOfLife.ApColors)
            {
                return item;
            }
            HoldUps.AddApColors();
            int shade = (flags & ItemFlags.Advancement) != 0 ? HoldUps.Progression
                : (flags & ItemFlags.NeverExclude) != 0 ? HoldUps.Useful
                : (flags & ItemFlags.Trap) != 0 ? HoldUps.Trap
                : HoldUps.Filler;
            if (QualityOfLife.RarityColors)
            {
                shade += HoldUps.RarityOffset;
            }
            return $"|color,{HoldUps.ApBase + shade}|{item}";
        }

        // With Item colors off, the whole name stays in the game's red, as it was.
        internal static string FromText(string item, ItemFlags flags, string player) =>
            ClassText(item, flags) + Black + " from " + PlayerText(player);

        private static string Black => QualityOfLife.ApColors ? "|color,0|" : "";

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

        // Turns a location pickup's add into |additemtoss,3,...| (crystal-berry kind: adds nothing, closes the box
        // the same way); the |flag,...| before it still marks the pickup taken, so the check is sent.
        public static void PickupPrefix(ref string text, NPCControl caller)
        {
            if (caller == null || caller.objecttype != NPCControl.ObjectTypes.Item || text == null || caller.entity == null)
            {
                return;
            }
            int kind = caller.entity.animid;
            string add = "|additemtoss," + kind + ",var,0|";
            if (kind < 0 || kind > 3 || !text.Contains(add))
            {
                return;
            }
            long at = FindPickup(caller);
            if (at < 0)
            {
                return;
            }
            bool respawning = connection.LocationPickups[at].Regional >= 0;
            if (respawning)
            {
                // Once its check is done, a respawning pickup is the game's own again.
                if (connection.IsDone(at))
                {
                    log.LogInfo($"[swap] location {at}: respawning pickup on {MapName()}, check already done: vanilla item");
                    return;
                }
                // The game marks nothing LocationChecks could read later, so its check goes out now.
                connection.QueueRespawnCheck(at, MainManager.instance.flagstring[ItemReceiver.SeedSlot]);
            }
            ScoutedItemInfo info = Describe(at, out string name, out Sprite sprite, out Color? color);
            Mark(caller.entity, null);
            string article = info != null && IsOurs(info) ? ArticleOf(info.ItemId, KindOf(info)) : null;
            // "You found |string,1| ...": the seed item's own article, none for a member or another player's item.
            bool other = ForOther(info, ref name);
            if (other || article == "")
            {
                text = text.Replace(ArticleSlot, "");
            }
            // The pickup line ends its name in the game's red ("...|string,0||color,1|!"); after a name in the Item colors
            // that "!" looked stray, so it ends in black as the gift line does.
            if (other && QualityOfLife.ApColors)
            {
                text = text.Replace(NameThenRed, NameThenBlack);
            }
            else if (article != null)
            {
                MainManager.instance.flagstring[1] = article;
            }
            MainManager.instance.flagstring[0] = name;
            SpriteRenderer held = caller.entity.sprite;
            if (sprite != null && held != null)
            {
                held.sprite = sprite;
            }
            Transform back = held == null ? null : held.transform.Find("back");
            SpriteRenderer backRenderer = back == null ? null : back.GetComponent<SpriteRenderer>();
            if (color.HasValue && backRenderer != null)
            {
                backRenderer.material.color = color.Value;
            }
            // Replace the vanilla box at once: DestroyDescWindow would shrink it out while the new one grows.
            if (descWindowField != null && descWindowField.GetValue(caller) is DialogueAnim box && box != null)
            {
                UnityEngine.Object.Destroy(box.gameObject);
                descWindowField.SetValue(caller, null);
            }
            ShowOwnDescription(caller, info);
            text = text.Replace(add, "|additemtoss,3,var,0|");
            if (kind == 3)
            {
                // The berry's pickup code already marked it taken and raised the count: undo the count, keep the mark.
                MainManager.instance.flagvar[14]--;
                text = text.Replace(FirstBerryTutorial + "|break|", "").Replace(FirstBerryTutorial, "");
                ShowAsSprite(caller.entity, sprite);
            }
            text = text.Replace(FirstMedalTutorial + "|break|", "").Replace(FirstMedalTutorial, "");
            log.LogInfo($"[swap] location {at}: pickup (kind {kind}, id {caller.entity.animstate}, flag {caller.activationflag}) "
                + $"on {MapName()} is a location; showing '{name}'" + (info == null ? " (not scouted yet)" : ""));
        }

        internal static void TickGround()
        {
            if (Time.frameCount % 15 != 0 || connection == null || !randomizerOn())
            {
                return;
            }
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            MapControl map = MainManager.map;
            if (pickups == null || map == null)
            {
                return;
            }
            string mapName = map.mapid.ToString();
            NPCControl[] entities = null;
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                if (entry.Value.Map != mapName || (entry.Value.Regional >= 0 && connection.IsDone(entry.Key)))
                {
                    continue;
                }
                Describe(entry.Key, out _, out Sprite sprite, out _);
                if (sprite == null)
                {
                    continue;
                }
                entities = entities ?? map.GetComponentsInChildren<NPCControl>(true);
                foreach (NPCControl npc in entities)
                {
                    EntityControl entity = npc.entity;
                    if (npc.objecttype != NPCControl.ObjectTypes.Item || !IsPickup(entry.Value, npc)
                        || entity == null || entity.sprite == null)
                    {
                        continue;
                    }
                    // A crystal berry every time: the game shows its model again after it's hidden.
                    if (entity.animid == 3)
                    {
                        ShowAsSprite(entity, sprite);
                        Mark(entity, MarkColorOf(entry.Key));
                        continue;
                    }
                    if (entity.sprite.sprite != sprite)
                    {
                        entity.sprite.sprite = sprite;
                    }
                    // Sets the lift too (half the sprite's height, raised while marked).
                    Mark(entity, MarkColorOf(entry.Key));
                }
            }
        }

        private static bool IsPickup(ApConnection.Pickup pickup, NPCControl npc)
        {
            if (pickup.Berry >= 0)
            {
                return npc.entity != null && npc.entity.animid == 3 && npc.data != null && npc.data.Length > 0 && npc.data[0] == pickup.Berry;
            }
            if (pickup.Regional >= 0)
            {
                return npc.activationflag <= 0 && npc.regionalflag == pickup.Regional;
            }
            if (pickup.Event >= 0)
            {
                // A story pickup has no flag of its own: match the story event it starts (data[1]), not the entity name.
                return npc.data != null && npc.data.Length > 1 && npc.data[1] == pickup.Event;
            }
            return npc.activationflag >= 0 && npc.activationflag == pickup.Flag;
        }

        // |giveitem,-1,<amount>| (berries) never reaches the stand-ins, so at a berry location it becomes a hand-over
        // of item 0 marked as that location, which the stand-ins then swap.
        private static long pendingBerries = -1;

        public static void BerryPrefix(ref string text)
        {
            Dictionary<long, ApConnection.Give> gives = connection?.LocationGives;
            string map = MapName();
            if (text == null || gives == null || map == null || randomizerOn == null || !randomizerOn() || !text.Contains("|giveitem,-1,"))
            {
                return;
            }
            foreach (KeyValuePair<long, ApConnection.Give> entry in gives)
            {
                ApConnection.Give give = entry.Value;
                string token = "|giveitem,-1," + give.Item + ",";
                int at = text.IndexOf(token, StringComparison.Ordinal);
                if (give.Type != -1 || give.Map != map || at < 0)
                {
                    continue;
                }
                text = text.Substring(0, at) + "|giveitem,0,0," + text.Substring(at + token.Length);
                pendingBerries = entry.Key;
                log.LogInfo($"[swap] location {entry.Key}: berry reward ({give.Item}) on {map} turned into a hand-over to swap");
                return;
            }
        }

        private static long FindPickup(NPCControl caller)
        {
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            string map = MapName();
            if (!randomizerOn() || pickups == null || map == null)
            {
                return -1;
            }
            foreach (KeyValuePair<long, ApConnection.Pickup> entry in pickups)
            {
                if (entry.Value.Map == map && IsPickup(entry.Value, caller))
                {
                    return entry.Key;
                }
            }
            return -1;
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
            MainManager.instance.flagstring[0] = shownName;
            if (shownArticle != null)
            {
                MainManager.instance.flagstring[1] = shownArticle;
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
