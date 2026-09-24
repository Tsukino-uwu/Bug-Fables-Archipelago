using System;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;
using WebSocketSharp;

namespace BugFablesAP
{
    // Compressed connections (permessage-deflate, RFC 7692). The Archipelago server warns every client that
    // doesn't compress that it "may stop working in the future" (MultiServer.py, 0.6.7). Two small patches,
    // because MultiClient.Net 6.7.1 never turns compression on, and websocket-sharp can't accept the server's
    // answer as it stands (upstream issue ArchipelagoMW/Archipelago.MultiClient.Net#141, 2026-09-06):
    //
    // 1. ArchipelagoSocketHelper.CreateWebSocket (private) builds the websocket-sharp socket and connects it
    //    straight away. A postfix sets Compression = Deflate in between, while the socket still allows it.
    // 2. websocket-sharp offers "permessage-deflate; server_no_context_takeover; client_no_context_takeover" and
    //    refuses an answer carrying any other parameter (WebSocket.validateSecWebSocketExtensionsServerHeader).
    //    The server is set up with server_max_window_bits=11 (MultiServer.py), and Python websockets 13.1 always
    //    answers with it (permessage_deflate.py, process_request_params). That parameter only limits the window the
    //    server compresses with; an inflater with the full 15-bit window reads any smaller one (RFC 7692 7.1.2.1).
    //    So a prefix removes it before the check. Anything else still fails the check as before.
    internal static class WebSocketCompression
    {
        private const string ServerWindow = "server_max_window_bits";

        private static Action<string> report;
        private static Func<bool> wanted;
        private static Harmony harmony;

        // `post` must be safe to call from any thread: both patches run on connection threads. `on` is read at
        // each new socket, so the setting applies from the next connect.
        internal static void Enable(string guid, Action<string> post, Func<bool> on)
        {
            report = post;
            wanted = on;
            var create = AccessTools.Method(typeof(ArchipelagoSocketHelper), "CreateWebSocket");
            var validate = AccessTools.Method(typeof(WebSocket), "validateSecWebSocketExtensionsServerHeader");
            if (create == null || validate == null)
            {
                // A library update renamed one of them. Connect uncompressed rather than not at all.
                post("[ws] compression left off: " + (create == null ? "CreateWebSocket" : "validateSecWebSocketExtensionsServerHeader")
                    + " not found");
                return;
            }
            harmony = new Harmony(guid + ".compression." + DateTime.UtcNow.Ticks);
            // The check first: compression must never be switched on without it, or every handshake fails.
            harmony.Patch(validate, prefix: new HarmonyMethod(typeof(WebSocketCompression), nameof(BeforeValidate)));
            harmony.Patch(create, postfix: new HarmonyMethod(typeof(WebSocketCompression), nameof(AfterCreate)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void AfterCreate(WebSocket __result)
        {
            if (__result == null)
            {
                return;
            }
            // websocket-sharp writes its own errors to the console only; a refused handshake would say nothing in
            // BepInEx's log file. Route them to ours.
            __result.Log.Output = (data, file) => report?.Invoke("[ws] " + data.Level + ": " + data.Message);
            bool on = wanted == null || wanted();
            if (on)
            {
                __result.Compression = CompressionMethod.Deflate;
            }
            report?.Invoke("[ws] new socket, compression " + (on ? "requested" : "off (setting)"));
        }

        private static void BeforeValidate(ref string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf(ServerWindow, StringComparison.Ordinal) < 0)
            {
                return;
            }
            string original = value;
            value = string.Join(",", value.Split(',')
                .Select(extension => string.Join(";", extension.Split(';')
                    .Where(parameter => !parameter.Trim().StartsWith(ServerWindow, StringComparison.Ordinal))
                    .ToArray()))
                .ToArray());
            report?.Invoke($"[ws] server answered '{original}'; checked as '{value}'");
        }
    }
}
