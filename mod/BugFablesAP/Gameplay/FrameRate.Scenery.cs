using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Uncap FPS: scenery the game swings or bobs inside physics steps (StaticModelAnim, 50 times a second: the Rubber
    // Prison's swinging platforms, boats, floating things) is drawn between its last two steps, as the camera is, and
    // put back after the frame's last camera, so the game only ever reads its true pose.
    internal static partial class FrameRate
    {
        internal static bool SmoothScenery;

        private sealed class Scenery
        {
            internal StaticModelAnim Anim;
            internal Vector3 PrevPos, CurrPos, SavedPos;
            internal Quaternion PrevRot, CurrRot, SavedRot;
            internal int Samples;
            internal KeepAngle[] Level;
            internal Quaternion[] LevelSaved;
            internal bool[] Levelled;
        }

        private static readonly Dictionary<int, Scenery> scenery = new Dictionary<int, Scenery>();
        private static readonly List<Scenery> shiftedScenery = new List<Scenery>();
        private static readonly List<int> goneScenery = new List<int>();
        // A swing further than this in one step is a cut, not motion.
        private const float CutAngle = 30f;

        [HarmonyPatch(typeof(StaticModelAnim), "Start")]
        [HarmonyPostfix]
        private static void AfterStaticModelAnimStart(StaticModelAnim __instance)
        {
            if (active)
            {
                TrackScenery(__instance);
            }
        }

        private static void TrackScenery(StaticModelAnim anim)
        {
            int id = anim.GetInstanceID();
            if (!scenery.ContainsKey(id))
            {
                scenery[id] = new Scenery { Anim = anim };
            }
        }

        private static void TrackAllScenery()
        {
            scenery.Clear();
            foreach (StaticModelAnim anim in Resources.FindObjectsOfTypeAll<StaticModelAnim>())
            {
                if (anim.gameObject.scene.IsValid())
                {
                    TrackScenery(anim);
                }
            }
        }

        // StaticModelAnim.FixedUpdate's own test for writing a swing or a bob.
        private static bool Moves(StaticModelAnim a) => a.enabled && a.gameObject.activeInHierarchy && !a.nomove
            && a.bobspeed != Vector3.zero && (a.bobangle != Vector3.zero || !a.stopbob);

        // In the parent's space, so a parent moved outside the step doesn't count as the swing.
        private static void SampleScenery()
        {
            foreach (KeyValuePair<int, Scenery> pair in scenery)
            {
                Scenery s = pair.Value;
                if (s.Anim == null)
                {
                    goneScenery.Add(pair.Key);
                    continue;
                }
                if (!Moves(s.Anim))
                {
                    s.Samples = 0;
                    continue;
                }
                Transform t = s.Anim.transform;
                s.PrevPos = s.CurrPos;
                s.PrevRot = s.CurrRot;
                s.CurrPos = t.localPosition;
                s.CurrRot = t.localRotation;
                if (s.Samples < 2)
                {
                    s.Samples++;
                }
                if ((s.CurrPos - s.PrevPos).sqrMagnitude > CutDistance * CutDistance
                    || Quaternion.Angle(s.PrevRot, s.CurrRot) > CutAngle)
                {
                    s.PrevPos = s.CurrPos;
                    s.PrevRot = s.CurrRot;
                }
            }
            foreach (int id in goneScenery)
            {
                scenery.Remove(id);
            }
            goneScenery.Clear();
        }

        // After DrawBodies: a character standing on the swing is set back in its parent's space, then carried to the
        // swing's drawn pose with it.
        private static void DrawScenery(float alpha)
        {
            if (!SmoothScenery)
            {
                return;
            }
            foreach (Scenery s in scenery.Values)
            {
                if (s.Anim == null || s.Samples < 2)
                {
                    continue;
                }
                Transform t = s.Anim.transform;
                Vector3 pos = t.localPosition;
                Quaternion rot = t.localRotation;
                if (pos != s.CurrPos || rot != s.CurrRot)
                {
                    // Moved outside a physics step (a scene placing it): no motion to smooth.
                    s.Samples = 0;
                    continue;
                }
                if (Exactly(s.PrevPos, s.CurrPos) && Exactly(s.PrevRot, s.CurrRot))
                {
                    continue;
                }
                s.SavedPos = pos;
                s.SavedRot = rot;
                t.localPosition = Vector3.Lerp(s.PrevPos, s.CurrPos, alpha);
                t.localRotation = Quaternion.Slerp(s.PrevRot, s.CurrRot, alpha);
                LevelUnder(s);
                shiftedScenery.Add(s);
            }
        }

        // Unity's == counts rotations under about 0.16 degrees apart as equal, and the Rubber Prison's swing turns about
        // that much in a physics step.
        private static bool Exactly(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        private static bool Exactly(Quaternion a, Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z
            && a.w == b.w;

        // KeepAngle holds its object's world angle in LateUpdate (the crane platform hanging level under its swing arm),
        // so under a drawn swing it's held again for the draw.
        private static void LevelUnder(Scenery s)
        {
            if (s.Level == null)
            {
                s.Level = s.Anim.GetComponentsInChildren<KeepAngle>(true);
                s.LevelSaved = new Quaternion[s.Level.Length];
                s.Levelled = new bool[s.Level.Length];
            }
            for (int i = 0; i < s.Level.Length; i++)
            {
                KeepAngle k = s.Level[i];
                s.Levelled[i] = k != null && k.enabled && k.gameObject.activeInHierarchy;
                if (s.Levelled[i])
                {
                    s.LevelSaved[i] = k.transform.localRotation;
                    k.transform.eulerAngles = k.angle;
                }
            }
        }

        private static void RestoreScenery()
        {
            foreach (Scenery s in shiftedScenery)
            {
                if (s.Anim == null)
                {
                    continue;
                }
                s.Anim.transform.localPosition = s.SavedPos;
                s.Anim.transform.localRotation = s.SavedRot;
                for (int i = 0; i < s.Level.Length; i++)
                {
                    if (s.Levelled[i] && s.Level[i] != null)
                    {
                        s.Level[i].transform.localRotation = s.LevelSaved[i];
                    }
                }
            }
            shiftedScenery.Clear();
        }
    }
}
