using System;
using System.Collections.Generic;
using System.Threading;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
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
        private bool busy;
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
            var worker = new Thread(() => ConnectOnWorker(server, slot, password)) { IsBackground = true };
            worker.Start();
        }

        private void ConnectOnWorker(string server, string slot, string password)
        {
            try
            {
                ArchipelagoSession attempt = ArchipelagoSessionFactory.CreateSession(server);
                LoginResult result = attempt.TryConnectAndLogin(
                    Game, slot, ItemsHandlingFlags.AllItems, password: string.IsNullOrEmpty(password) ? null : password);

                if (result is LoginSuccessful ok)
                {
                    session = attempt;
                    failures = 0;
                    refused = false;
                    status = $"Connected as {slot}.";
                    attempt.Socket.SocketClosed += reason =>
                    {
                        // Only this session dropping counts; Disconnect() clears `session` first.
                        if (!ReferenceEquals(session, attempt))
                        {
                            return;
                        }
                        session = null;
                        Post("[ap] connection lost: " + reason);
                        ScheduleRetry("Connection lost.");
                    };
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
            catch (Exception e)
            {
                // The first connect is also the measurement of whether this game's Mono can run the client
                // library at all (ClientWebSocket, no System.Reflection.Emit), so report the whole exception.
                Post("[ap] connect threw: " + e);
                ScheduleRetry("Could not connect: " + e.GetBaseException().Message + ".");
            }
            finally
            {
                busy = false;
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
            try
            {
                closing?.Socket.DisconnectAsync();
            }
            catch (Exception e)
            {
                log.LogWarning("[ap] disconnect threw: " + e.Message);
            }
            failures = 0;
        }
    }
}
