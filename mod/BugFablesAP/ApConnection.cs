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
                    status = $"Connected as {slot}.";
                    string version = ok.SlotData != null && ok.SlotData.TryGetValue("world_version", out object v) ? v?.ToString() : "missing";
                    Post($"[ap] logged in: slot {ok.Slot}, team {ok.Team}, world_version {version}, "
                        + $"{attempt.Items.AllItemsReceived.Count} items received so far, "
                        + $"{attempt.Locations.AllLocationsChecked.Count} of {attempt.Locations.AllLocations.Count} locations checked");
                }
                else if (result is LoginFailure failed)
                {
                    status = "Refused: " + string.Join("; ", failed.Errors);
                    Post("[ap] login refused: " + string.Join("; ", failed.Errors));
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
                status = "Could not connect: " + e.GetBaseException().Message;
                Post("[ap] connect threw: " + e);
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
            try
            {
                session?.Socket.DisconnectAsync();
            }
            catch (Exception e)
            {
                log.LogWarning("[ap] disconnect threw: " + e.Message);
            }
            session = null;
        }
    }
}
