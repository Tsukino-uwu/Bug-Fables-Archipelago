using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace BugFablesAP
{
    // The connection to the Archipelago server: connect, log in, stay connected, and send the checks that
    // LocationChecks finds. No items are granted yet.
    //
    // TryConnectAndLogin blocks for up to 5 s (MultiClient.Net's own docs), so it runs on a worker thread and
    // its outcome is handed to the game thread through a queue. Nothing here touches game state.
    //
    // It runs on MultiClient.Net's net40 build, over websocket-sharp, so the connection can be compressed
    // (WebSocketCompression). On that build every send first pings and waits for the pong: never send on the
    // game thread.
    internal sealed class ApConnection
    {
        internal const string Game = "Bug Fables";

        private readonly ManualLogSource log;
        private readonly object gate = new object();
        private readonly Queue<string> messages = new Queue<string>();
        private ArchipelagoSession session;
        private volatile bool busy;
        // Each attempt gets a number. An attempt that outlives its deadline is abandoned: the number moves on, and
        // whatever the old worker reports later is thrown away.
        private int attemptNumber;
        private DateTime attemptStartedUtc;
        private ArchipelagoSession attemptSession;
        private const double AttemptDeadlineSeconds = 12;
        private volatile string status = "Not connected.";

        // A one-line summary for the Archipelago panel: the last thing that happened.
        internal string Status => status;
        internal bool Busy => busy;

        // Refused (wrong slot, password, game...) waits for the details to change. Unreachable or dropped retries
        // on its own with a growing wait (AP's hard requirement: reconnect when the connection is lost).
        private static readonly int[] RetrySeconds = { 2, 4, 8, 15, 30 };
        private volatile bool refused;
        private int failures;
        private DateTime nextRetryUtc;

        // New details: start over, connect at once.
        internal void ResetForNewDetails()
        {
            refused = false;
            failures = 0;
        }

        internal bool ShouldRetry(DateTime nowUtc) => !busy && !refused && session == null && failures > 0 && nowUtc >= nextRetryUtc;

        // A dead server isn't noticed by an idle socket (2026-09-24: the local server was killed and nothing was
        // reported). So while connected, ask the server something tiny every few seconds, and treat a long
        // silence, a failed send or a socket error as a lost connection. `_read_race_mode` is a documented
        // read-only key (network protocol.md, "Get"); any packet from the server counts as a sign of life.
        private const double PingSeconds = 5, SilenceSeconds = 15;
        private long lastHeardTicks;
        private DateTime lastPingUtc;

        internal void Watchdog(DateTime nowUtc)
        {
            // TryConnectAndLogin bounds the socket connect at 4 s, but its login step waits on SendPacket with no
            // timeout (ArchipelagoSession.LoginAsync -> BaseArchipelagoSocketHelper.SendMultiplePackets(...).Wait(),
            // MultiClient.Net 6.7.1). An attempt stuck there kept `busy` set and stopped every retry (2026-09-24).
            if (busy && (nowUtc - attemptStartedUtc).TotalSeconds > AttemptDeadlineSeconds)
            {
                ArchipelagoSession stuck;
                lock (gate)
                {
                    attemptNumber++;
                    stuck = attemptSession;
                    attemptSession = null;
                    busy = false;
                }
                Post("[ap] connect attempt gave no answer in " + AttemptDeadlineSeconds + " s; abandoned it");
                KillSocket(stuck);
                ScheduleRetry("No answer from the server.");
                return;
            }
            ArchipelagoSession s = session;
            if (s == null)
            {
                return;
            }
            if (!s.Socket.Connected)
            {
                MarkLost(s, "the socket closed");
                return;
            }
            if ((nowUtc - new DateTime(Interlocked.Read(ref lastHeardTicks), DateTimeKind.Utc)).TotalSeconds > SilenceSeconds)
            {
                MarkLost(s, "no reply from the server for " + SilenceSeconds + " s");
                return;
            }
            if ((nowUtc - lastPingUtc).TotalSeconds >= PingSeconds && Interlocked.CompareExchange(ref pingInFlight, 1, 0) == 0)
            {
                lastPingUtc = nowUtc;
                // Never send on the game thread: websocket-sharp's IsAlive, which every MultiClient.Net send checks
                // first, sends a ping and blocks until the pong arrives, up to 5 s (WebSocket.ping, WaitTime).
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        s.Socket.SendPacketAsync(new GetPacket { Keys = new[] { "_read_race_mode" } }).Wait();
                    }
                    catch (Exception e)
                    {
                        MarkLost(s, "sending failed: " + e.GetBaseException().Message);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref pingInFlight, 0);
                    }
                });
            }
        }

        private int pingInFlight;

        private void Heard()
        {
            Interlocked.Exchange(ref lastHeardTicks, DateTime.UtcNow.Ticks);
        }

        private void MarkLost(ArchipelagoSession s, string reason)
        {
            // Called from the game thread and from connection threads: only the first caller for a session acts.
            if (s == null || !ReferenceEquals(Interlocked.CompareExchange(ref session, null, s), s))
            {
                return;
            }
            KillSocket(s);
            Post("[ap] connection lost: " + reason);
            ScheduleRetry("Connection lost.");
        }

        private void ScheduleRetry(string why)
        {
            failures++;
            int wait = RetrySeconds[Math.Min(failures, RetrySeconds.Length) - 1];
            nextRetryUtc = DateTime.UtcNow.AddSeconds(wait);
            status = why + " Retrying in " + wait + " s.";
        }

        internal void SetStatus(string text)
        {
            status = text;
        }

        internal ApConnection(ManualLogSource log)
        {
            this.log = log;
        }

        internal bool Connected => session != null && session.Socket.Connected;

        // The logged-in session, or null. Read on the game thread by LocationChecks.
        internal ArchipelagoSession Session => session;

        // True once a login this run has brought the seed's tables (slot_data). They stay after a drop, so a dropped
        // connection keeps the rules in force; before the first login nothing is known, and the file select holds
        // randomizer saves back (MenuToggle): the mod keeps no copy of the seed on disk (the user, 2026-09-24).
        internal bool SeedKnown => seedKnown;
        private volatile bool seedKnown;

        // slot_data's location_flags: which game flag marks each location done ({location id: flag}). Set at each
        // login; null when the world didn't send it.
        internal Dictionary<long, int> LocationFlags => locationFlags;
        private volatile Dictionary<long, int> locationFlags;

        private static Dictionary<long, int> ReadLocationFlags(Dictionary<string, object> slotData)
        {
            if (slotData == null || !slotData.TryGetValue("location_flags", out object raw) || !(raw is JObject map))
            {
                return null;
            }
            var result = new Dictionary<long, int>();
            foreach (JProperty entry in map.Properties())
            {
                result[long.Parse(entry.Name)] = entry.Value.Value<int>();
            }
            return result;
        }

        // slot_data's location_gives: the |giveitem| that hands out each location's vanilla item. ItemSwap keeps
        // that item out of the inventory. Null when the world didn't send it.
        internal Dictionary<long, Give> LocationGives => locationGives;
        private volatile Dictionary<long, Give> locationGives;

        // slot_data's item_kinds: which inventory list each Bug Fables item belongs to ({item id: 0 item, 1 key item}).
        internal Dictionary<long, int> ItemKinds => itemKinds;
        private volatile Dictionary<long, int> itemKinds;

        private static Dictionary<long, int> ReadItemKinds(Dictionary<string, object> slotData)
        {
            if (slotData == null || !slotData.TryGetValue("item_kinds", out object raw) || !(raw is JObject map))
            {
                return null;
            }
            return map.Properties().ToDictionary(p => long.Parse(p.Name), p => p.Value.Value<int>());
        }

        // This player's slot number, from the last login (kept after a drop, like the tables above).
        internal int OwnSlot => ownSlot;
        private volatile int ownSlot = -1;

        internal sealed class Give
        {
            internal string Map;
            internal int Type;
            internal int Item;
        }

        private static Dictionary<long, Give> ReadLocationGives(Dictionary<string, object> slotData)
        {
            if (slotData == null || !slotData.TryGetValue("location_gives", out object raw) || !(raw is JObject map))
            {
                return null;
            }
            var result = new Dictionary<long, Give>();
            foreach (JProperty entry in map.Properties())
            {
                result[long.Parse(entry.Name)] = new Give
                {
                    Map = entry.Value.Value<string>("map"),
                    Type = entry.Value.Value<int>("type"),
                    Item = entry.Value.Value<int>("item"),
                };
            }
            return result;
        }

        // slot_data's location_pickups: locations that are items lying in the world (or buried), each known by the
        // pickup's own activationflag on its map ({location id: {map, flag}}). ItemSwap's pickup prefix keeps their
        // vanilla item out. Null when the world didn't send it.
        internal Dictionary<long, Pickup> LocationPickups => locationPickups;
        private volatile Dictionary<long, Pickup> locationPickups;

        internal sealed class Pickup
        {
            internal string Map;
            internal int Flag;
            // A story pickup has no activationflag of its own (its story event hides it for good): it's known by the
            // story event picking it up starts (its data[1]), the same whether the scene created it (Event4's
            // "tempitem") or the map did on a later visit ("MushroomItem"). -1 for ordinary pickups.
            internal int Event = -1;
            // A crystal berry is known by its index (crystalbflags), in data[0] at pickup. -1 for other pickups.
            internal int Berry = -1;
            // A respawning pickup has no flag of its own, only a regional flag the game wipes on every area change
            // (NPCControl.CheckItem writes |regionalflag,N,true| before the add). -1 for other pickups.
            internal int Regional = -1;
        }

        private static Dictionary<long, Pickup> ReadLocationPickups(Dictionary<string, object> slotData)
        {
            if (slotData == null || !slotData.TryGetValue("location_pickups", out object raw) || !(raw is JObject map))
            {
                return null;
            }
            var result = new Dictionary<long, Pickup>();
            foreach (JProperty entry in map.Properties())
            {
                result[long.Parse(entry.Name)] = new Pickup
                {
                    Map = entry.Value.Value<string>("map"),
                    Flag = entry.Value.Value<int>("flag"),
                    Event = entry.Value.Value<int?>("event") ?? -1,
                    Berry = entry.Value.Value<int?>("berry") ?? -1,
                    Regional = entry.Value.Value<int?>("regional") ?? -1,
                };
            }
            return result;
        }

        // slot_data's location_vars: locations marked done by a number slot reaching a value, not a flag
        // ({location id: {var, at_least}}; a boss prize handed over is its prize slot reaching 3). Null when not sent.
        internal Dictionary<long, int[]> LocationVars => locationVars;
        private volatile Dictionary<long, int[]> locationVars;

        private static Dictionary<long, int[]> ReadLocationVars(Dictionary<string, object> slotData)
        {
            if (slotData == null || !slotData.TryGetValue("location_vars", out object raw) || !(raw is JObject map))
            {
                return null;
            }
            return map.Properties().ToDictionary(p => long.Parse(p.Name),
                p => new[] { p.Value.Value<int>("var"), p.Value.Value<int>("at_least") });
        }

        // slot_data's location_berries: crystal berry locations, done when their crystalbflags index is set
        // ({location id: index}). Null when not sent.
        internal Dictionary<long, int> LocationBerries => locationBerries;
        private volatile Dictionary<long, int> locationBerries;

        // slot_data's location_discoveries: journal discovery locations, done when librarystuff[0, n] is set
        // ({location id: n}; Shuffle Discoveries, 2026-09-25). Null when not sent.
        internal Dictionary<long, int> LocationDiscoveries => locationDiscoveries;
        private volatile Dictionary<long, int> locationDiscoveries;

        private static Dictionary<long, int> ReadLocationBerries(Dictionary<string, object> slotData, string key = "location_berries")
        {
            if (slotData == null || !slotData.TryGetValue(key, out object raw) || !(raw is JObject map))
            {
                return null;
            }
            return map.Properties().ToDictionary(p => long.Parse(p.Name), p => p.Value.Value<int>());
        }

        // slot_data's kept_open: blockers the story puts up for a while that the seed keeps out of the way, so an area
        // with locations never closes ([{map, entity}], KeptOpen). Null when the world didn't send it.
        internal List<Blocker> KeptOpen => keptOpen;
        private volatile List<Blocker> keptOpen;

        // slot_data's kept_present: ways the story only makes later (a door, a bounce mushroom) that the seed makes
        // exist from the start ([{map, entity}], KeptOpen). Null when the world didn't send it.
        internal List<Blocker> KeptPresent => keptPresent;
        private volatile List<Blocker> keptPresent;

        // slot_data's scenery_hidden: map scenery the story removes later (the Outskirts rocks) that the seed removes from
        // the start ([{map, entity}], entity being the object's path inside the map, as MapDump writes it).
        internal List<Blocker> SceneryHidden => sceneryHidden;
        private volatile List<Blocker> sceneryHidden;

        // slot_data's held_until: an entity with no gate of its own that the seed keeps away until a story flag
        // ([{map, entity, flag}]): the town's first-entry scene, reachable once the rocks are gone, waits for the first
        // boss as it did behind them.
        internal List<Blocker> HeldUntil => heldUntil;
        private volatile List<Blocker> heldUntil;

        // slot_data's present_from: a way the story makes at a late flag that the seed makes at an earlier one instead
        // ([{map, entity, flag}]): the door back down to the fall room exists from the trapdoor (14), not the first boss.
        internal List<Blocker> PresentFrom => presentFrom;
        private volatile List<Blocker> presentFrom;

        internal sealed class Blocker
        {
            internal string Map;
            internal string Entity;
            internal int Flag = -1;
        }

        private static List<Blocker> ReadKeptOpen(Dictionary<string, object> slotData, string key = "kept_open")
        {
            if (slotData == null || !slotData.TryGetValue(key, out object raw) || !(raw is JArray list))
            {
                return null;
            }
            return list.Select(e => new Blocker
            {
                Map = e.Value<string>("map"),
                Entity = e.Value<string>("entity"),
                Flag = e["flag"] != null ? e.Value<int>("flag") : -1,
            }).ToList();
        }

        // Respawning pickups (the user, 2026-09-24): the first pickup sends the check and gives nothing, later ones are
        // the game's own again. Nothing in the save marks them, so the mod keeps what's done: the server's checked list
        // from the last login, its updates, and the checks picked up here. A pickup made while the connection is down
        // waits in the outbox, tagged with its save's seed, and LocationChecks sends it once a session for that seed
        // is up. Only memory: if the game closes first, the spot shows the seed's item again next time and the pickup
        // is simply made again. Game thread and connection threads, hence the lock.
        private readonly object doneLock = new object();
        private readonly HashSet<long> done = new HashSet<long>();
        private readonly Dictionary<long, string> respawnOutbox = new Dictionary<long, string>();

        internal bool IsDone(long location)
        {
            lock (doneLock)
            {
                return done.Contains(location);
            }
        }

        internal void QueueRespawnCheck(long location, string seed)
        {
            lock (doneLock)
            {
                done.Add(location);
                respawnOutbox[location] = seed;
            }
        }

        // The outbox's checks for this seed, taken out to send; a stale seed's are dropped.
        internal long[] TakeRespawnChecks(string seed)
        {
            lock (doneLock)
            {
                long[] ids = respawnOutbox.Where(e => e.Value == seed).Select(e => e.Key).ToArray();
                respawnOutbox.Clear();
                return ids;
            }
        }

        private void ResetDone(ArchipelagoSession s)
        {
            string seed = s.RoomState.Seed;
            lock (doneLock)
            {
                done.Clear();
                done.UnionWith(s.Locations.AllLocationsChecked);
                done.UnionWith(respawnOutbox.Where(e => e.Value == seed).Select(e => e.Key));
            }
        }

        private void MarkDone(IEnumerable<long> ids)
        {
            lock (doneLock)
            {
                done.UnionWith(ids);
            }
        }

        // What the seed put at each of this slot's locations, asked once per login without creating hints
        // (HintCreationPolicy.None: a hint-creating scout would announce the seed). Null until it arrives.
        internal Dictionary<long, ScoutedItemInfo> Scouts => scouts;
        private volatile Dictionary<long, ScoutedItemInfo> scouts;

        private void Scout(ArchipelagoSession s, ICollection<long> locations)
        {
            if (locations == null || locations.Count == 0)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var task = s.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locations.ToArray());
                    if (!task.Wait(TimeSpan.FromSeconds(10)))
                    {
                        Post("[swap] scouting gave no answer in 10 s; finds will show a plain Archipelago item");
                        return;
                    }
                    scouts = task.Result;
                    Post("[swap] scouted " + string.Join(", ", task.Result.Values
                        .Select(i => i.LocationId + " = " + i.ItemDisplayName + " (" + i.ItemGame + ", for " + i.Player.Name + ")")
                        .ToArray()));
                }
                catch (Exception e)
                {
                    Post("[swap] scouting failed: " + e.GetBaseException().Message);
                }
            });
        }

        // Send finished locations from a worker thread (on this build every send pings first and waits). The
        // library keeps every check the server hasn't confirmed and resends them with the next send
        // (LocationCheckHelper, 6.7.1); checks made offline are found again in the save's flags at the next login.
        internal void SendChecks(ArchipelagoSession s, long[] ids)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    s.Locations.CompleteLocationChecksAsync(ids).Wait();
                    Post("[check] sent " + string.Join(", ", ids));
                }
                catch (Exception e)
                {
                    Post("[check] sending " + string.Join(", ", ids) + " failed: " + e.GetBaseException().Message
                        + " (sent again after the next login)");
                }
            });
        }

        internal void Connect(string server, string slot, string password)
        {
            if (busy)
            {
                log.LogInfo("[ap] a connect attempt is already running");
                return;
            }
            busy = true;
            // Connecting again (another room, or the same one after a change) replaces the old session.
            Disconnect();
            log.LogInfo($"[ap] connecting to {server} as '{slot}'");
            status = $"Connecting to {server} as {slot}...";
            int number;
            lock (gate)
            {
                number = ++attemptNumber;
                attemptStartedUtc = DateTime.UtcNow;
            }
            var worker = new Thread(() => ConnectOnWorker(number, server, slot, password)) { IsBackground = true };
            worker.Start();
        }

        private bool IsCurrent(int number)
        {
            lock (gate)
            {
                return number == attemptNumber;
            }
        }

        private void ConnectOnWorker(int number, string server, string slot, string password)
        {
            try
            {
                ArchipelagoSession attempt = ArchipelagoSessionFactory.CreateSession(server);
                lock (gate)
                {
                    if (number == attemptNumber)
                    {
                        attemptSession = attempt;
                    }
                }
                // A login that times out says only "Connection timed out." Log what the socket reported on the way,
                // once per attempt and message.
                var seen = new HashSet<string>();
                attempt.Socket.ErrorReceived += (e, message) =>
                {
                    string line = (e?.GetType().Name ?? "no exception") + ": " + (e?.GetBaseException().Message ?? message);
                    bool first;
                    lock (seen)
                    {
                        first = seen.Add(line);
                    }
                    if (first && ReferenceEquals(session, null))
                    {
                        // The whole exception, stack included: the message alone didn't say where it came from.
                        Post("[ap] socket error while connecting: " + line + "\n" + e);
                    }
                };
                LoginResult result = attempt.TryConnectAndLogin(
                    Game, slot, ItemsHandlingFlags.AllItems, password: string.IsNullOrEmpty(password) ? null : password);

                if (!IsCurrent(number))
                {
                    // Abandoned at its deadline (Watchdog) or replaced: close whatever it opened, report nothing else.
                    KillSocket(attempt);
                    Post("[ap] an abandoned connect attempt finished late (" + result?.GetType().Name + "); ignored");
                    return;
                }

                if (result is LoginSuccessful ok)
                {
                    session = attempt;
                    failures = 0;
                    refused = false;
                    status = $"Connected as {slot}.";
                    Heard();
                    lastPingUtc = DateTime.UtcNow;
                    // Only this session dropping counts; Disconnect() clears `session` first.
                    locationFlags = ReadLocationFlags(ok.SlotData);
                    locationGives = ReadLocationGives(ok.SlotData);
                    locationPickups = ReadLocationPickups(ok.SlotData);
                    keptOpen = ReadKeptOpen(ok.SlotData);
                    keptPresent = ReadKeptOpen(ok.SlotData, "kept_present");
                    sceneryHidden = ReadKeptOpen(ok.SlotData, "scenery_hidden");
                    heldUntil = ReadKeptOpen(ok.SlotData, "held_until");
                    presentFrom = ReadKeptOpen(ok.SlotData, "present_from");
                    locationVars = ReadLocationVars(ok.SlotData);
                    locationBerries = ReadLocationBerries(ok.SlotData);
                    locationDiscoveries = ReadLocationBerries(ok.SlotData, "location_discoveries");
                    ownSlot = ok.Slot;
                    itemKinds = ReadItemKinds(ok.SlotData);
                    seedKnown = true;
                    scouts = null;
                    ResetDone(attempt);
                    // Every location this slot has: gifts and pickups alike show what's really there.
                    Scout(attempt, (locationFlags?.Keys ?? Enumerable.Empty<long>()).Concat(locationVars?.Keys ?? Enumerable.Empty<long>())
                        .Concat(locationBerries?.Keys ?? Enumerable.Empty<long>())
                        .Concat(locationDiscoveries?.Keys ?? Enumerable.Empty<long>())
                        .Concat(locationPickups?.Keys ?? Enumerable.Empty<long>()).Distinct().ToList());
                    attempt.Locations.CheckedLocationsUpdated += ids =>
                    {
                        MarkDone(ids);
                        Post("[check] now checked on the server: " + string.Join(", ", ids.Select(id => id.ToString()).ToArray()));
                    };
                    attempt.Socket.PacketReceived += packet => Heard();
                    attempt.Socket.SocketClosed += reason => MarkLost(attempt, "closed: " + reason);
                    attempt.Socket.ErrorReceived += (e, message) => MarkLost(attempt, "socket error: " + message);
                    string version = ok.SlotData != null && ok.SlotData.TryGetValue("world_version", out object v) ? v?.ToString() : "missing";
                    Post($"[ap] logged in: slot {ok.Slot}, team {ok.Team}, world_version {version}, "
                        + $"{attempt.Items.AllItemsReceived.Count} items received so far, "
                        + $"{attempt.Locations.AllLocationsChecked.Count} of {attempt.Locations.AllLocations.Count} locations checked");
                    // Read back what the handshake settled, from the socket itself.
                    // A bare address tries wss:// first and falls back to ws:// (ArchipelagoSocketHelper), so which
                    // one connected is only known from the socket.
                    WebSocket socket = WebSocketOf(attempt);
                    string extensions = socket?.Extensions;
                    Post("[ap] connected over " + (socket?.Url?.Scheme ?? "unknown") + ", compression: "
                        + (string.IsNullOrEmpty(extensions) ? "none" : extensions));
                }
                else if (result is LoginFailure failed)
                {
                    string why = string.Join("; ", failed.Errors);
                    if (failed.ErrorCodes != null && failed.ErrorCodes.Length > 0)
                    {
                        // The server answered and said no: retrying the same details cannot help.
                        refused = true;
                        status = "Refused: " + why;
                        Post("[ap] login refused: " + why + " (" + string.Join(", ", failed.ErrorCodes) + ")");
                    }
                    else
                    {
                        // No answer from a server (unreachable, timed out): worth retrying.
                        Post("[ap] could not reach the server: " + why);
                        ScheduleRetry("Could not reach the server.");
                    }
                }
                else
                {
                    Post($"[ap] login returned an unexpected result type: {result?.GetType().FullName ?? "null"}");
                }
            }
            catch (Exception e) when (!IsCurrent(number))
            {
                Post("[ap] an abandoned connect attempt threw late: " + e.GetBaseException().Message);
            }
            catch (Exception e)
            {
                // The first connect is also the measurement of whether this game's Mono can run the client
                // library at all (websocket-sharp, no System.Reflection.Emit), so report the whole exception.
                Post("[ap] connect threw: " + e);
                ScheduleRetry("Could not connect: " + e.GetBaseException().Message + ".");
            }
            finally
            {
                lock (gate)
                {
                    if (number == attemptNumber)
                    {
                        busy = false;
                        attemptSession = null;
                    }
                }
            }
        }

        internal void Post(string message)
        {
            lock (gate)
            {
                messages.Enqueue(message);
            }
        }

        // Game thread: write out what the worker reported.
        internal void Tick()
        {
            lock (gate)
            {
                while (messages.Count > 0)
                {
                    log.LogInfo(messages.Dequeue());
                }
            }
        }

        internal void Disconnect()
        {
            // First, so the SocketClosed handler knows this close was on purpose.
            ArchipelagoSession closing = Interlocked.Exchange(ref session, null);
            KillSocket(closing);
            failures = 0;
        }

        // The websocket-sharp socket inside MultiClient.Net's net40 helper (an internal field, hence reflection).
        private static WebSocket WebSocketOf(ArchipelagoSession s)
        {
            FieldInfo field = s?.Socket?.GetType().GetField("webSocket", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(s.Socket) as WebSocket;
        }

        // Close a session's socket for good, on a worker thread: websocket-sharp's Close sends a close frame and waits
        // up to 5 s for the answer (WaitTime), which a dead server never sends. Closing releases the TCP stream, which
        // also ends websocket-sharp's receive thread if the server vanished without a word.
        // History: on the netstandard2.0 build (Mono's ClientWebSocket) a lost socket stayed "Open" and the library's
        // receive loop spun forever, measured 2026-09-24 as five threads at ~75% of a core and memory growing
        // ~2.5 MB/s. Switching to the net40 build (websocket-sharp, for compression) replaced that layer; the same
        // drop test is re-run on it (agent_docs/apimplementation.md, build step 5).
        private void KillSocket(ArchipelagoSession s)
        {
            if (s == null)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    WebSocket socket = WebSocketOf(s);
                    if (socket == null)
                    {
                        Post("[ap] could not reach the web socket to close it (field 'webSocket' missing)");
                        return;
                    }
                    WebSocketState before = socket.ReadyState;
                    socket.Close();
                    Post($"[ap] socket closed: {before} -> {socket.ReadyState}");
                }
                catch (Exception e)
                {
                    Post("[ap] closing the socket threw: " + e.GetBaseException().Message);
                }
            });
        }
    }
}
