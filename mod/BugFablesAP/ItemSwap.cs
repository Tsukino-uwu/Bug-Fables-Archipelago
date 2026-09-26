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

        // The one place the game redraws an item entity's own sprite: put the seed's item back in the same call, so
        // no frame shows the vanilla item (houses redraw their pickups on the way in).
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
                    if (__instance.spritetransform != null)
                    {
                        __instance.spritetransform.localPosition = new Vector2(0f, sprite.bounds.extents.y);
                    }
                }
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
                shownArticle = ArticleOf(info.ItemId, KindOf(info));
                if (info.Player.Slot != connection.OwnSlot)
                {
                    name = info.Player.Name + "'s " + name;
                }
            }
            else
            {
                // Archipelago's classification colours (NetUtils.py): progression, useful, trap, filler.
                name = info.Player.Name + "'s " + info.ItemDisplayName;
                color = (info.Flags & ItemFlags.Advancement) != 0 ? Hex(0xAF99EF)
                    : (info.Flags & ItemFlags.NeverExclude) != 0 ? Hex(0x6D8BE8)
                    : (info.Flags & ItemFlags.Trap) != 0 ? Hex(0xFA8072)
                    : Hex(0x00EEEE);
            }
            return info;
        }

        internal static void LookOf(long at, out string name, out Sprite sprite, out string description)
        {
            ScoutedItemInfo info = Describe(at, out name, out sprite, out _);
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
                        : MainManager.itemdata[0, gameId, 2];
                }
                catch (IndexOutOfRangeException)
                {
                }
            }
            else
            {
                description = $"An item for {info.Player.Name} ({info.ItemGame}).";
            }
        }

        internal static void DescribeOurs(long itemId, int kind, out string name, out Sprite sprite, out Color? color)
        {
            {
                int gameId = ItemIds.GameId(itemId, kind);
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

        // A hold-up without a pickup runs Giveitem on a key item stand-in (key items have no bag limit), and the
        // stand-ins show the chosen item instead. Its follow-up line is the empty one QualityOfLife answers.
        private const int StandIn = 0;
        internal const int EmptyLine = -90000;

        // An item's itemdata[0, id, 3], a medal's badgedata[id, 6]; null for berries, which keep the default.
        internal static string ArticleOf(long itemId, int kind)
        {
            int gameId = ItemIds.GameId(itemId, kind);
            try
            {
                return kind == ItemIds.MedalKind ? MainManager.badgedata[gameId, 6]
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
                        continue;
                    }
                    if (entity.sprite.sprite == sprite)
                    {
                        continue;
                    }
                    entity.sprite.sprite = sprite;
                    if (entity.spritetransform != null)
                    {
                        entity.spritetransform.localPosition = new Vector2(0f, sprite.bounds.extents.y);
                    }
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
            if (info == null || !IsOurs(info) || KindOf(info) == ItemIds.MoneyKind || KindOf(info) == ItemIds.CrystalKind)
            {
                return; // berries have no description box, as in the game's own money giveitem
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
