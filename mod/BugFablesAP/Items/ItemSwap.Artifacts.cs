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
        // Each scene: its map, its chapter (slot_data's location_artifacts), the map's child holding the artifact's
        // model (-1: the scene makes a sprite instead), and the map's dialogue line its banner shows.
        private static readonly (string Map, int Chapter, int Child, int Line)[] ArtifactScenes =
        {
            ("SnakemouthTreasureRoom", 1, 1, 7),
            ("RubberPrisonGiantLairBridge", 6, -1, 15),
        };
        private const string ArtifactLookName = "apartifactlook";
        // The sprite a sprite scene shows instead of its artifact, for the map it was set on.
        private static Sprite artifactSprite;
        private static string artifactSpriteMap;

        private static class Artifacts
        {
            [HarmonyPatch(typeof(MapControl), "CreateEntities")]
            [HarmonyPostfix]
            private static void AfterCreate(MapControl __instance) => ShowArtifactScene(__instance);

            [HarmonyPatch(typeof(EventControl), "Event194", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "swap");

            // The Wasp Queen's `Resources.LoadAll<Sprite>("Sprites/Objects/artifacts")[5]`: the element read becomes
            // HeldArtifact(sheet, 5), the same stack in and out.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
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
                    throw new InvalidOperationException($"Event194 has {reads.Length} artifact sprite reads where 1 "
                        + "was measured");
                }
                code[reads[0] + 2].opcode = OpCodes.Call;
                code[reads[0] + 2].operand = AccessTools.Method(typeof(ItemSwap), nameof(HeldArtifact));
                log.LogInfo("[artifact] installed in EventControl.Event194");
                return code;
            }
        }

        public static Sprite HeldArtifact(Sprite[] sheet, int index)
        {
            string map = MainManager.map != null ? MainManager.map.mapid.ToString() : null;
            if (artifactSprite != null && map == artifactSpriteMap)
            {
                log.LogInfo($"[artifact] {map}: the scene holds up its location's item, not artifact sprite {index}");
                return artifactSprite;
            }
            return sheet[index];
        }

        private static void ShowArtifactScene(MapControl map)
        {
            string mapName = map.mapid.ToString();
            if (mapName != artifactSpriteMap)
            {
                artifactSprite = null;
                artifactSpriteMap = null;
            }
            Dictionary<long, int> artifacts = connection?.LocationArtifacts;
            if (randomizerOn == null || !randomizerOn() || artifacts == null)
            {
                return;
            }
            foreach (var scene in ArtifactScenes.Where(s => s.Map == mapName))
            {
                long at = artifacts.Where(a => a.Value == scene.Chapter).Select(a => a.Key).DefaultIfEmpty(-1).First();
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
                bool own = info != null && IsOurs(info) && KindOf(info) == ItemIds.ArtifactKind
                    && ItemIds.GameId(info.ItemId, ItemIds.ArtifactKind) == scene.Chapter;
                if (!own && map.dialogues != null && scene.Line < map.dialogues.Length)
                {
                    map.dialogues[scene.Line] = BannerText(info, name);
                }
                if (scene.Child < 0)
                {
                    artifactSprite = sprite;
                    artifactSpriteMap = mapName;
                }
                else if (scene.Child < map.transform.childCount)
                {
                    HideModel(map.transform.GetChild(scene.Child), sprite, color);
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
                SpriteRenderer mark = MainManager.NewSpriteObject(MarkName, new Vector3(0f, 0f, MarkBehind), Vector3.zero,
                    look, MainManager.guisprites[85], MainManager.spritedefaultunity);
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
