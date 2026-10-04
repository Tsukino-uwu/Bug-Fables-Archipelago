using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only, the console's audioprobe: logs each AudioSource played while disabled (Unity's "Can not play a disabled
    // audio source", which names nothing), with its object's path and clip. Read-only prefixes, off until asked.
    internal static class AudioProbe
    {
        private static ManualLogSource log;
        private static Harmony harmony;

        internal static string Toggle(ManualLogSource logger)
        {
            log = logger;
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
                return "audioprobe: off";
            }
            harmony = Hooks.Create("audioprobe");
            var prefix = new HarmonyMethod(AccessTools.Method(typeof(AudioProbe), nameof(BeforePlay)));
            foreach (var target in new[]
            {
                AccessTools.Method(typeof(AudioSource), nameof(AudioSource.Play), Type.EmptyTypes),
                AccessTools.Method(typeof(AudioSource), nameof(AudioSource.PlayDelayed), new[] { typeof(float) }),
                AccessTools.Method(typeof(AudioSource), nameof(AudioSource.PlayOneShot),
                    new[] { typeof(AudioClip), typeof(float) }),
            })
            {
                harmony.Patch(target, prefix: prefix);
            }
            return "audioprobe: on, logging disabled audio sources played";
        }

        private static void BeforePlay(AudioSource __instance)
        {
            if (__instance == null || __instance.isActiveAndEnabled)
            {
                return;
            }
            string path = __instance.name;
            for (Transform at = __instance.transform.parent; at != null; at = at.parent)
            {
                path = at.name + "/" + path;
            }
            string objectState = __instance.gameObject.activeInHierarchy ? "active" : "inactive";
            string clip = __instance.clip != null ? __instance.clip.name : "none";
            string map = MainManager.map != null ? MainManager.map.mapid.ToString() : "none";
            log.LogInfo($"[audio] disabled source played: {path} (object {objectState}, component "
                + $"{(__instance.enabled ? "on" : "off")}), its clip {clip}, map {map}");
        }
    }
}
