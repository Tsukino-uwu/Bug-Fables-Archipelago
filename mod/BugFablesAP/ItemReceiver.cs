using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;

namespace BugFablesAP
{
    // Gives the items the server sends, one per frame, only while the player is free. The given count (flagvar[60])
    // and the seed (flagstring[5]) live in the save. Never in battle: a retry restores flagvar but not key items.
    internal sealed class ItemReceiver
    {
        internal const int CountSlot = 60;
        internal const int SeedSlot = 5;

        private readonly ManualLogSource log;
        private readonly ApConnection connection;
        private string lastState;
        private int waitingAt = -1;

        internal ItemReceiver(ManualLogSource log, ApConnection connection)
        {
            this.log = log;
            this.connection = connection;
        }

        // Whether the save in play belongs to the connected seed (null: can't tell yet). Binds an unbound save.
        internal static bool? SaveMatchesSeed(ApConnection connection, ManualLogSource log)
        {
            MainManager mm = MainManager.instance;
            ArchipelagoSession session = connection.Session;
            string seed = session?.RoomState.Seed;
            if (mm == null || MainManager.map == null || mm.flagstring == null || mm.flagvar == null || string.IsNullOrEmpty(seed))
            {
                return null;
            }
            string bound = mm.flagstring[SeedSlot];
            if (string.IsNullOrEmpty(bound))
            {
                mm.flagstring[SeedSlot] = seed;
                log.LogInfo($"[recv] this save is now tied to seed {seed} (received count {mm.flagvar[CountSlot]})");
                return true;
            }
            if (bound != seed && AdoptOtherSeed != null && AdoptOtherSeed())
            {
                // Dev only (Debug.AdoptSeed): re-tie a test file to a new seed; the count goes back to 0 for a full replay.
                log.LogWarning($"[recv] AdoptSeed: this save belonged to seed {bound}; now tied to {seed}, received count "
                    + $"{mm.flagvar[CountSlot]} -> 0. The old seed's items and flags stay in it: a test file only.");
                mm.flagstring[SeedSlot] = seed;
                mm.flagvar[CountSlot] = 0;
                mm.flagvar[CrystalBerryTotal.ReceivedSlot] = 0;
                foreach (int slot in ShopSwap.BoughtSlot)
                {
                    mm.flagvar[slot] = 0; // the old seed's shop purchases aren't this seed's locations
                }
                return true;
            }
            return bound == seed;
        }

        internal static System.Func<bool> AdoptOtherSeed;

        internal void Tick(bool randomizerOn)
        {
            MainManager mm = MainManager.instance;
            ArchipelagoSession session = connection.Session;
            bool? matches = randomizerOn && session != null ? SaveMatchesSeed(connection, log) : null;
            string blocked = !randomizerOn ? "the Archipelago mod is disabled"
                : session == null ? "not connected"
                : matches == null ? "no save in play"
                : matches == false ? $"this save belongs to seed {mm.flagstring[SeedSlot]}, not {session.RoomState.Seed}"
                : Busy(mm);
            ReadOnlyCollection<ItemInfo> received = session?.Items.AllItemsReceived;
            int given = matches == true ? mm.flagvar[CountSlot] : -1;
            string state = blocked != null ? "waiting: " + blocked
                : $"giving: {given} of {received.Count} received";
            // Log the guard's decision when it changes; busy/free flips often, so all busy reasons compare equal.
            string key = blocked != null && blocked.StartsWith("busy") ? "busy" : state;
            if (key != lastState)
            {
                log.LogInfo("[recv] " + state);
                lastState = key;
            }
            if (blocked != null || given == received.Count)
            {
                return;
            }
            if (given < 0 || given > received.Count)
            {
                // The save counts more items than the server has sent this slot: leave it.
                if (waitingAt != -2)
                {
                    log.LogWarning($"[recv] this save counts {given} items received, but the server has sent {received.Count}; nothing given");
                    waitingAt = -2;
                }
                return;
            }

            ItemInfo item = received[given];
            string outcome = Give(mm, item);
            if (outcome == null)
            {
                if (waitingAt != given)
                {
                    log.LogInfo($"[recv] item {given + 1} ({item.ItemDisplayName}) waits: the bag and storage are both full");
                    waitingAt = given;
                }
                return; // no room yet: try again next frame, in order
            }
            mm.flagvar[CountSlot] = given + 1;
            log.LogInfo($"[recv] item {given + 1} of {received.Count}: {item.ItemDisplayName} from {item.Player.Name} "
                + $"({item.LocationDisplayName}): {outcome}");
            ShowIfWanted(item, given);
        }

        // Another player's item gets a hold-up per the Item animation setting; items the server had at login (a replay) don't.
        private void ShowIfWanted(ItemInfo item, int index)
        {
            if (index < connection.ReceivedAtLogin)
            {
                return;
            }
            string setting = QualityOfLife.ItemAnimation?.Value ?? "All";
            bool fromOther = item.Player.Slot != connection.OwnSlot;
            bool progression = (item.Flags & ItemFlags.Advancement) != 0;
            if (!fromOther || setting == "Off" || (setting == "Progression" && !progression)
                || connection.ItemKinds == null || !connection.ItemKinds.TryGetValue(item.ItemId, out int kind))
            {
                return;
            }
            ItemSwap.DescribeOurs(item.ItemId, kind, out string name, out UnityEngine.Sprite sprite, out UnityEngine.Color? color);
            HoldUps.Received(name + " from " + item.Player.Name, sprite, color, ItemSwap.ArticleOf(item.ItemId, kind));
        }

        // Returns what happened, or null when the item must wait.
        private string Give(MainManager mm, ItemInfo item)
        {
            if (item.ItemGame != ApConnection.Game || item.ItemId < ItemIds.Base)
            {
                return "not a Bug Fables item, skipped";
            }
            int kind = -1;
            if (connection.ItemKinds == null || !connection.ItemKinds.TryGetValue(item.ItemId, out kind))
            {
                return "unknown to this world's item list, skipped";
            }
            int gameId = ItemIds.GameId(item.ItemId, kind);
            if (kind == ItemIds.KeyItemKind)
            {
                mm.items[1].Add(gameId);
                return "added to key items";
            }
            if (kind == ItemIds.CrystalKind)
            {
                // flagvar[14]: the crystal berry count, the shop's currency.
                mm.flagvar[14]++;
                mm.flagvar[CrystalBerryTotal.ReceivedSlot]++;
                return $"added a crystal berry (count now {mm.flagvar[14]}, received {mm.flagvar[CrystalBerryTotal.ReceivedSlot]})";
            }
            if (kind == ItemIds.MoneyKind)
            {
                mm.showmoney = 1f;
                mm.money = UnityEngine.Mathf.Clamp(mm.money + gameId, 0, 999);
                return $"added {gameId} berries (now {mm.money})";
            }
            if (kind == ItemIds.MedalKind)
            {
                MainManager.AddBadge(gameId);
                return "added to medals";
            }
            if (mm.items[0].Count < mm.maxitems)
            {
                mm.items[0].Add(gameId);
                return "added to the bag";
            }
            if (mm.items[2].Count < mm.maxstorage)
            {
                mm.items[2].Add(gameId);
                return "bag full: added to storage";
            }
            return null;
        }

        internal static string Busy(MainManager mm)
        {
            if (MainManager.player == null) return "busy: no player";
            if (mm.inbattle || MainManager.battle != null) return "busy: battle";
            if (mm.inevent) return "busy: event";
            if (mm.message || mm.waitinput || mm.prompt || mm.inlist) return "busy: dialogue";
            if (mm.pause || mm.minipause || MainManager.pausemenu != null) return "busy: paused";
            if (mm.intransition) return "busy: changing maps";
            return null;
        }
    }
}
