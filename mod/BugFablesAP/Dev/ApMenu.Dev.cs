using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    internal sealed partial class ApMenu
    {
        // Dev (console `menuinfo`): what the panel's text holds right now, to see why a page shows no text.
        internal string TextReport()
        {
            if (textRoot == null)
            {
                return "no text root";
            }
            var report =
                new System.Text.StringBuilder($"page {page}, text root active {textRoot.gameObject.activeInHierarchy}, "
                + $"layer {textRoot.gameObject.layer}, pieces {textRoot.childCount}");
            for (int i = 0; i < textRoot.childCount && i < 6; i++)
            {
                Transform piece = textRoot.GetChild(i);
                int letters = 0, shown = 0;
                foreach (TextMesh letter in piece.GetComponentsInChildren<TextMesh>(true))
                {
                    letters++;
                    if (letter.gameObject.activeInHierarchy && letter.text != "")
                    {
                        shown++;
                    }
                }
                report.Append($"; piece {i} active {piece.gameObject.activeInHierarchy} layer {piece.gameObject.layer} letters {letters} shown {shown}");
            }
            TextMesh first = textRoot.GetComponentInChildren<TextMesh>(true);
            if (first != null)
            {
                MeshRenderer lr = first.GetComponent<MeshRenderer>();
                report.Append($"; first letter '{first.text}' layer {first.gameObject.layer} sort {lr.sortingLayerName}/{lr.sortingOrder} "
                    + $"z {first.transform.position.z:0.00} alpha {first.color.a:0.00} shader {lr.sharedMaterial?.shader?.name} queue {lr.sharedMaterial?.renderQueue}");
            }
            SpriteRenderer back = box != null ? box.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (back != null)
            {
                report.Append($"; box '{back.name}' layer {back.gameObject.layer} sort {back.sortingLayerName}/{back.sortingOrder} z {back.transform.position.z:0.00} "
                    + $"shader {back.sharedMaterial?.shader?.name} queue {back.sharedMaterial?.renderQueue}");
            }
            Camera gui = MainManager.GUICamera != null ? MainManager.GUICamera.GetComponent<Camera>() : null;
            if (gui != null)
            {
                report.Append($"; GUI camera mask {gui.cullingMask} at {gui.transform.position} rot {gui.transform.eulerAngles} ortho {gui.orthographic} "
                    + $"near {gui.nearClipPlane} far {gui.farClipPlane}");
                // Ours against one of the game's own letters (the Settings list, which shows), in the camera's own
                // frame.
                var pool = (TextMesh[])AccessTools.Field(typeof(MainManager), "letterpool").GetValue(null);
                TextMesh game = System.Linq.Enumerable.FirstOrDefault(pool, l => l != null && l.text != ""
                    && l.transform.parent != null && l.transform.parent.parent != null
                    && l.transform.parent.parent.name.StartsWith("Bar"));
                foreach (var pair in new[] { new System.Collections.Generic.KeyValuePair<string, TextMesh>("ours",
                    first), new System.Collections.Generic.KeyValuePair<string, TextMesh>("game", game) })
                {
                    if (pair.Value == null)
                    {
                        report.Append($"; {pair.Key}: none");
                        continue;
                    }
                    Transform t = pair.Value.transform;
                    MeshRenderer r = pair.Value.GetComponent<MeshRenderer>();
                    report.Append($"; {pair.Key} '{pair.Value.text}' in camera {gui.transform.InverseTransformPoint(t.position)} scale {t.lossyScale} "
                        + $"rot {t.eulerAngles} layer {t.gameObject.layer} sort {r.sortingOrder} visible {r.isVisible} enabled {r.enabled}");
                }
            }
            return report.ToString();
        }
    }
}
