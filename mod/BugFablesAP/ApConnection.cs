using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using BepInEx.Logging;

namespace BugFablesAP
{
    // The connection to the Archipelago server. Step 1 of the build: connect, log in, report what the server
    // said. No items are granted and no checks are sent yet.
    //
    // TryConnectAndLogin blocks for up to 5 s (MultiClient.Net's own docs), so it runs on a worker thread and
    // its outcome is handed to the game thread through a queue. Nothing here touches game state.
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
            if ((nowUtc - lastPingUtc).TotalSeconds >= PingSeconds)
            {
                lastPingUtc = nowUtc;
                try
                {
                    s.Socket.SendPacketAsync(new GetPacket { Keys = new[] { "_read_race_mode" } });
                }
                catch (Exception e)
                {
                    MarkLost(s, "sending failed: " + e.GetBaseException().Message);
                }
            }
        }

        private void Heard()
        {
            Interlocked.Exchange(ref lastHeardTicks, DateTime.UtcNow.Ticks);
        }

        private void MarkLost(ArchipelagoSession s, string reason)
        {
            if (!ReferenceEquals(session, s))
            {
                return;
            }
            session = null;
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
                    attempt.Socket.PacketReceived += packet => Heard();
                    attempt.Socket.SocketClosed += reason => MarkLost(attempt, "closed: " + reason);
                    attempt.Socket.ErrorReceived += (e, message) => MarkLost(attempt, "socket error: " + message);
                    string version = ok.SlotData != null && ok.SlotData.TryGetValue("world_version", out object v) ? v?.ToString() : "missing";
                    Post($"[ap] logged in: slot {ok.Slot}, team {ok.Team}, world_version {version}, "
                        + $"{attempt.Items.AllItemsReceived.Count} items received so far, "
                        + $"{attempt.Locations.AllLocationsChecked.Count} of {attempt.Locations.AllLocations.Count} locations checked");
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
                // library at all (ClientWebSocket, no System.Reflection.Emit), so report the whole exception.
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

        private void Post(string message)
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
            ArchipelagoSession closing = session;
            session = null; // first, so the SocketClosed handler knows this close was on purpose
            KillSocket(closing);
            failures = 0;
        }

        // MultiClient.Net's receive loop is `while (Socket.State == Open)`, catching and reporting every error
        // (BaseArchipelagoSocketHelper.PollingLoop, 6.7.1). This game's Mono ManagedWebSocket throws
        // ConnectionClosedPrematurely from ReceiveAsync without leaving the Open state when the server vanishes
        // (System.dll, ManagedWebSocket.ReceiveAsyncPrivate), so the loop spun forever: measured 2026-09-24 as five
        // thread-pool threads at ~75% of a core each and memory growing ~2.5 MB/s after a server drop.
        // ClientWebSocket.Abort() moves the state to Aborted (ManagedWebSocket's abort registration), which ends
        // both library loops; the socket field is internal, hence reflection. DisconnectAsync alone doesn't: its
        // close frame fails on a dead stream and leaves the state as it was.
        private void KillSocket(ArchipelagoSession s)
        {
            if (s == null)
            {
                return;
            }
            try
            {
                object helper = s.Socket;
                FieldInfo field = null;
                for (Type t = helper.GetType(); t != null && field == null; t = t.BaseType)
                {
                    field = t.GetField("Socket", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                        | BindingFlags.DeclaredOnly);
                }
                if (!(field?.GetValue(helper) is WebSocket socket))
                {
                    Post("[ap] could not reach the web socket to close it (field "
                        + (field == null ? "missing" : "not a WebSocket") + ")");
                    return;
                }
                WebSocketState before = socket.State;
                socket.Abort();
                // The send loop waits in BlockingCollection.Take(); one more packet wakes it, it sees the socket is
                // no longer open and ends. The packet itself is never sent.
                s.Socket.SendPacketAsync(new GetPacket { Keys = new[] { "_read_race_mode" } });
                Post($"[ap] socket closed: {before} -> {socket.State}");
            }
            catch (Exception e)
            {
                Post("[ap] closing the socket threw: " + e.GetBaseException().Message);
            }
        }
    }
}
