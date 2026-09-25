using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: logs every dialogue script (MainManager.SetText, the 10-argument overload the others call) that carries
    // an item command, with the map and calling NPC. Read-only prefix.
    internal static class TextProbe
    {
        private static ManualLogSource log;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid)
        {
            log = logger;
            // A distinct id per load: a shared id's UnpatchSelf from the old instance would strip the new patch too.
            harmony = new Harmony(guid + ".textprobe." + DateTime.UtcNow.Ticks);
            var target = AccessTools.Method(typeof(MainManager), "SetText", new[]
            {
                typeof(string), typeof(int), typeof(float?), typeof(bool), typeof(bool),
                typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(Transform), typeof(NPCControl)
            });
            if (target == null)
            {
                log.LogWarning("[text] MainManager.SetText (10 arguments) not found; TextProbe is off.");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(TextProbe), nameof(Prefix)));
            log.LogInfo("[text] TextProbe on: logging SetText scripts that carry an item command.");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void Prefix(string text, NPCControl caller)
        {
            if (text == null || log == null)
            {
                return;
            }
            if (text.IndexOf("item", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }
            if (text.IndexOf("giveitem", StringComparison.OrdinalIgnoreCase) < 0
                && text.IndexOf("additem", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }
            MapControl map = MainManager.map;
            string where = map == null ? "none" : $"{map.mapid}/{map.areaid}";
            string who = caller == null ? "none" : caller.name;
            string shown = text.Length > 800 ? text.Substring(0, 800) + "…(" + text.Length + " chars)" : text;
            // World pickups pass the item id as "var,0": NPCControl.CheckItem puts it in flagvar[0] first.
            string var0 = "";
            if (text.IndexOf(",var,0", StringComparison.Ordinal) >= 0 && MainManager.instance?.flagvar != null)
            {
                int id = MainManager.instance.flagvar[0];
                var0 = $" flagvar[0]={id} ({(MainManager.Items)id})";
            }
            log.LogInfo($"[text] map={where} caller={who}{var0} script={shown}");
        }
    }
}
