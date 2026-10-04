using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Gives the items the server sends, one per frame, only while the player is free. The given count (flagvar[60])
    // and the seed (flagstring[5]) live in the save. Never in battle: a retry restores flagvar but not medals or money.
    internal sealed class ItemReceiver
    {
        internal const int CountSlot = 60;
        internal const int SeedSlot = 5;
        // Archipelago's own location for an item sent with the server's /send.
        private const long CheatConsole = -1;

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
            string seed = ServerText.SeedOf(session);
            if (mm == null || MainManager.map == null || mm.flagstring == null || mm.flagvar == null
                || string.IsNullOrEmpty(seed))
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
                // Dev only (Debug.AdoptSeed): re-tie a test file to a new seed; the count goes back to 0 for a full
                // replay.
                log.LogWarning(
                    $"[recv] AdoptSeed: this save belonged to seed {bound}; now tied to {seed}, received count "
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

        // Dev only ([Debug] AdoptSeed); off in the release build, which never sets it.
        internal static System.Func<bool> AdoptOtherSeed = null;

        internal void Tick(bool randomizerOn)
        {
            MainManager mm = MainManager.instance;
            ArchipelagoSession session = connection.Session;
            bool? matches = randomizerOn && session != null ? SaveMatchesSeed(connection, log) : null;
            string blocked = !randomizerOn ? "the Archipelago mod is disabled"
                : session == null ? "not connected"
                : matches == null ? "no save in play"
                : matches == false
                    ? $"this save belongs to seed {mm.flagstring[SeedSlot]}, not {ServerText.SeedOf(session)}"
                : connection.ItemKinds == null
                    ? "the seed's item table isn't loaded" // skipping would count the item as given
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
            if (matches == true && given >= 0 && given <= received.Count)
            {
                PartyMembers.SetReceived(MembersGiven(received, given));
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
                    log.LogInfo(
                        $"[recv] item {given + 1} ({item.ShownItem()}) waits: the bag and storage are both full");
                    waitingAt = given;
                }
                return; // no room yet: try again next frame, in order
            }
            mm.flagvar[CountSlot] = given + 1;
            log.LogInfo($"[recv] item {given + 1} of {received.Count}: {item.ShownItem()} from {item.ShownPlayer()} "
                + $"({item.ShownLocation()}): {outcome}");
            ShowIfWanted(item, given);
        }

        private System.Collections.Generic.IEnumerable<int> MembersGiven(ReadOnlyCollection<ItemInfo> received,
            int given) =>
            KindGiven(received, given, ItemIds.MemberKind);

        private System.Collections.Generic.IEnumerable<int> KindGiven(ReadOnlyCollection<ItemInfo> received, int given,
            int wanted)
        {
            for (int i = 0; i < given; i++)
            {
                ItemInfo item = received[i];
                if (item.ShownGame() == ApConnection.Game && connection.ItemKinds != null
                    && connection.ItemKinds.TryGetValue(item.ItemId, out int kind) && kind == wanted)
                {
                    yield return ItemIds.GameId(item.ItemId, kind);
                }
            }
        }

        // Every received item gets a hold-up per the Item animation setting, replays included, except an item a scene
        // just showed at its check (ItemSwap.ShownInScene), starting items (the server's location -2; its -1, the
        // cheat console's /send, is held up) and the opening's checks.
        private void ShowIfWanted(ItemInfo item, int index)
        {
            bool own = item.Player.Slot == connection.OwnSlot;
            if ((item.Player.Slot == 0 && item.LocationId != CheatConsole)
                || (own && connection.QuietLocations != null && connection.QuietLocations.Contains(item.LocationId)))
            {
                return;
            }
            if (own && ItemSwap.ShownInScene.Remove(item.LocationId))
            {
                return;
            }
            string setting = QualityOfLife.ItemAnimation?.Value ?? "All";
            bool progression = (item.Flags & ItemFlags.Advancement) != 0;
            if (setting == "Off" || (setting == "Progression" && !progression)
                || connection.ItemKinds == null || !connection.ItemKinds.TryGetValue(item.ItemId, out int kind))
            {
                return;
            }
            ItemSwap.DescribeOurs(item.ItemId, kind, out string name, out UnityEngine.Sprite sprite,
                out UnityEngine.Color? color);
            color = ItemSwap.StarburstColor(item.Flags) ?? color;
            HoldUps.Received(ItemSwap.FromText(name, item.Flags, item.ShownPlayer()), sprite, color,
                ItemSwap.ArticleOf(item.ItemId, kind));
        }

        // Returns what happened, or null when the item must wait.
        private string Give(MainManager mm, ItemInfo item)
        {
            if (item.ShownGame() != ApConnection.Game || item.ItemId < ItemIds.Base)
            {
                return "not a Bug Fables item, skipped";
            }
            int kind = -1;
            if (connection.ItemKinds == null || !connection.ItemKinds.TryGetValue(item.ItemId, out kind))
            {
                return "unknown to this world's item list, skipped";
            }
            int gameId = ItemIds.GameId(item.ItemId, kind);
            if (kind == ItemIds.KeyItemKind && gameId == CustomItems.ProgressiveBoat)
            {
                // A copy is the next level: the Boat Ticket, then the submarine; each is its own key item in the bag.
                int key = CustomItems.NextBoat(mm.items[1]);
                if (!mm.items[1].Contains(key))
                {
                    mm.items[1].Add(key);
                }
                return $"Progressive Boat: key item {key} added";
            }
            if (kind == ItemIds.KeyItemKind)
            {
                mm.items[1].Add(gameId);
                return "added to key items";
            }
            if (kind == ItemIds.CrystalKind)
            {
                // flagvar[14]: the crystal berry count, the shop's currency.
                mm.flagvar[GameVars.CrystalBerries]++;
                mm.flagvar[CrystalBerryTotal.ReceivedSlot]++;
                return $"added a crystal berry (count now {mm.flagvar[GameVars.CrystalBerries]}, received {mm.flagvar[CrystalBerryTotal.ReceivedSlot]})";
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
            if (kind == ItemIds.MemberKind)
            {
                return PartyMembers.Receive(gameId);
            }
            if (kind == ItemIds.MoveKind)
            {
                // A key item of the mod's own, the next level of a progressive item: the bag shows it, and FieldMoves
                // and Abilities read it.
                int key = Abilities.KeyFor(gameId, mm.items[1]);
                if (!mm.items[1].Contains(key))
                {
                    mm.items[1].Add(key);
                }
                return $"{Abilities.KeyName(key)} added to key items ({key}): it can be used now";
            }
            if (kind != 0)
            {
                // A kind from a newer apworld: never guessed into the bag.
                return $"item kind {kind} unknown to this mod, skipped";
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

        // Giving waits for these; a hold-up also waits for the screen (onScreen): a visible fade.
        internal static string Busy(MainManager mm, bool onScreen = false)
        {
            if (MainManager.player == null) return "busy: no player";
            if (mm.inbattle || MainManager.battle != null) return "busy: battle";
            if (mm.inevent) return "busy: event";
            if (mm.message || mm.waitinput || mm.prompt || mm.inlist) return "busy: dialogue";
            if (mm.pause || mm.minipause || MainManager.pausemenu != null) return "busy: paused";
            if (MainManager.roomtransition) return "busy: changing maps";
            if (onScreen && mm.intransition && !FadeAllButDone(mm)) return "busy: fading";
            return null;
        }

        // A dimmer fade-out eases towards clear and never reaches it: the game's loop ends only at its 10 s failsafe,
        // with intransition set all along. Below 25% a hold-up reads fine over it (about 1 s into the opening's
        // fade-in instead of 3 s at 2%); other transitions (no dimmer) count until they end.
        private static bool FadeAllButDone(MainManager mm)
        {
            Transform dimmer = mm.transitionobj != null && mm.transitionobj.Length == 1 ? mm.transitionobj[0] : null;
            SpriteRenderer fade = dimmer != null ? dimmer.GetComponent<SpriteRenderer>() : null;
            return fade != null && dimmer.name == "Dimmer" && fade.color.a < 0.25f;
        }
    }
}
