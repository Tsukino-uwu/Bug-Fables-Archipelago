using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace BugFablesAP
{
    // The Archipelago server connection: connect, log in, stay connected, send checks. Never touches game state.
    // On the net40 build (websocket-sharp) every send pings and waits for the pong: never send on the game thread.
    internal sealed class ApConnection
    {
        internal const string Game = "Bug Fables";

        private readonly ManualLogSource log;
        private readonly object gate = new object();
        private readonly Queue<string> messages = new Queue<string>();
        private ArchipelagoSession session;
        private volatile bool busy;
        // An attempt past its deadline is abandoned: the number moves on and its late report is thrown away.
        private int attemptNumber;
        private DateTime attemptStartedUtc;
        private ArchipelagoSession attemptSession;
        private const double AttemptDeadlineSeconds = 12;
        private volatile string status = "Not connected.";

        internal string Status => status;
        internal bool Busy => busy;

        // Refused waits for new details; unreachable or dropped retries on its own with a growing wait.
        private static readonly int[] RetrySeconds = { 2, 4, 8, 15, 30 };
        private volatile bool refused;
        private int failures;
        private DateTime nextRetryUtc;

        internal void ResetForNewDetails()
        {
            refused = false;
            failures = 0;
        }

        internal bool ShouldRetry(DateTime nowUtc) => !busy && !refused && session == null && failures > 0
            && nowUtc >= nextRetryUtc;

        // An idle socket never notices a dead server: ping every few seconds and treat silence or a failed send as
        // lost.
        private const double PingSeconds = 5, SilenceSeconds = 15;
        private long lastHeardTicks;
        private DateTime lastPingUtc;

        internal void Watchdog(DateTime nowUtc)
        {
            // The login step waits on a send with no timeout, so a stuck attempt would keep `busy` set forever.
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
            if ((nowUtc - new DateTime(Interlocked.Read(ref lastHeardTicks), DateTimeKind.Utc)).TotalSeconds
                > SilenceSeconds)
            {
                MarkLost(s, "no reply from the server for " + SilenceSeconds + " s");
                return;
            }
            if ((nowUtc - lastPingUtc).TotalSeconds >= PingSeconds
                && Interlocked.CompareExchange(ref pingInFlight, 1, 0) == 0)
            {
                lastPingUtc = nowUtc;
                // Off the game thread: IsAlive, checked before every send, blocks up to 5 s for a pong.
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
            // Game thread and connection threads: only the first caller for a session acts.
            if (s == null || !ReferenceEquals(Interlocked.CompareExchange(ref session, null, s), s))
            {
                return;
            }
            deathLinks = null;
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

        internal ArchipelagoSession Session => session;

        // The seed's data, parsed at a login; kept after a drop so the rules stay in force. Never saved to disk.
        internal SeedData Seed => seedData;
        private volatile SeedData seedData;
        internal bool SeedKnown => seedData != null;

        internal Dictionary<long, int> LocationFlags => seedData?.LocationFlags;
        internal Dictionary<long, Give> LocationGives => seedData?.LocationGives;
        internal Dictionary<long, int[]> LocationAdded => seedData?.LocationAdded;
        internal Dictionary<long, Pickup> LocationPickups => seedData?.LocationPickups;
        internal Dictionary<long, int[]> LocationVars => seedData?.LocationVars;
        internal Dictionary<long, int> LocationBerries => seedData?.LocationBerries;
        internal Dictionary<long, int> LocationDiscoveries => seedData?.LocationDiscoveries;
        internal HashSet<long> SilentLocations => seedData?.SilentLocations;
        internal HashSet<long> QuietLocations => seedData?.QuietLocations;
        internal Dictionary<long, int[]> LocationShops => seedData?.LocationShops;
        internal Dictionary<long, ItemShopSlot> LocationItemShops => seedData?.LocationItemShops;
        internal Dictionary<long, int> ItemKinds => seedData?.ItemKinds;
        internal List<Blocker> KeptOpen => seedData?.KeptOpen;
        internal List<Blocker> KeptPresent => seedData?.KeptPresent;
        internal List<Blocker> SceneryHidden => seedData?.SceneryHidden;
        internal List<Blocker> SceneryPresent => seedData?.SceneryPresent;
        internal List<Blocker> HeldUntil => seedData?.HeldUntil;
        internal List<Blocker> PresentFrom => seedData?.PresentFrom;
        internal List<Blocker> PresentWithItem => seedData?.PresentWithItem;
        internal List<Blocker> HeldUntilItem => seedData?.HeldUntilItem;
        internal List<DialogueFlag> DialogueFlags => seedData?.DialogueFlags;
        internal List<DoorShuffle.Target> DoorTargets => seedData?.DoorTargets;
        internal Dictionary<string, int[]> EnemySwaps => seedData?.EnemySwaps;
        internal Dictionary<string, string> MusicMap => seedData?.MusicMap;
        internal Dictionary<string, string> JingleMap => seedData?.JingleMap;
        internal List<InventorySpot> ShopInventories => seedData?.ShopInventories;
        internal KeyValuePair<string, int>? Start => seedData?.Start;
        internal string StartFrom => seedData?.StartFrom;
        internal int ArtifactsRequired => seedData?.ArtifactsRequired ?? 0;
        internal int OwnSlot => seedData?.OwnSlot ?? -1;

        // Items the server had sent when this login began: those are a replay, not something arriving during play.
        internal int ReceivedAtLogin => receivedAtLogin;
        private volatile int receivedAtLogin;

        internal sealed class Give
        {
            internal string Map;
            internal int Type;
            internal int Item;
        }

        internal sealed class Pickup
        {
            internal string Map;
            internal int Flag;
            // A story pickup has no activationflag: it's known by the event picking it up starts (its data[1]).
            internal int Event = -1;
            // Crystal berry index (crystalbflags), in data[0].
            internal int Berry = -1;
            // A respawning pickup has only a regional flag, which the game wipes on every area change.
            internal int Regional = -1;
        }

        internal sealed class ItemShopSlot
        {
            internal string Map;
            internal string Keeper;
            internal int Item;
        }

        // A shop slot (Keeper, by the item the map stocks there) or a respawning pickup (Regional), and the item it
        // holds instead (To).
        internal sealed class InventorySpot
        {
            internal string Map;
            internal string Keeper;
            internal int Regional = -1;
            internal int Item;
            internal int To;
        }

        internal sealed class DialogueFlag
        {
            internal string Map;
            internal string Entity;
            internal int From;
            internal int To;
        }

        internal sealed class Blocker
        {
            internal string Map;
            internal string Entity;
            internal int Flag = -1;
            // One of the mod's key items (present_with_item, held_until_item).
            internal int Item = -1;
        }

        // Respawning pickups and item-shop purchases: nothing in the save marks them, so what's done lives here (server
        // list, updates, local checks), and offline ones wait in the outbox tagged with their seed. Memory only; locked
        // across threads.
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

        // A stale seed's checks are dropped.
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
            string seed = ServerText.SeedOf(s);
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

        internal Dictionary<long, ScoutedItemInfo> Scouts => scouts;
        private volatile Dictionary<long, ScoutedItemInfo> scouts;

        // HintCreationPolicy.None: a hint-creating scout would hint every placement in the seed.
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
                        .Select(i => i.LocationId + " = " + i.ShownItem() + " (" + i.ShownGame() + ", for "
                            + i.ShownPlayer() + ")")
                        .ToArray()));
                }
                catch (Exception e)
                {
                    Post("[swap] scouting failed: " + e.GetBaseException().Message);
                }
            });
        }

        // The library resends unconfirmed checks with the next send; offline checks are found in the save's flags at
        // login.
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
                        + " (a flag's check is found again at the next login)");
                }
            });
        }

        // StatusUpdate, as Archipelago asks: the server marks the slot finished and releases per the room's settings.
        internal void SendGoal(ArchipelagoSession s, string why)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    s.Socket.SendPacketAsync(new StatusUpdatePacket { Status = ArchipelagoClientState.ClientGoal })
                        .Wait();
                    Post("[goal] sent: " + why);
                }
                catch (Exception e)
                {
                    Post("[goal] sending failed: " + e.GetBaseException().Message
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
                // A timed-out login says only "Connection timed out.": log what the socket reported on the way.
                var seen = new HashSet<string>();
                attempt.Socket.ErrorReceived += (e, message) =>
                {
                    string line = (e?.GetType().Name ?? "no exception") + ": "
                        + (e?.GetBaseException().Message ?? message);
                    bool first;
                    lock (seen)
                    {
                        first = seen.Add(line);
                    }
                    if (first && ReferenceEquals(session, null))
                    {
                        Post("[ap] socket error while connecting: " + line + "\n" + e);
                    }
                };
                LoginResult result = attempt.TryConnectAndLogin(
                    Game, slot, ItemsHandlingFlags.AllItems,
                    password: string.IsNullOrEmpty(password) ? null : password);

                if (!IsCurrent(number))
                {
                    // Abandoned at its deadline or replaced: close what it opened, report nothing else.
                    KillSocket(attempt);
                    Post("[ap] an abandoned connect attempt finished late (" + result?.GetType().Name + "); ignored");
                    return;
                }

                if (result is LoginSuccessful ok)
                {
                    failures = 0;
                    refused = false;
                    status = $"Connected as {slot}.";
                    Heard();
                    lastPingUtc = DateTime.UtcNow;
                    // Parsed whole before anything is published: a malformed slot_data changes nothing.
                    var parsed = new SeedData(ok.SlotData, ok.Slot);
                    seedData = parsed;
                    // Published last, with a fence: the game thread never sees a live session with the seed's tables
                    // unread.
                    Interlocked.Exchange(ref session, attempt);
                    scouts = null;
                    ResetDone(attempt);
                    Scout(attempt, parsed.ScoutedLocations());
                    attempt.Locations.CheckedLocationsUpdated += ids =>
                    {
                        MarkDone(ids);
                        Post("[check] now checked on the server: "
                            + string.Join(", ", ids.Select(id => id.ToString()).ToArray()));
                    };
                    attempt.Socket.PacketReceived += packet => Heard();
                    attempt.Socket.SocketClosed += reason => MarkLost(attempt, "closed: " + reason);
                    attempt.Socket.ErrorReceived += (e, message) => MarkLost(attempt, "socket error: " + message);
                    receivedAtLogin = attempt.Items.AllItemsReceived.Count;
                    string version = ok.SlotData != null && ok.SlotData.TryGetValue("world_version", out object v)
                        ? v?.ToString() : "missing";
                    // The tag is set after login (a ConnectUpdate), so it never depends on what Connect was sent with.
                    DeathLinkService links = attempt.CreateDeathLinkService();
                    links.OnDeathLinkReceived += death =>
                    {
                        lock (gate)
                        {
                            receivedDeaths.Enqueue(death);
                        }
                    };
                    deathLinks = links;
                    slotName = slot;
                    deathLinkTagged = null;
                    SetDeathLinkTag(DeathLinkWanted != null && DeathLinkWanted());
                    Post($"[ap] logged in: slot {ok.Slot}, team {ok.Team}, world_version {version}, "
                        + $"{attempt.Items.AllItemsReceived.Count} items received so far, "
                        + $"{attempt.Locations.AllLocationsChecked.Count} of {attempt.Locations.AllLocations.Count} locations checked");
                    // A bare address tries wss:// then ws://: only the socket knows which connected.
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
                        // The server said no: retrying the same details cannot help.
                        refused = true;
                        status = "Refused: " + why;
                        Post("[ap] login refused: " + why + " (" + string.Join(", ", failed.ErrorCodes) + ")");
                    }
                    else
                    {
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

        // DeathLink (the Archipelago panel's row): the tag follows the row, received deaths wait here for the game thread.
        internal Func<bool> DeathLinkWanted;
        private volatile DeathLinkService deathLinks;
        private string slotName;
        private readonly Queue<DeathLink> receivedDeaths = new Queue<DeathLink>();
        private bool? deathLinkTagged;

        // Off a worker: the tag change is a ConnectUpdate, sent and waited for.
        internal void SetDeathLinkTag(bool on)
        {
            DeathLinkService links = deathLinks;
            if (links == null || session == null || deathLinkTagged == on)
            {
                return;
            }
            deathLinkTagged = on;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (on)
                    {
                        links.EnableDeathLink();
                    }
                    else
                    {
                        links.DisableDeathLink();
                    }
                    Post("[death] DeathLink tag " + (on ? "added" : "removed"));
                }
                catch (Exception e)
                {
                    deathLinkTagged = null;
                    Post("[death] changing the DeathLink tag failed: " + e.GetBaseException().Message);
                }
            });
        }

        internal bool SendDeath(string cause)
        {
            DeathLinkService links = deathLinks;
            string source = slotName;
            if (links == null || session == null || deathLinkTagged != true)
            {
                return false;
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    links.SendDeathLink(new DeathLink(source, cause));
                    Post("[death] sent: " + cause);
                }
                catch (Exception e)
                {
                    Post("[death] sending failed: " + e.GetBaseException().Message);
                }
            });
            return true;
        }

        // Our own echo never gets here: the library drops a death equal to the last one it sent (name and time).
        internal DeathLink TakeDeath()
        {
            lock (gate)
            {
                return receivedDeaths.Count > 0 ? receivedDeaths.Dequeue() : null;
            }
        }

        internal string SlotName => slotName;

        internal void Post(string message)
        {
            lock (gate)
            {
                messages.Enqueue(message);
            }
        }

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
            deathLinks = null;
            KillSocket(closing);
            failures = 0;
        }

        // An internal field of MultiClient.Net's net40 helper, hence reflection.
        private static WebSocket WebSocketOf(ArchipelagoSession s)
        {
            FieldInfo field = s?.Socket?.GetType().GetField("webSocket",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(s.Socket) as WebSocket;
        }

        // On a worker thread: Close waits up to 5 s for a close frame a dead server never sends. Closing also ends
        // websocket-sharp's receive thread.
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
