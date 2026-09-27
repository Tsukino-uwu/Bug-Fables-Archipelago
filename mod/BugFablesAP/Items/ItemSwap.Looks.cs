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
    internal static partial class ItemSwap
    {
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
    }
}
