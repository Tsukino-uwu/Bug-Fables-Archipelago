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
    // Known gap: a save isn't tied to a seed yet. Loading one seed's save while connected to another sends that
    // save's finished locations to the other seed. The received-item count in the save will carry the seed.
    internal sealed class LocationChecks
    {
        private readonly ManualLogSource log;
        private readonly ApConnection connection;
        private readonly HashSet<long> handled = new HashSet<long>();
        private ArchipelagoSession handledFor;
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
