using System;
using System.Linq;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;
using WebSocketSharp;

namespace BugFablesAP
{
    // Turns on permessage-deflate: MultiClient.Net never enables it, and websocket-sharp rejects the server's
    // server_max_window_bits, which is safe to drop (a 15-bit inflater reads any smaller window).
    internal static class WebSocketCompression
    {
        private const string ServerWindow = "server_max_window_bits";

        private static Action<string> report;
        private static Func<bool> wanted;

        // `post` must be thread-safe: both patches run on connection threads. `on` is read at each new socket.
        internal static void Enable(Action<string> post, Func<bool> on)
        {
            report = post;
            wanted = on;
            // The check first: compression must never be switched on without it, or every handshake fails.
            // Either missing, the connection is made uncompressed rather than not at all.
            if (Hooks.Install(typeof(Validate), "ws", "compression left off"))
            {
                Hooks.Install(typeof(Create), "ws", "compression left off");
            }
        }

        private static class Create
        {
            [HarmonyPatch(typeof(ArchipelagoSocketHelper), "CreateWebSocket")]
            [HarmonyPostfix]
            private static void AfterCreate(WebSocket __result)
            {
                if (__result == null)
                {
                    return;
                }
                // websocket-sharp logs its errors to the console only; route them to BepInEx's log.
                __result.Log.Output = (data, file) => report?.Invoke("[ws] " + data.Level + ": " + data.Message);
                bool on = wanted == null || wanted();
                // Set both ways, so a library that enables compression itself still obeys the setting.
                __result.Compression = on ? CompressionMethod.Deflate : CompressionMethod.None;
                report?.Invoke("[ws] new socket, compression " + (on ? "requested" : "off (setting)"));
            }
        }

        private static class Validate
        {
            [HarmonyPatch(typeof(WebSocket), "validateSecWebSocketExtensionsServerHeader")]
            [HarmonyPrefix]
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
}
