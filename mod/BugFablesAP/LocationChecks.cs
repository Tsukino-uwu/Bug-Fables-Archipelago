using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using BepInEx.Logging;

namespace BugFablesAP
{
    // Sends a location's check when the game marks it done. The save is the outbox: a location done offline is found
    // again at the next login. A save from another seed sends nothing.
    internal sealed class LocationChecks
    {
        private readonly ManualLogSource log;
        private readonly ApConnection connection;
        private readonly HashSet<long> handled = new HashSet<long>();
        private ArchipelagoSession handledFor;
        // Discoveries seen unrecorded this session: only one recorded in play gets a hold-up.
        private readonly HashSet<long> notYetRecorded = new HashSet<long>();
        private ArchipelagoSession notYetRecordedFor;
        private string lastState;

        internal LocationChecks(ManualLogSource log, ApConnection connection)
        {
            this.log = log;
            this.connection = connection;
        }

        internal void Tick(bool randomizerOn)
        {
            ArchipelagoSession session = connection.Session;
            Dictionary<long, int> flagsById = connection.LocationFlags;
            MainManager mm = MainManager.instance;
            string waiting = !randomizerOn ? "the Archipelago mod is disabled"
                : session == null ? "not connected"
                : flagsById == null ? "slot_data has no location_flags"
                : mm == null || mm.flags == null ? "no game state"
                : MainManager.map == null ? "no save in play"
                : ItemReceiver.SaveMatchesSeed(connection, log) == false ? "this save belongs to another seed"
                : null;
            string state = waiting == null
                ? "watching " + string.Join(", ", flagsById.Select(e => e.Key + " (flag " + e.Value + ")").ToArray())
                : "waiting: " + waiting;
            if (state != lastState)
            {
                log.LogInfo("[check] " + state);
                lastState = state;
            }
            if (waiting != null)
            {
                return;
            }

            if (!ReferenceEquals(session, handledFor))
            {
                handledFor = session;
                handled.Clear();
            }

            bool[] flags = mm.flags;
            List<long> finished = null;
            foreach (KeyValuePair<long, int> entry in flagsById)
            {
                if (handled.Contains(entry.Key) || entry.Value < 0 || entry.Value >= flags.Length || !flags[entry.Value])
                {
                    continue;
                }
                handled.Add(entry.Key);
                if (session.Locations.AllLocationsChecked.Contains(entry.Key))
                {
                    continue; // the server has it already (sent earlier, or by another client on this slot)
                }
                log.LogInfo($"[check] location {entry.Key} is done (flag {entry.Value} set) on {Where()}: sending");
                (finished ?? (finished = new List<long>())).Add(entry.Key);
            }
            // Locations marked by a flagvar slot reaching a value (a boss prize handed over: 3).
            Dictionary<long, int[]> vars = connection.LocationVars;
            if (vars != null && mm.flagvar != null)
            {
                foreach (KeyValuePair<long, int[]> entry in vars)
                {
                    int slot = entry.Value[0];
                    if (handled.Contains(entry.Key) || slot < 0 || slot >= mm.flagvar.Length || mm.flagvar[slot] < entry.Value[1])
                    {
                        continue;
                    }
                    handled.Add(entry.Key);
                    if (session.Locations.AllLocationsChecked.Contains(entry.Key))
                    {
                        continue;
                    }
                    log.LogInfo($"[check] location {entry.Key} is done (flagvar[{slot}] = {mm.flagvar[slot]}) on {Where()}: sending");
                    (finished ?? (finished = new List<long>())).Add(entry.Key);
                }
            }
            Dictionary<long, int> berries = connection.LocationBerries;
            if (berries != null && mm.crystalbflags != null)
            {
                foreach (KeyValuePair<long, int> entry in berries)
                {
                    if (handled.Contains(entry.Key) || entry.Value < 0 || entry.Value >= mm.crystalbflags.Length || !mm.crystalbflags[entry.Value])
                    {
                        continue;
                    }
                    handled.Add(entry.Key);
                    if (session.Locations.AllLocationsChecked.Contains(entry.Key))
                    {
                        continue;
                    }
                    log.LogInfo($"[check] location {entry.Key} is done (crystal berry {entry.Value} taken) on {Where()}: sending");
                    (finished ?? (finished = new List<long>())).Add(entry.Key);
                }
            }
            if (!ReferenceEquals(session, notYetRecordedFor))
            {
                notYetRecordedFor = session;
                notYetRecorded.Clear();
            }
            // Journal discoveries: librarystuff[0, n].
            Dictionary<long, int> discoveries = connection.LocationDiscoveries;
            bool[,] journal = mm.librarystuff;
            if (discoveries != null && journal != null)
            {
                foreach (KeyValuePair<long, int> entry in discoveries)
                {
                    if (handled.Contains(entry.Key) || entry.Value < 0 || entry.Value >= journal.GetLength(1))
                    {
                        continue;
                    }
                    if (!journal[0, entry.Value])
                    {
                        notYetRecorded.Add(entry.Key);
                        continue;
                    }
                    handled.Add(entry.Key);
                    if (notYetRecorded.Remove(entry.Key))
                    {
                        HoldUps.FoundAt(entry.Key, "discovery " + entry.Value);
                    }
                    if (session.Locations.AllLocationsChecked.Contains(entry.Key))
                    {
                        continue;
                    }
                    log.LogInfo($"[check] location {entry.Key} is done (discovery {entry.Value} recorded) on {Where()}: sending");
                    (finished ?? (finished = new List<long>())).Add(entry.Key);
                }
            }
            // Shop stock: the save's bought bit (ShopSwap), since the stock can't tell two copies of a medal apart.
            Dictionary<long, int[]> shops = connection.LocationShops;
            if (shops != null && MainManager.map != null)
            {
                foreach (KeyValuePair<long, int[]> entry in shops)
                {
                    int shop = entry.Value[0];
                    if (handled.Contains(entry.Key) || !ShopSwap.BoughtInSave(entry.Key))
                    {
                        continue;
                    }
                    handled.Add(entry.Key);
                    if (session.Locations.AllLocationsChecked.Contains(entry.Key))
                    {
                        continue;
                    }
                    log.LogInfo($"[check] location {entry.Key} is done (medal {entry.Value[1]} bought from shop {shop}, the save's bit) on {Where()}: sending");
                    (finished ?? (finished = new List<long>())).Add(entry.Key);
                }
            }
            // Respawning pickups send their own check (ItemSwap); queued per seed.
            foreach (long id in connection.TakeRespawnChecks(session.RoomState.Seed))
            {
                log.LogInfo($"[check] location {id} is done (respawning pickup taken) on {Where()}: sending");
                (finished ?? (finished = new List<long>())).Add(id);
            }
            if (finished != null)
            {
                connection.SendChecks(session, finished.ToArray());
            }
        }

        private static string Where()
        {
            MapControl map = MainManager.map;
            return map == null ? "no map" : $"{map.mapid}/{map.areaid}";
        }
    }
}
