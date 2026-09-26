using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The travel buttons after the pause menu's four (Quality of life, Travel: Warp / Map / Both), each behind a
    // Yes / No box. Warp to start: the game's own map transfer to where a new game begins. Map: the game's own map
    // window in a travel mode, where confirm on a visited area travels to its save point (the map opened any other way
    // keeps vanilla controls). The logic never counts on either. Confirm is caught before the game would act on it.
    internal static class WarpButton
    {
        private static ManualLogSource log;
        private static Func<bool> warpOn;
        private static Func<bool> mapOn;
        private static Harmony harmony;

        private static readonly FieldInfo optionField = AccessTools.Field(typeof(PauseMenu), "option");
        private static readonly FieldInfo maxField = AccessTools.Field(typeof(PauseMenu), "maxoptions");
        private static readonly FieldInfo spritesField = AccessTools.Field(typeof(PauseMenu), "sprites");
        private static readonly FieldInfo boxesField = AccessTools.Field(typeof(PauseMenu), "boxes");
        private static readonly MethodInfo prepareExit = AccessTools.Method(typeof(PauseMenu), "PrepareExit");
        private static readonly MethodInfo buildWindow = AccessTools.Method(typeof(PauseMenu), "BuildWindow");

        private enum Kind { Warp, Map }
        // The game's four buttons are options 0-3 (sprites 13-16); the travel buttons follow as options 4 and 5. Sprite 18
        // is the map shortcut, so a second travel button takes sprite 19 in a grown array.
        private const int FirstOption = 4;
        private static readonly int[] SpriteSlot = { 17, 19 };
        // The round icon in the other buttons' style (a blue map). Alone, either button uses it as is; with both, Warp's
        // is tinted warm so the two tell apart (the user).
        private const int IconSprite = 34;
        private static readonly Color BothWarpTint = new Color(1f, 0.75f, 0.45f);
        // By its save point: entity 1 (SaveTutorial) before flag 41, entity 22 (SaveAfterTutorial) after.
        private const MainManager.Maps StartMap = MainManager.Maps.BugariaOutskirtsOutsideCity;

        // Each area's travel spot: a save point at its entrance or hub (starting choices; the Outskirts use the start).
        private static readonly Dictionary<int, KeyValuePair<MainManager.Maps, int>> AreaSpots = new Dictionary<int, KeyValuePair<MainManager.Maps, int>>
        {
            { 1, Spot(MainManager.Maps.BugariaMainPlaza, 6) },
            { 2, Spot(MainManager.Maps.SnakemouthFallRoom, 0) },
            { 3, Spot(MainManager.Maps.DesertCaravanMap, 0) },
            { 4, Spot(MainManager.Maps.GoldenHillsDungeonEntrance, 0) },
            { 5, Spot(MainManager.Maps.GoldenHillsCableCar, 0) },
            { 6, Spot(MainManager.Maps.GoldenSettlement1, 1) },
            { 7, Spot(MainManager.Maps.BarrenLandsEntrance, 0) },
            { 8, Spot(MainManager.Maps.FarGrasslands1, 5) },
            { 9, Spot(MainManager.Maps.SwamplandsBridge, 6) },
            { 10, Spot(MainManager.Maps.DefiantRoot1, 0) },
            { 11, Spot(MainManager.Maps.SandCastleMainRoom, 0) },
            { 12, Spot(MainManager.Maps.BeehiveMainArea, 3) },
            { 13, Spot(MainManager.Maps.HoneyFactoryEntrance, 4) },
            { 14, Spot(MainManager.Maps.RubberPrisonPier, 0) },
            { 15, Spot(MainManager.Maps.GiantLairEntrance, 5) },
            { 16, Spot(MainManager.Maps.MysteryIsland, 0) },
            { 17, Spot(MainManager.Maps.MetalIsland1, 1) },
            { 18, Spot(MainManager.Maps.TermiteMainPlaza, 0) },
            { 19, Spot(MainManager.Maps.WaspKingdom2, 5) },
            { 20, Spot(MainManager.Maps.HideoutWestStorage, 9) },
            { 21, Spot(MainManager.Maps.StreamMountain5, 1) },
            { 22, Spot(MainManager.Maps.ChomperCave1, 1) },
            { 23, Spot(MainManager.Maps.FishingVillage, 2) },
            { 24, Spot(MainManager.Maps.UpperSnekMiddleRoom, 2) },
        };
        private const int OutskirtsArea = 0;

        private static KeyValuePair<MainManager.Maps, int> Spot(MainManager.Maps map, int entity) => new KeyValuePair<MainManager.Maps, int>(map, entity);

        private static readonly List<Kind> buttons = new List<Kind>();
        private static readonly List<SpriteRenderer> icons = new List<SpriteRenderer>();
        private static SpriteRenderer[] builtFor;
        private static bool mapTravel;
        private static Transform confirmBox;
        private static SpriteRenderer leaf;
        private static bool yes;
        private static Kind asking;
        private static int askedArea;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> warpEnabled, Func<bool> mapEnabled)
        {
            log = logger;
            warpOn = warpEnabled;
            mapOn = mapEnabled;
            MethodInfo update = AccessTools.Method(typeof(PauseMenu), "Update");
            MethodInfo updateText = AccessTools.Method(typeof(PauseMenu), "UpdateText");
            if (update == null || updateText == null || optionField == null || maxField == null || spritesField == null
                || boxesField == null || prepareExit == null || buildWindow == null)
            {
                log.LogError("[warp] NOT installed: PauseMenu's Update, UpdateText, BuildWindow or fields weren't found; no travel buttons.");
                return;
            }
            harmony = new Harmony(guid + ".warp." + DateTime.UtcNow.Ticks);
            harmony.Patch(update, prefix: new HarmonyMethod(typeof(WarpButton), nameof(BeforeUpdate)));
            harmony.Patch(updateText, postfix: new HarmonyMethod(typeof(WarpButton), nameof(AfterUpdateText)));
            // Window 0 hands IconAnim four icons and it indexes them by option: hand it one per button.
            MethodInfo iconAnim = AccessTools.Method(typeof(PauseMenu), "IconAnim");
            if (iconAnim != null)
            {
                harmony.Patch(iconAnim, prefix: new HarmonyMethod(typeof(WarpButton), nameof(BeforeIconAnim)));
            }
            log.LogInfo("[warp] installed on PauseMenu.Update and UpdateText");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            // Take this instance's icons and box with it, or a hot reload leaves the old icons behind.
            CloseConfirm();
            ClearIcons();
            builtFor = null;
            mapTravel = false;
        }

        private static void ClearIcons()
        {
            foreach (SpriteRenderer icon in icons)
            {
                if (icon != null)
                {
                    UnityEngine.Object.Destroy(icon.gameObject);
                }
            }
            icons.Clear();
            buttons.Clear();
        }

        private static bool BeforeUpdate(PauseMenu __instance)
        {
            if (__instance.windowid == 6 && mapTravel)
            {
                return MapWindow(__instance);
            }
            mapTravel = false;
            if (__instance.windowid != 0 || MainManager.battle != null || (!warpOn() && !mapOn()))
            {
                CloseConfirm();
                return true;
            }
            SpriteRenderer[] sprites = (SpriteRenderer[])spritesField.GetValue(__instance);
            if (!ReferenceEquals(builtFor, sprites))
            {
                // Window 0 builds over a few frames. Coming back from another page, sprites is briefly that page's shorter array.
                if (sprites == null || sprites.Length < 19 || sprites[16] == null)
                {
                    return true;
                }
                AddButtons(__instance, sprites);
            }
            if (confirmBox != null)
            {
                Confirm(__instance);
                return false;
            }
            int button = (int)optionField.GetValue(__instance) - FirstOption;
            if (button >= 0 && button < buttons.Count && MainManager.instance.inputcooldown <= 0f && MainManager.GetKey(4, hold: false))
            {
                MainManager.PlaySound("Confirm", 10);
                if (buttons[button] == Kind.Warp)
                {
                    OpenConfirm(__instance, Kind.Warp, -1);
                }
                else
                {
                    OpenMap(__instance);
                }
                return false;
            }
            return true;
        }

        private static void BeforeIconAnim(PauseMenu __instance, ref int[] values)
        {
            if (__instance.windowid == 0 && buttons.Count > 0 && values != null && values.Length == 4 && values[0] == 13
                && ReferenceEquals(builtFor, spritesField.GetValue(__instance)))
            {
                var all = new List<int> { 13, 14, 15, 16 };
                for (int i = 0; i < buttons.Count; i++)
                {
                    all.Add(SpriteSlot[i]);
                }
                values = all.ToArray();
            }
        }

        private static void AddButtons(PauseMenu menu, SpriteRenderer[] sprites)
        {
            ClearIcons();
            if (warpOn())
            {
                buttons.Add(Kind.Warp);
            }
            if (mapOn())
            {
                buttons.Add(Kind.Map);
            }
            if (sprites.Length <= SpriteSlot[buttons.Count - 1])
            {
                Array.Resize(ref sprites, SpriteSlot[buttons.Count - 1] + 1);
                spritesField.SetValue(menu, sprites);
            }
            builtFor = sprites;
            // All across, centred, inside the 11-wide box (the game's four sit at -3..3; five fit two apart).
            int total = 4 + buttons.Count;
            float step = total <= 5 ? 2f : 1.8f;
            float x = -step * (total - 1) / 2f;
            for (int n = 0; n < 4; n++)
            {
                sprites[13 + n].transform.localPosition = new Vector3(x + step * n, 3f);
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                // Made as BuildWindow makes the other four, so IconAnim outlines and wiggles it like them.
                SpriteRenderer icon = MainManager.NewUIObject("menuicon" + (FirstOption + i), sprites[16].transform.parent,
                    new Vector3(x + step * (4 + i), 3f), Vector3.one, MainManager.guisprites[IconSprite]).GetComponent<SpriteRenderer>();
                if (buttons.Count == 2 && buttons[i] == Kind.Warp)
                {
                    icon.color = BothWarpTint;
                }
                sprites[SpriteSlot[i]] = icon;
                icons.Add(icon);
            }
            maxField.SetValue(menu, total);
        }

        private static void AfterUpdateText(PauseMenu __instance)
        {
            if (__instance.windowid != 0 || buttons.Count == 0 || !ReferenceEquals(builtFor, spritesField.GetValue(__instance)))
            {
                return;
            }
            int button = (int)optionField.GetValue(__instance) - FirstOption;
            if (button < 0 || button >= buttons.Count)
            {
                return;
            }
            // The game just wrote this option's labels: replace them.
            bool warp = buttons[button] == Kind.Warp;
            Transform labels = ((DialogueAnim[])boxesField.GetValue(__instance))[0].transform;
            MainManager.DestroyText(labels);
            __instance.StartCoroutine(MainManager.SetText("|single|" + (warp ? "Go back to where the game started." : "Travel to an area you've been to."),
                0, 99999f, false, false, new Vector3(-5f, 0.1f), Vector3.zero, Vector2.one, labels, null));
            __instance.StartCoroutine(MainManager.SetText("|center||single|" + (warp ? "Warp" : "Map"), 0, 99999f, false, false,
                new Vector3(0f, 7.5f), Vector3.zero, Vector2.one, labels, null));
        }

        // The game's own map window (as its map shortcut opens it), in travel mode until it closes.
        private static void OpenMap(PauseMenu menu)
        {
            mapTravel = true;
            menu.windowid = 6;
            menu.StartCoroutine((IEnumerator)buildWindow.Invoke(menu, null));
            log.LogInfo("[warp] map travel opened");
        }

        // In travel mode, confirm on a visited area asks to travel there instead of flipping its description's pages.
        private static bool MapWindow(PauseMenu menu)
        {
            if (confirmBox != null)
            {
                Confirm(menu);
                return false;
            }
            int area = (int)optionField.GetValue(menu);
            if (area < 0 || MainManager.instance.inputcooldown > 0f || !MainManager.GetKey(4, hold: false))
            {
                return true;
            }
            if (area >= MainManager.areanames.Length || !MainManager.instance.librarystuff[4, area]
                || (area != OutskirtsArea && !AreaSpots.ContainsKey(area)))
            {
                MainManager.PlayBuzzer();
                return false;
            }
            MainManager.PlaySound("Confirm", 10);
            OpenConfirm(menu, Kind.Map, area);
            return false;
        }

        private static void OpenConfirm(PauseMenu menu, Kind kind, int area)
        {
            yes = false;
            asking = kind;
            askedArea = area;
            confirmBox = MainManager.Create9Box(new Vector3(0f, 0f, 5f), new Vector2(kind == Kind.Map ? 9f : 7f, 3f), 1, 30, Color.white, grow: false);
            confirmBox.parent = menu.transform;
            confirmBox.localPosition = new Vector3(0f, -0.5f, -1f);
            DrawConfirm(menu);
            MainManager.instance.inputcooldown = 10f;
        }

        private static void DrawConfirm(PauseMenu menu)
        {
            // Yes and No at fixed spots so they don't shift when switching; the chosen one coloured, the leaf beside it.
            MainManager.DestroyText(confirmBox);
            string question = asking == Kind.Warp ? "Warp to the start?" : "Travel to " + MainManager.areanames[askedArea] + "?";
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|" + question, 0, 99999f, false, false,
                new Vector3(0f, 0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|" + (yes ? "|color,1|" : "") + "Yes", 0, 99999f, false, false,
                new Vector3(-1.5f, -0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            menu.StartCoroutine(MainManager.SetText("|center||sort,40|" + (yes ? "" : "|color,1|") + "No", 0, 99999f, false, false,
                new Vector3(1.5f, -0.6f), Vector3.zero, Vector2.one * 0.8f, confirmBox, null));
            if (leaf == null)
            {
                // The game's menu cursor, set up as it sets up its own.
                leaf = new GameObject("warpleaf").AddComponent<SpriteRenderer>();
                leaf.sprite = MainManager.cursorsprite[0];
                leaf.sortingOrder = 41;
                leaf.gameObject.layer = 5;
                leaf.transform.parent = confirmBox;
                leaf.transform.localEulerAngles = Vector3.zero;
                leaf.transform.localScale = Vector3.one;
                leaf.gameObject.AddComponent<SpriteBounce>().MessageBounce();
            }
            leaf.transform.localPosition = new Vector3(yes ? -2.4f : 0.9f, -0.45f, 0f);
        }

        private static void Confirm(PauseMenu menu)
        {
            if (MainManager.instance.inputcooldown > 0f)
            {
                return;
            }
            if (MainManager.GetKey(2, hold: false) || MainManager.GetKey(3, hold: false))
            {
                yes = !yes;
                MainManager.PlayScrollSound();
                DrawConfirm(menu);
            }
            else if (MainManager.GetKey(5, hold: false))
            {
                MainManager.PlaySound("Cancel", 10);
                CloseConfirm();
                MainManager.instance.inputcooldown = 10f;
            }
            else if (MainManager.GetKey(4, hold: false))
            {
                if (!yes)
                {
                    MainManager.PlaySound("Cancel", 10);
                    CloseConfirm();
                    MainManager.instance.inputcooldown = 10f;
                    return;
                }
                MainManager.PlaySound("Confirm", 10);
                CloseConfirm();
                Kind kind = asking;
                int area = askedArea;
                mapTravel = false;
                log.LogInfo(kind == Kind.Warp ? $"[warp] warp to start chosen on {MainManager.map?.mapid}"
                    : $"[warp] travel to area {area} ({MainManager.areanames[area]}) chosen on {MainManager.map?.mapid}");
                prepareExit.Invoke(menu, null);
                MainManager.instance.StartCoroutine(TravelWhenUnpaused(kind, area));
            }
        }

        private static void CloseConfirm()
        {
            if (confirmBox != null)
            {
                UnityEngine.Object.Destroy(confirmBox.gameObject);
                confirmBox = null;
                leaf = null;
            }
        }

        private static IEnumerator TravelWhenUnpaused(Kind kind, int area)
        {
            // PrepareExit shrinks the boxes; DestroyPause follows 0.25 s later and clears pause.
            float since = Time.realtimeSinceStartup;
            while ((MainManager.instance.pause || MainManager.pausemenu != null) && Time.realtimeSinceStartup - since < 3f)
            {
                yield return null;
            }
            if (MainManager.player == null || MainManager.instance.inevent || MainManager.instance.message || MainManager.battle != null)
            {
                log.LogWarning("[warp] not now: an event, dialogue or battle started; nothing done");
                yield break;
            }
            MainManager.Maps map = StartMap;
            int entity = MainManager.instance.flags[41] ? 22 : 1;
            if (kind == Kind.Map && area != OutskirtsArea)
            {
                map = AreaSpots[area].Key;
                entity = AreaSpots[area].Value;
            }
            Vector3 target = SavePointSpot(map, entity);
            log.LogInfo($"[warp] to {map} at {target}");
            yield return MainManager.TransferMap((int)map, target);
        }

        // Entity table fields 6-8, then a step toward the camera so the party lands beside the save point, not in it.
        private static Vector3 SavePointSpot(MainManager.Maps map, int entity)
        {
            TextAsset data = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
            string[] lines = data == null ? new string[0] : data.ToString().Split('\n');
            if (entity < lines.Length)
            {
                string[] f = lines[entity].Split('}');
                if (f.Length > 8)
                {
                    try
                    {
                        return new Vector3(Convert.ToSingle(f[6]), Convert.ToSingle(f[7]) + 0.5f, Convert.ToSingle(f[8]) - 2f);
                    }
                    catch (FormatException)
                    {
                    }
                }
            }
            log.LogWarning($"[warp] the save point's spot on {map} wasn't found; landing at the map's origin");
            return Vector3.zero;
        }
    }
}
