using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;
using WebSocketSharp;

namespace BugFablesAP
{
    // Dev only: for each wss:// socket, logs what this Mono's own certificate check decided about the server (policy
    // errors, the chain it built), then accepts the certificate as websocket-sharp's default does, so connecting is
    // unchanged. Measures whether certificates could be checked here at all before the mod ever does.
    internal static class TlsProbe
    {
        private static Action<string> report;

        // `post` must be thread-safe: the socket is made, and its certificate checked, on connection threads.
        internal static void Enable(Action<string> post)
        {
            report = post;
            if (Hooks.Install(typeof(TlsProbe), "tls", "TlsProbe is off"))
            {
                report("[tls] TlsProbe on: each wss:// certificate check is logged, and accepted as before");
            }
        }

        [HarmonyPatch(typeof(ArchipelagoSocketHelper), "CreateWebSocket")]
        [HarmonyPostfix]
        private static void AfterCreate(WebSocket __result)
        {
            if (__result == null || !__result.IsSecure)
            {
                report?.Invoke($"[tls] {__result?.Url}: not a secure socket, nothing to check");
                return;
            }
            string url = __result.Url.ToString();
            __result.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                Report(url, certificate, chain, errors);
                return true;
            };
        }

        private static void Report(string url, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            try
            {
                report?.Invoke($"[tls] {url}: policy errors {errors}; certificate '{certificate?.Subject}' from "
                    + $"'{certificate?.Issuer}', until {certificate?.GetExpirationDateString()}");
                if (chain == null)
                {
                    report?.Invoke("[tls] no chain given");
                    return;
                }
                string[] links = chain.ChainElements.Cast<X509ChainElement>().Select(e => e.Certificate.Subject)
                    .ToArray();
                report?.Invoke($"[tls] chain of {links.Length}: {string.Join(" <- ", links)}");
                foreach (X509ChainStatus status in chain.ChainStatus)
                {
                    report?.Invoke($"[tls] chain status {status.Status}: {status.StatusInformation?.Trim()}");
                }
            }
            catch (Exception e)
            {
                report?.Invoke("[tls] reading the certificate threw: " + e.GetBaseException().Message);
            }
        }
    }
}
