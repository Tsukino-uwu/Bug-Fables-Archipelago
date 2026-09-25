using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Medal shops (the user, 2026-09-25). A medal shop is a counter of item entities: each slot is an NPCControl with
    // interacttype Shop, animid 2 (medal) and animstate the medal id, made by the shopkeeper's SetBadgeShop from
    // avaliablebadgepool (NPCControl.cs:1504-1580); the shopkeeper's dialogues[9].x is the badgeshops index. Looking at a
    // slot opens CreateDescWindow(shop), which reads the medal's name and description from badgedata[id, 0] and [id, 1]
    // (NPCControl.cs:4183-4228); interacting copies the name into the buy prompt's text and the price into flagvar[1]
    // (NPCControl.Interact, :4360-4372), and the shopkeeper's buy line gives the medal with giveitem, which ItemSwap swaps
    // as for a gift (the location's give). So for a slot whose medal is a location: the shelf shows the seed's item's
    // sprite, and while those two methods run the medal's name and description read as the seed's item.
    //
    // Shop prices (a Quality of life row: Normal, Half, Free) scale the medal table's price columns in memory, 5 for
    // berries and 7 for crystal berries (MainManager.cs:3474-3488), and put them back when the setting or the mod is off.
    //
    // Full stock from the start (the user, 2026-09-25). Every copy a shop will ever stock is a location, a medal the story
    // adds twice being two ("a 2nd copy is a 2nd check"). A shop's copies are its locations in id order. The mod owns the
    // stock: whenever the game rebuilds its shelf pool (UpdateShops, on every map start and after each purchase,
    // MainManager.cs:4087, MapControl.cs:343, NPCControl.cs:1528), badgeshops[shop] is first set to the copies not yet
    // done, as the game's own shoppool command writes it (MainManager.cs:11638-11657); what the story adds is trimmed there.
    // Which copies were bought is a bit per copy in the save, flagvar[7] for Merab's and [8] for Shades's (both unused by
    // the game's code and text, MEASURED.md), set when the swapped giveitem of a purchase runs; a copy is done once its
    // bit is set or the server has its check. So an offline purchase is kept by the save and sent on reconnecting.
    internal static class ShopSwap
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static string[,] originalPrices;
        private static string appliedPrices = "Normal";

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo desc = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.CreateDescWindow), new[] { typeof(bool) });
            MethodInfo interact = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.Interact), new[] { typeof(string) });
            if (desc == null || interact == null)
            {
                log.LogError($"[shop] NOT installed (CreateDescWindow {desc != null}, Interact {interact != null}): shop shelves show their own medals.");
                return;
            }
            harmony = new Harmony(guid + ".shop." + DateTime.UtcNow.Ticks);
            var before = new HarmonyMethod(typeof(ShopSwap), nameof(BeforeShow));
            var after = new HarmonyMethod(typeof(ShopSwap), nameof(AfterShow));
            harmony.Patch(desc, prefix: before, postfix: after);
            harmony.Patch(interact, prefix: new HarmonyMethod(typeof(ShopSwap), nameof(BeforeInteract)), postfix: after);
            MethodInfo shelf = AccessTools.Method(typeof(NPCControl), nameof(NPCControl.SetBadgeShop), new[] { typeof(bool) });
            if (shelf != null)
            {
                harmony.Patch(shelf, prefix: new HarmonyMethod(typeof(ShopSwap), nameof(BeforeShelf)), postfix: new HarmonyMethod(typeof(ShopSwap), nameof(AfterShelf)));
            }
            MethodInfo pool = AccessTools.Method(typeof(MainManager), nameof(MainManager.UpdateShops));
            if (pool == null)
            {
                log.LogError("[shop] UpdateShops not found: shops keep the game's own stock.");
            }
            else
            {
                harmony.Patch(pool, prefix: new HarmonyMethod(typeof(ShopSwap), nameof(BeforeUpdateShops)));
            }
            log.LogInfo("[shop] installed on NPCControl.CreateDescWindow, Interact and MainManager.UpdateShops");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            SetPrices("Normal");
        }

        // More medals on show at once (the user, 2026-09-25: a QoL thing): Merab's shelf 5 instead of 3 (a 6th past her last
        // spot was hard to reach), Shades's 4 instead of 2, spread wider than her two spots (1.35) (6 between them packed
        // them too close; 6 with one past each end, then back to 4 "a tiny bit" further apart, the user). The shelf has one
        // slot per entry of the shopkeeper's data, each at vectordata[j] (NPCControl.cs:1531-1534); before it's built, the
        // spots are laid out evenly around the middle of her first and last spot: {shop: (slots, spread)}, spread 1 filling
        // exactly from her first spot to her last, more reaching past them.
        private static readonly Dictionary<int, float[]> ShelfSlots = new Dictionary<int, float[]> { { 0, new[] { 5f, 1f } }, { 1, new[] { 4f, 1.35f } } };

        // Shopkeepers already stretched: the game rebuilds the shelf on the same shopkeeper after a purchase (SetBadgeShop
        // with refresh), and stretching the stretched spots again drifted the shelf right (a 6th slot appeared).
        private static readonly HashSet<NPCControl> stretched = new HashSet<NPCControl>();

        // A rebuilt shelf's slots appear with the game's own medal sprites, and the swap below ran every 15 frames, so the
        // vanilla medals flashed on every reshuffle (the user, 2026-09-25). For a second after a rebuild, every frame.
        private static float shelfBuiltAt = -10f;

        private static void AfterShelf()
        {
            shelfBuiltAt = Time.realtimeSinceStartup;
        }

        private static void BeforeShelf(NPCControl __instance)
        {
            stretched.RemoveWhere(n => n == null);
            if (stretched.Contains(__instance))
            {
                return;
            }
            if (randomizerOn == null || !randomizerOn() || __instance.interacttype == NPCControl.Interaction.CaravanBadge
                || __instance.dialogues == null || __instance.dialogues.Length < 10
                || !ShelfSlots.TryGetValue((int)__instance.dialogues[9].x, out float[] layout)
                || __instance.data == null || __instance.data.Length < 2
                || __instance.vectordata == null || __instance.vectordata.Length < __instance.data.Length)
            {
                return;
            }
            int shown = __instance.data.Length;
            int slots = (int)layout[0];
            Vector3 first = __instance.vectordata[0];
            Vector3 last = __instance.vectordata[shown - 1];
            Vector3 middle = (first + last) / 2f;
            Vector3 step = (last - first) / (slots - 1) * layout[1];
            var spots = new Vector3[slots];
            var data = new int[slots];
            for (int j = 0; j < slots; j++)
            {
                spots[j] = middle + step * (j - (slots - 1) / 2f);
                data[j] = __instance.data[Math.Min(j, shown - 1)];
            }
            __instance.vectordata = spots;
            __instance.data = data;
            stretched.Add(__instance);
            log.LogInfo($"[shop] shop {(int)__instance.dialogues[9].x}'s shelf: {slots} slots instead of {shown}");
        }

        // The save's bought-copy bits, one flagvar slot per shop (badgeshops index).
        internal static readonly int[] BoughtSlot = { 7, 8 };

        private static readonly AccessTools.FieldRef<NPCControl, EntityControl[]> ShelfItems =
            AccessTools.FieldRefAccess<NPCControl, EntityControl[]>("shopitems");

        // The copy whose buy prompt the player last opened (the slot's Interact), so the purchase is that slot's copy.
        private static long pendingCopy = -1;

        // A shop's copies, in location id order: (location, medal).
        private static List<KeyValuePair<long, int>> Copies(int shop)
        {
            Dictionary<long, int[]> shops = connection?.LocationShops;
            if (shops == null)
            {
                return new List<KeyValuePair<long, int>>();
            }
            return shops.Where(e => e.Value[0] == shop).OrderBy(e => e.Key)
                .Select(e => new KeyValuePair<long, int>(e.Key, e.Value[1])).ToList();
        }

        private static bool Bought(int shop, int index)
        {
            int[] vars = MainManager.instance?.flagvar;
            return vars != null && shop < BoughtSlot.Length && index < 31 && (vars[BoughtSlot[shop]] & (1 << index)) != 0;
        }

        private static bool Done(int shop, int index, long location)
        {
            return Bought(shop, index) || connection.IsDone(location);
        }

        // Whether the save marks this shop location bought (LocationChecks sends it from here).
        internal static bool BoughtInSave(long location)
        {
            Dictionary<long, int[]> shops = connection?.LocationShops;
            if (shops == null || !shops.TryGetValue(location, out int[] at))
            {
                return false;
            }
            int index = Copies(at[0]).FindIndex(c => c.Key == location);
            return index >= 0 && Bought(at[0], index);
        }

        // The k-th copy of this medal in this shop not yet done, or -1.
        private static long UndoneCopy(int shop, int medal, int k)
        {
            List<KeyValuePair<long, int>> copies = Copies(shop);
            for (int i = 0; i < copies.Count; i++)
            {
                if (copies[i].Value == medal && !Done(shop, i, copies[i].Key) && k-- == 0)
                {
                    return copies[i].Key;
                }
            }
            return -1;
        }

        // A purchase: the giveitem of a shop location's medal is running (ItemSwap.FindLocation). The copy bought is the
        // slot's the player chose, else the first not yet done; its bit is set in the save and it becomes the swap's
        // location. With every copy done (a stale shelf), the first copy's location still swaps: nothing local is given.
        internal static long Buy(long location)
        {
            Dictionary<long, int[]> shops = connection?.LocationShops;
            MainManager mm = MainManager.instance;
            if (shops == null || mm?.flagvar == null || !shops.TryGetValue(location, out int[] at))
            {
                return location;
            }
            int shop = at[0];
            int medal = at[1];
            long chosen = pendingCopy;
            pendingCopy = -1;
            List<KeyValuePair<long, int>> copies = Copies(shop);
            int index = copies.FindIndex(c => c.Key == chosen);
            if (index < 0 || copies[index].Value != medal || Done(shop, index, chosen))
            {
                chosen = UndoneCopy(shop, medal, 0);
                index = copies.FindIndex(c => c.Key == chosen);
            }
            if (index < 0)
            {
                log.LogWarning($"[shop] medal {medal} bought from shop {shop}, but every copy of it is done; location {location} swaps");
                return location;
            }
            if (shop < BoughtSlot.Length && index < 31)
            {
                mm.flagvar[BoughtSlot[shop]] |= 1 << index;
            }
            log.LogInfo($"[shop] bought copy {index + 1} of shop {shop} (medal {medal}): location {chosen}, save's bits now {mm.flagvar[BoughtSlot[shop]]}");
            return chosen;
        }

        // The game is rebuilding a shop's shelf pool from badgeshops: make each shop with locations hold exactly its copies
        // not yet done.
        private static void BeforeUpdateShops()
        {
            MainManager mm = MainManager.instance;
            Dictionary<long, int[]> shops = connection?.LocationShops;
            if (randomizerOn == null || !randomizerOn() || shops == null || mm?.badgeshops == null || mm.flagvar == null)
            {
                return;
            }
            // A purchase in flight: the buy line's kill,caller rebuilds the shelf after removebadgeshop but before its
            // giveitem (Shades's line 3, MEASURED.md; MainManager.cs:12800-12812), so the copy's bit isn't set yet. The
            // game's own removal stands until the next rebuild, by which time the bit is set.
            if (mm.message && pendingCopy >= 0)
            {
                log.LogInfo($"[shop] shelf rebuilt during a purchase (location {pendingCopy}): stock left as the game has it");
                return;
            }
            foreach (int shop in shops.Values.Select(v => v[0]).Distinct())
            {
                if (shop < 0 || shop >= mm.badgeshops.Length)
                {
                    continue;
                }
                List<KeyValuePair<long, int>> copies = Copies(shop);
                List<int> wanted = copies.Where((c, i) => !Done(shop, i, c.Key)).Select(c => c.Value).ToList();
                List<int> had = mm.badgeshops[shop] ?? new List<int>();
                if (had.OrderBy(m => m).SequenceEqual(wanted.OrderBy(m => m)))
                {
                    continue;
                }
                mm.badgeshops[shop] = wanted;
                log.LogInfo($"[shop] shop {shop}'s stock set to its {wanted.Count} copies not yet done (of {copies.Count}; it held {had.Count}): {string.Join(",", wanted)}");
            }
        }

        // The location a shop slot stands for, or -1: among the shelf's live slots of the same medal, the k-th stands
        // for that medal's k-th copy not yet done.
        private static long LocationOf(NPCControl npc)
        {
            if (connection?.LocationShops == null || npc == null || npc.interacttype != NPCControl.Interaction.Shop || npc.entity == null
                || npc.entity.animid != 2 || npc.shopkeeper == null || npc.shopkeeper.dialogues == null || npc.shopkeeper.dialogues.Length < 10)
            {
                return -1;
            }
            int shop = (int)npc.shopkeeper.dialogues[9].x;
            int medal = npc.entity.animstate;
            int k = 0;
            EntityControl[] shelf = ShelfItems(npc.shopkeeper);
            if (shelf != null)
            {
                foreach (EntityControl slot in shelf)
                {
                    if (slot == npc.entity)
                    {
                        break;
                    }
                    if (slot != null && !slot.iskill && slot.animid == 2 && slot.animstate == medal)
                    {
                        k++;
                    }
                }
            }
            return UndoneCopy(shop, medal, k);
        }

        private sealed class Saved
        {
            internal int Medal;
            internal string Name;
            internal string Description;
        }

        private static void BeforeShow(NPCControl __instance, out Saved __state)
        {
            __state = null;
            if (randomizerOn == null || !randomizerOn())
            {
                return;
            }
            long at = LocationOf(__instance);
            if (at < 0)
            {
                return;
            }
            int medal = __instance.entity.animstate;
            ItemSwap.LookOf(at, out string name, out _, out string description);
            __state = new Saved { Medal = medal, Name = MainManager.badgedata[medal, 0], Description = MainManager.badgedata[medal, 1] };
            MainManager.badgedata[medal, 0] = name ?? __state.Name;
            MainManager.badgedata[medal, 1] = description ?? __state.Description;
        }

        // Interacting with a slot opens its buy prompt: the same swap as for the description box, and the slot's copy is
        // the one a purchase in this dialogue buys.
        private static void BeforeInteract(NPCControl __instance, out Saved __state)
        {
            BeforeShow(__instance, out __state);
            if (__state != null)
            {
                pendingCopy = LocationOf(__instance);
            }
        }

        private static void AfterShow(Saved __state)
        {
            if (__state == null)
            {
                return;
            }
            MainManager.badgedata[__state.Medal, 0] = __state.Name;
            MainManager.badgedata[__state.Medal, 1] = __state.Description;
        }

        internal static void Tick()
        {
            if (Time.frameCount % 15 != 0 && Time.realtimeSinceStartup - shelfBuiltAt > 1f)
            {
                return;
            }
            bool on = randomizerOn != null && randomizerOn();
            SetPrices(on ? QualityOfLife.ShopPrices?.Value ?? "Normal" : "Normal");
            MapControl map = MainManager.map;
            if (!on || map == null || connection?.LocationShops == null)
            {
                return;
            }
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
            {
                long at = LocationOf(npc);
                if (at < 0 || npc.entity.sprite == null)
                {
                    continue;
                }
                ItemSwap.LookOf(at, out _, out Sprite sprite, out _);
                if (sprite != null && npc.entity.sprite.sprite != sprite)
                {
                    npc.entity.sprite.sprite = sprite;
                }
            }
        }

        // Normal, Half or Free, applied to the medal table's price columns; the originals are kept to put back.
        private static void SetPrices(string setting)
        {
            string[,] table = MainManager.badgedata;
            if (table == null || setting == appliedPrices)
            {
                return;
            }
            int rows = table.GetLength(0);
            if (originalPrices == null)
            {
                originalPrices = new string[rows, 2];
                for (int i = 0; i < rows; i++)
                {
                    originalPrices[i, 0] = table[i, 5];
                    originalPrices[i, 1] = table[i, 7];
                }
            }
            for (int i = 0; i < rows; i++)
            {
                table[i, 5] = Scaled(originalPrices[i, 0], setting);
                table[i, 7] = Scaled(originalPrices[i, 1], setting);
            }
            log.LogInfo($"[shop] prices: {setting}");
            appliedPrices = setting;
        }

        private static string Scaled(string original, string setting)
        {
            if (setting == "Normal" || !int.TryParse(original, out int price))
            {
                return original;
            }
            return setting == "Free" ? "0" : Math.Max(1, (price + 1) / 2).ToString();
        }
    }
}
