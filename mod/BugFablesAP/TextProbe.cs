using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only measurement: the game's events and pickups are dialogue scripts passed to MainManager.SetText,
    // with commands such as |giveitem,1,27| and |flag,15,true| inline (MainManager.cs:10626). This logs every
    // script that contains an item command, together with the map and the calling NPC. That shows which flag
    // travels with which grant, so the two can be tied together as one location.
    //
    // Read-only: a Harmony prefix that logs its arguments and always lets the original run.
    // Patches the 10-argument overload that the other three SetText overloads all call.
    internal static class TextProbe
    {
        private static ManualLogSource log;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid)
        {
            log = logger;
            harmony = new Harmony(guid + ".textprobe");
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
            // ScriptEngine reloads leave old patches behind unless removed; a second copy would log twice.
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
            log.LogInfo($"[text] map={where} caller={who} script={shown}");
        }
    }
}
