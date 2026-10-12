using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BugFablesAP
{
    // An artifact's own scene shows its location's item: the artifact's model hidden behind the item's sprite, or the
    // sprite the scene makes swapped, and its banner naming the item unless the location holds its own artifact.
    internal static partial class ItemSwap
    {
        // Each scene: its map, its chapter (slot_data's location_artifacts), the artifact's model (null: none; the
        // scene makes a sprite instead), and the map's dialogue line its banner shows.
        private static readonly (string Map, int Chapter, Func<MapControl, Transform> Model, int Line)[]
            ArtifactScenes =
        {
            ("SnakemouthTreasureRoom", 1, m => Child(m.transform, 1), 7),
            // The tablet inside the Venus' Guardian's flower.
            ("GoldenHillsDungeonBoss", 2, m => Child(Child(m.mainmesh, 1), 0), 11),
            ("RubberPrisonGiantLairBridge", 6, null, 15),
        };

        private static Transform Child(Transform parent, int index) =>
            parent != null && index < parent.childCount ? parent.GetChild(index) : null;
        private const string ArtifactLookName = "apartifactlook";

        private static class Artifacts
        {
            [HarmonyPatch(typeof(MapControl), "CreateEntities")]
            [HarmonyPostfix]
            private static void AfterCreate(MapControl __instance) => ShowArtifactScene(__instance);

            // Snakemouth's artifact lying outside the cave, after the pedestal (its sprite 0).
            [HarmonyPatch(typeof(EventControl), "Event26", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Snakemouth(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, "Event26", nameof(HeldArtifact1)), "swap");

            // The Wasp Queen's gift (its sprite 5).
            [HarmonyPatch(typeof(EventControl), "Event194", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Queen(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => Edit(code, "Event194", nameof(HeldArtifact6)), "swap");

            // The scene's one `Resources.LoadAll<Sprite>("Sprites/Objects/artifacts")[n]`: the element read becomes
            // the chapter's HeldArtifact(sheet, n), the same stack in and out.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code, string scene, string held)
            {
                MethodInfo loadAll = typeof(Resources).GetMethods()
                    .First(m => m.Name == nameof(Resources.LoadAll) && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 1)
                    .MakeGenericMethod(typeof(Sprite));
                int[] reads = Enumerable.Range(1, Math.Max(0, code.Count - 3))
                    .Where(i => code[i].Calls(loadAll) && code[i - 1].operand as string == "Sprites/Objects/artifacts"
                        && code[i + 2].opcode == OpCodes.Ldelem_Ref)
                    .ToArray();
                if (reads.Length != 1)
                {
                    throw new InvalidOperationException($"{scene} has {reads.Length} artifact sprite reads where 1 "
                        + "was measured");
                }
                code[reads[0] + 2].opcode = OpCodes.Call;
                code[reads[0] + 2].operand = AccessTools.Method(typeof(ItemSwap), held);
                log.LogInfo($"[artifact] installed in EventControl.{scene}");
                return code;
            }
        }

        public static Sprite HeldArtifact1(Sprite[] sheet, int index) => HeldArtifact(sheet, index, 1);

        public static Sprite HeldArtifact6(Sprite[] sheet, int index) => HeldArtifact(sheet, index, 6);

        // The chapter's location's item, unless the scene isn't a location or holds its own artifact.
        private static Sprite HeldArtifact(Sprite[] sheet, int index, int chapter)
        {
            long at = ArtifactLocation(chapter);
            if (at < 0)
            {
                return sheet[index];
            }
            string article = shownArticle;
            ScoutedItemInfo info = Describe(at, out string name, out Sprite sprite, out _);
            shownArticle = article;
            if (OwnArtifact(info, chapter))
            {
                return sheet[index];
            }
            log.LogInfo($"[artifact] chapter {chapter}'s scene shows '{TextFit.Joined(name)}', "
                + $"not artifact sprite {index}");
            return sprite ?? ApIcon.Get();
        }

        private static long ArtifactLocation(int chapter)
        {
            Dictionary<long, int> artifacts = connection?.LocationArtifacts;
            if (randomizerOn == null || !randomizerOn() || artifacts == null)
            {
                return -1;
            }
            return artifacts.Where(a => a.Value == chapter).Select(a => a.Key).DefaultIfEmpty(-1).First();
        }

        private static bool OwnArtifact(ScoutedItemInfo info, int chapter) =>
            info != null && IsOurs(info) && KindOf(info) == ItemIds.ArtifactKind
            && ItemIds.GameId(info.ItemId, ItemIds.ArtifactKind) == chapter;

        private static void ShowArtifactScene(MapControl map)
        {
            string mapName = map.mapid.ToString();
            foreach (var scene in ArtifactScenes.Where(s => s.Map == mapName))
            {
                long at = ArtifactLocation(scene.Chapter);
                if (at < 0)
                {
                    log.LogInfo($"[artifact] {mapName}: chapter {scene.Chapter}'s scene isn't a location in this seed: "
                        + "the game's own");
                    continue;
                }
                // Describe sets the article a give then uses: kept as it was.
                string article = shownArticle;
                ScoutedItemInfo info = Describe(at, out string name, out Sprite sprite, out Color? color);
                shownArticle = article;
                sprite = sprite ?? ApIcon.Get();
                bool own = OwnArtifact(info, scene.Chapter);
                if (!own && map.dialogues != null && scene.Line < map.dialogues.Length)
                {
                    map.dialogues[scene.Line] = BannerText(info, name);
                }
                // A sprite scene's sprite is swapped as the scene makes it (HeldArtifact).
                Transform model = scene.Model?.Invoke(map);
                if (model != null)
                {
                    HideModel(model, sprite, color);
                }
                else if (scene.Model != null)
                {
                    log.LogInfo($"[artifact] {mapName}: the artifact's model isn't where it was read: left as it is");
                }
                log.LogInfo($"[artifact] {mapName}: location {at} shows '{TextFit.Joined(name)}'"
                    + (own ? ", its own artifact: the game's banner" : ", the banner names it")
                    + (info == null ? " (not scouted yet)" : ""));
            }
        }

        // "You found <item>!", as a pickup says it; another player's item as "<player>'s <item>".
        private static string BannerText(ScoutedItemInfo info, string name)
        {
            string article = info != null && IsOurs(info) && info.Player.Slot == connection.OwnSlot
                ? ArticleOf(info.ItemId, KindOf(info)) : "";
            ForOther(info, ref name);
            string shown = TextFit.Joined(name);
            return FoundWords + (string.IsNullOrEmpty(article) ? "" : article + " ") + "|color,1|" + shown
                + "|color,0|!";
        }

        // The model's renderers off and the item's sprite in its place, a child the scene moves, raises and hides with
        // it; its class colour behind it as on the ground.
        private static void HideModel(Transform model, Sprite sprite, Color? color)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name != ArtifactLookName && r.gameObject.name != MarkName)
                {
                    r.enabled = false;
                }
            }
            Transform look = model.Find(ArtifactLookName);
            if (look == null)
            {
                SpriteRenderer shown = MainManager.NewSpriteObject(ArtifactLookName, Vector3.zero, Vector3.zero, model,
                    sprite, MainManager.spritedefaultunity);
                look = shown.transform;
                look.gameObject.AddComponent<ArtifactLook>();
            }
            look.GetComponent<SpriteRenderer>().sprite = sprite;
            Transform back = look.Find(MarkName);
            if (color.HasValue && back == null)
            {
                SpriteRenderer mark = MainManager.NewSpriteObject(MarkName, new Vector3(0f, 0f, MarkBehind),
                    Vector3.zero, look, MainManager.guisprites[85], MainManager.spritedefaultunity);
                mark.transform.localScale = Vector3.one * MarkScale;
                back = mark.transform;
            }
            if (back != null)
            {
                SpriteRenderer mark = back.GetComponent<SpriteRenderer>();
                mark.enabled = color.HasValue;
                if (color.HasValue)
                {
                    mark.color = color.Value;
                }
            }
        }

        // Upright and item-sized whatever the model's spin and scale, and drawn on the layer the scene puts the model
        // on (its hold-up draws it above the fade).
        private sealed class ArtifactLook : MonoBehaviour
        {
            private void LateUpdate()
            {
                Transform parent = transform.parent;
                if (parent == null)
                {
                    return;
                }
                transform.rotation = Quaternion.identity;
                Vector3 scale = parent.lossyScale;
                transform.localScale = new Vector3(1f / NonZero(scale.x), 1f / NonZero(scale.y), 1f / NonZero(scale.z));
                int layer = parent.gameObject.layer;
                if (gameObject.layer != layer)
                {
                    gameObject.layer = layer;
                    foreach (Transform child in transform)
                    {
                        child.gameObject.layer = layer;
                    }
                }
            }

            private static float NonZero(float value) => Mathf.Abs(value) < 0.0001f ? 1f : value;
        }
    }
}
