using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using BepInEx.Logging;

namespace BugFablesAP
{
    // Sends a location's check when the game marks it done. Each location has one game flag (slot_data's
    // location_flags, from the apworld's locations.json), and the game sets that flag itself when the location is
    // finished (agent_docs/MEASURED.md). So the mod only reads flags: it never writes game state here.
    //
    // The save is the outbox. Flags live in the save, so a location finished while offline is found again at the
    // next login and sent then; the library resends anything the server hasn't confirmed.
    //
    // Game thread, every frame. The cost is a handful of bool reads.
    //
    // A save is tied to its seed (ItemReceiver, flagstring[5]): a save from another seed sends nothing here.
    internal sealed class LocationChecks
    {
        private readonly ManualLogSource log;
        private readonly ApConnection connection;
        private readonly HashSet<long> handled = new HashSet<long>();
        private ArchipelagoSession handledFor;
        // Discoveries seen unrecorded in this session, so one recorded in play can be told from one recorded before.
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
            // A save is in play once a map is loaded: the title screen and the file select have none.
            string waiting = !randomizerOn ? "the Archipelago mod is disabled"
                : session == null ? "not connected"
                : flagsById == null ? "slot_data has no location_flags"
                : mm == null || mm.flags == null ? "no game state"
                : MainManager.map == null ? "no save in play"
                : ItemReceiver.SaveMatchesSeed(connection, log) == false ? "this save belongs to another seed"
                : null;
            // Say what the guard decided, each time it changes, so a silent non-send can't hide.
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

            // A new session starts from what the server says is already checked.
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
            // Locations marked by a number slot reaching a value (a boss prize handed over: its slot reaching 3).
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
            // Crystal berry locations: done when their index in crystalbflags is set (the pickup sets it).
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
            // Journal discoveries: done when librarystuff[0, n] is set (MainManager.UpdateJounal, MainManager.cs:14742).
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
                    // Recorded in play, seen unrecorded before: show what it found. One already recorded when the save
                    // was loaded or the seed joined was found earlier, so no hold-up.
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
            // Respawning pickups: sent by the pickup itself (ItemSwap), queued here for the seed this save belongs to.
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
