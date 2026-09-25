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
        private static FieldInfo descWindowField;

        // NPCControl.CheckItem's first-berry tutorial (NPCControl.cs:5700), dropped for a swapped berry like the medal one.
        private const string FirstBerryTutorial = "|flag,108,true||tail,null||center,true||destroydescbox||goto,-88,break,end|";

        // A crystal berry is drawn as a spinning 3D model, not its sprite (NPCControl.cs:946): to show the seed's item,
        // hide the model and show the sprite.
        private static void ShowAsSprite(EntityControl entity, Sprite sprite)
        {
            if (entity == null || sprite == null || entity.sprite == null)
            {
                return;
            }
            // The berry model hangs under the sprite (AddModel, EntityControl.cs:2684), and every frame the game makes that
            // first child active exactly when the sprite is enabled (EntityControl.cs:2781-2786). Showing the item needs the
            // sprite enabled, so switching the model's object off never held: two tries failed (the user, 2026-09-25) until
            // the dev console's tree and that line showed why. The game only toggles the object, never its renderers, so
            // the renderers are what the swap switches off.
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
            // A crystal berry spot is set up for its 3D model: its sprite centred on the ground and the whole entity
            // spinning (NPCControl.cs:945-949). As a flat item sprite it sat half in the ground (the user's screenshot,
            // 2026-09-25): lift it by half its height, as the game does for items (EntityControl.cs:3241), and stop the spin.
            if (entity.spritetransform != null)
            {
                entity.spritetransform.localPosition = new Vector2(0f, sprite.bounds.extents.y);
                // The spin turns the sprite itself (EntityControl.cs:3943-3945), so it may have stopped edge-on.
                entity.spritetransform.localEulerAngles = Vector3.zero;
            }
            entity.spin = Vector3.zero;
        }

        // NPCControl.CheckItem's first-medal tutorial, put after the add (NPCControl.cs:5673).
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
            // Items lying in the world (and buried ones, which pop out as the same kind of entity) don't use
            // |giveitem|: NPCControl.CheckItem sets up the item-get itself, then hands SetText a text ending in
            // |flag,<activationflag>,true||additemtoss,<kind>,var,0|. A prefix sees that text before it runs.
            descWindowField = AccessTools.Field(typeof(NPCControl), "descwindow");
            harmony.Patch(setText, prefix: new HarmonyMethod(typeof(ItemSwap), nameof(PickupPrefix)));
            harmony.Patch(setText, prefix: new HarmonyMethod(typeof(ItemSwap), nameof(BerryPrefix)));
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
            ShowOwnDescription(caller, Scouted());
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
            ScoutedItemInfo info = Describe(location, out shownName, out shownSprite, out shownColor);
            log.LogInfo($"[swap] location {location}: giveitem {(badge ? "medal" : "item")} {id} on {MapName()} is a location; showing '{shownName}'"
                + (info == null ? " (not scouted yet)" : ""));
        }

        // What's really at a location: the name for the "You got" box, our own sprite when it's a Bug Fables item,
        // and the starburst colour. Returns the scout, or null when it hasn't arrived.
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
                int kind = KindOf(info);
                int gameId = ItemIds.GameId(info.ItemId, kind);
                bool medal = kind == ItemIds.MedalKind;
                bool money = kind == ItemIds.MoneyKind;
                bool crystal = kind == ItemIds.CrystalKind;
                // A crystal berry: the game's own name for it and its berry icon (NPCControl.cs:5657, :4201).
                sprite = crystal ? MainManager.guisprites[83] : money ? ItemIds.BerrySprite(gameId) : MainManager.GetItemSprite(medal, gameId);
                name = crystal ? MainManager.menutext[112] : money ? gameId + " Berries"
                    : medal ? MainManager.GetBadgeName(gameId) : MainManager.itemdata[0, gameId, 0];
                if (info.Player.Slot != connection.OwnSlot)
                {
                    name = info.Player.Name + "'s " + name;
                }
                // The game's own starburst colours (the Giveitem switch, NPCControl.CheckItem): medal, key item, item.
                color = medal ? new Color(1f, 0.5f, 0f)
                    : kind == ItemIds.KeyItemKind ? new Color(1f, 0.3f, 0.4f)
                    : new Color(0f, 0.7f, 0.7f);
            }
            else
            {
                // Another game's item. The Archipelago icon replaces this sprite once it's in the mod; the colour is
                // Archipelago's for its classification (NetUtils.py): progression, useful, trap, filler.
                name = info.Player.Name + "'s " + info.ItemDisplayName;
                color = (info.Flags & ItemFlags.Advancement) != 0 ? Hex(0xAF99EF)
                    : (info.Flags & ItemFlags.NeverExclude) != 0 ? Hex(0x6D8BE8)
                    : (info.Flags & ItemFlags.Trap) != 0 ? Hex(0xFA8072)
                    : Hex(0x00EEEE);
            }
            return info;
        }

        // Prefix on MainManager.SetText (10 arguments). Acts only on the item-get NPCControl.CheckItem starts for a
        // pickup that is one of this seed's locations: it shows what's really there, and turns the add into
        // |additemtoss,3,...|, the game's crystal-berry kind, which adds nothing to any list but closes the
        // description box and ends the text exactly as the item's own kind would (MainManager.cs:12517-12532).
        // The |flag,<activationflag>,true| before it is untouched, so the game still marks the pickup taken and
        // LocationChecks sends the check.
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
                // Once its check is done, a respawning pickup is the game's own again (the user, 2026-09-24).
                if (connection.IsDone(at))
                {
                    log.LogInfo($"[swap] location {at}: respawning pickup on {MapName()}, check already done: vanilla item");
                    return;
                }
                // Its check goes out now: the game marks nothing that LocationChecks could read later.
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
            // The vanilla description box is already up: replace it at once (DestroyDescWindow would shrink it out
            // over half a second while the new one grows).
            if (descWindowField != null && descWindowField.GetValue(caller) is DialogueAnim box && box != null)
            {
                UnityEngine.Object.Destroy(box.gameObject);
                descWindowField.SetValue(caller, null);
            }
            ShowOwnDescription(caller, info);
            text = text.Replace(add, "|additemtoss,3,var,0|");
            if (kind == 3)
            {
                // A crystal berry's pickup code already marked it taken (the check; it also keeps the berry gone) and
                // raised the count (NPCControl.cs:5656-5658) before this text: undo the count, keep the mark.
                MainManager.instance.flagvar[14]--;
                text = text.Replace(FirstBerryTutorial + "|break|", "").Replace(FirstBerryTutorial, "");
                ShowAsSprite(caller.entity, sprite);
            }
            // A swapped medal wasn't given, so the first-medal tutorial mustn't run (it would also set flag 31).
            text = text.Replace(FirstMedalTutorial + "|break|", "").Replace(FirstMedalTutorial, "");
            log.LogInfo($"[swap] location {at}: pickup (kind {kind}, id {caller.entity.animstate}, flag {caller.activationflag}) "
                + $"on {MapName()} is a location; showing '{name}'" + (info == null ? " (not scouted yet)" : ""));
        }

        // Pickups that are locations show the seed's item on the ground too, before they're touched (the user,
        // 2026-09-24: the ground still showed the vanilla medal). Four times a second, for this map's pickup
        // locations, the ground entity gets the Bug Fables sprite of what's really there, placed the way
        // EntityControl.UpdateItem places one (EntityControl.cs:3238-3241). The game only redraws an item's sprite
        // when its id changes (UpdateSprite, :4051), so it holds; this pass re-applies it if anything resets it.
        // Another game's item keeps the vanilla sprite until the Archipelago icon is in the mod.
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
                // A respawning pickup whose check is done shows its own item again.
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
                    // A crystal berry spot every time, not only when its sprite differs: the game shows its berry model
                    // again after the swap has hidden it, and a skip on "sprite already right" left the berry on top of
                    // the item for good (the dev console's tree, 2026-09-25: sprite items0_8 set, the one model active).
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

        // This pickup's own flag or, for a story pickup (no flag of its own), the story event it starts (data[1],
        // NPCControl.CheckItem's |event| chain). Matching by entity name missed the scene's own copy (2026-09-24).
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
                return npc.data != null && npc.data.Length > 1 && npc.data[1] == pickup.Event;
            }
            return npc.activationflag >= 0 && npc.activationflag == pickup.Flag;
        }

        // Berry rewards: |giveitem,-1,<amount>,...| is the same command as an item's, but its money branch never
        // reaches the calls the Giveitem stand-ins replace. So at a berry location the text is turned, just before it
        // runs, into a hand-over of item 0 (Crunchy Leaf) marked as that location; the stand-ins then show what's really
        // there and keep the Crunchy Leaf out, as for any gift. The line's other commands are untouched.
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
            // A dropped connection keeps the rules in force: the tables stay from the last login.
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

        private static int KindOf(ScoutedItemInfo info)
        {
            int kind = 0;
            connection.ItemKinds?.TryGetValue(info.ItemId, out kind);
            return kind;
        }

        // A Bug Fables item gets its own description box, the medal kind for a medal (NPCControl.CreateDescWindow:
        // type 2 reads badgedata, anything else itemdata). Another game's item, or one not scouted yet, gets none.
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
            // A berry reward turned into a hand-over of item 0 (BerryPrefix) is that location's.
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
