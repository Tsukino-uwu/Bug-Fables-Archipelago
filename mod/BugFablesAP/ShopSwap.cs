using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Medal shops: shelf slots whose medal is a location show and name the seed's item, prices scale with the QoL row,
    // and the stock is every copy not yet done (bits per copy in flagvar[7]/[8], so an offline purchase is kept).
    internal static class ShopSwap
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static string[,] originalPrices;
        private static int appliedPrices = QualityOfLife.FullPrice;

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
            SetPrices(QualityOfLife.FullPrice);
        }

        // {shop: (slots, spread)}: spots laid out evenly around the middle of the shopkeeper's first and last spot.
        private static readonly Dictionary<int, float[]> ShelfSlots = new Dictionary<int, float[]> { { 0, new[] { 5f, 1f } }, { 1, new[] { 4f, 1.35f } } };

        // The game rebuilds the shelf on the same shopkeeper after a purchase; stretching twice drifts it.
        private static readonly HashSet<NPCControl> stretched = new HashSet<NPCControl>();

        // Swap every frame for a second after a rebuild, or the vanilla medal sprites flash.
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

        // One flagvar slot per shop for the bought-copy bits (unused by the game).
        internal static readonly int[] BoughtSlot = { 7, 8 };

        private static readonly AccessTools.FieldRef<NPCControl, EntityControl[]> ShelfItems =
            AccessTools.FieldRefAccess<NPCControl, EntityControl[]>("shopitems");

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

        // A purchase: the chosen slot's copy, else the first not yet done. With every copy done (a stale shelf), the
        // location still swaps, so nothing local is given.
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

        private static void BeforeUpdateShops()
        {
            MainManager mm = MainManager.instance;
            Dictionary<long, int[]> shops = connection?.LocationShops;
            if (randomizerOn == null || !randomizerOn() || shops == null || mm?.badgeshops == null || mm.flagvar == null)
            {
                return;
            }
            // The buy line's kill,caller rebuilds the shelf before its giveitem sets the bit; leave the game's removal alone.
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

        // Among the shelf's live slots of the same medal, the k-th stands for that medal's k-th copy not yet done.
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
            SetPrices(QualityOfLife.SettingsOn != null && QualityOfLife.SettingsOn() ? QualityOfLife.ShopPrices?.Value ?? QualityOfLife.FullPrice : QualityOfLife.FullPrice);
            MapControl map = MainManager.map;
            if (!on || map == null || connection?.LocationShops == null)
            {
                return;
            }
            Settle();
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

        // A copy the server has checked with no bit in this save was bought in another save: charge it here, so reloads
        // can't refund berries. Merab's shop only (berries).
        private static void Settle()
        {
            MainManager mm = MainManager.instance;
            if (mm?.flagvar == null || MainManager.player == null || mm.inevent || mm.message || MainManager.battle != null
                || connection.Session == null || ItemReceiver.SaveMatchesSeed(connection, log) != true)
            {
                return;
            }
            const int shop = 0;
            List<KeyValuePair<long, int>> copies = Copies(shop);
            for (int i = 0; i < copies.Count && i < 31; i++)
            {
                if (Bought(shop, i) || !connection.IsDone(copies[i].Key))
                {
                    continue;
                }
                int.TryParse(MainManager.badgedata[copies[i].Value, 5], out int price);
                int had = mm.money;
                mm.money = Mathf.Clamp(mm.money - price, 0, 999);
                mm.flagvar[BoughtSlot[shop]] |= 1 << i;
                log.LogInfo($"[shop] copy {i + 1} of shop {shop} (location {copies[i].Key}) was bought in another save: charged {price} "
                    + $"berries here ({had} -> {mm.money}{(had < price ? ", not enough, the rest forgiven" : "")}); save's bits now {mm.flagvar[BoughtSlot[shop]]}");
            }
        }

        // Columns 5 (berries) and 7 (crystal berries) are the medal table's prices.
        private static void SetPrices(int setting)
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
            log.LogInfo($"[shop] prices: {setting * 10}%");
            appliedPrices = setting;
        }

        private static string Scaled(string original, int tenths)
        {
            if (tenths >= QualityOfLife.FullPrice || !int.TryParse(original, out int price))
            {
                return original;
            }
            return tenths <= 0 ? "0" : Math.Max(1, (price * tenths + 9) / 10).ToString();
        }
    }
}
