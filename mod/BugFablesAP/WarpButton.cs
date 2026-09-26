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
        // Map: the round blue map in the other buttons' style. Warp: the map item's scroll, a "return scroll" (the user).
        private const int MapIconSprite = 34;
        private const int ScrollItem = 41;
        // The scroll has no round backdrop of its own: one is drawn like the other buttons', a dark ring and a bright fill
        // of one vibrant colour (teal blended into the green and blue beside it, the user). Being chosen: orange or pink.
        // The game's own recipe, measured on its round icons: ring at full saturation and brightness 0.51, fill at
        // saturation 0.34 and full brightness, the fill's hue 0.01 lower. Orange at 0.08: the game's sprite 31 (0.05)
        // read salmon at this fill (the user), gold is 0.14.
        private static Color RingColor, FillColor;
        // Lime (the user's pick): the row's biggest gap on the colour wheel, between gold and green.
        private static float hue = LimeHue;
        private const float OrangeHue = 0.08f, PinkHue = 0.9f, LimeHue = 0.28f;

        private static void Colours()
        {
            RingColor = Color.HSVToRGB(hue, 1f, 0.51f);
            FillColor = Color.HSVToRGB(Mathf.Repeat(hue - 0.01f, 1f), 0.34f, 1f);
        }

        // Warp's icon: the game's own round leaf (22), picked by the user over the scroll on a drawn backdrop ("looks more
        // as the game intended"). Dev (console `warpicon`): the key (23), or the scroll again.
        private static int premadeIcon = 22;

        internal static string SetIcon(string name)
        {
            switch (name)
            {
                case "key": premadeIcon = 23; break;
                case "leaf": premadeIcon = 22; break;
                case "scroll": premadeIcon = -1; break;
                default: return "warpicon key|leaf|scroll";
            }
            builtFor = null;
            return "warp icon now " + name + ": reopen the pause menu";
        }

        // Dev only (console `warpcolor`): try a backdrop colour; the pause menu shows it the next time it opens.
        internal static string SetColour(string name)
        {
            switch (name)
            {
                case "orange":
                    hue = OrangeHue;
                    break;
                case "pink":
                    hue = PinkHue;
                    break;
                case "lime":
                    hue = LimeHue;
                    break;
                default:
                    // Or a hue from 0 to 1 (0.28 lime, 0.5 cyan, 0.9 pink).
                    if (!float.TryParse(name, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h)
                        || h < 0f || h > 1f)
                    {
                        return "warpcolor orange|pink|lime|<hue 0-1>";
                    }
                    hue = h;
                    break;
            }
            backdrop = null;
            builtFor = null;
            return "warp backdrop now " + name + ": reopen the pause menu";
        }
        private const float RingShare = 0.14f;
        private static Sprite backdrop;
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
            harmony.Patch(update, prefix: new HarmonyMethod(typeof(WarpButton), nameof(BeforeUpdate)),
                finalizer: new HarmonyMethod(typeof(WarpButton), nameof(UpdateFailed)));
            harmony.Patch(updateText, postfix: new HarmonyMethod(typeof(WarpButton), nameof(AfterUpdateText)));
            // Window 0 hands IconAnim four icons and it indexes them by option: hand it one per button.
            // The game's four buttons are placed at their final spots as they're made, so nothing jumps while the menu opens.
            MethodInfo newObject = AccessTools.Method(typeof(MainManager), nameof(MainManager.NewUIObject),
                new[] { typeof(string), typeof(Transform), typeof(Vector3), typeof(Vector3), typeof(Sprite), typeof(int) });
            if (newObject != null)
            {
                harmony.Patch(newObject, postfix: new HarmonyMethod(typeof(WarpButton), nameof(AfterNewObject)));
            }
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

        // Diagnostic (map travel threw every frame): the first failure's exception and what the map window holds.
        private static bool failureLogged;

        private static Exception UpdateFailed(Exception __exception, PauseMenu __instance)
        {
            if (__exception != null && !failureLogged)
            {
                failureLogged = true;
                var sprites = (SpriteRenderer[])spritesField.GetValue(__instance);
                var boxes = (DialogueAnim[])boxesField.GetValue(__instance);
                int option = (int)optionField.GetValue(__instance);
                var nulls = new List<string>();
                for (int i = 0; sprites != null && i < sprites.Length; i++)
                {
                    if (sprites[i] == null)
                    {
                        nulls.Add(i.ToString());
                    }
                }
                log.LogWarning($"[warp] PauseMenu.Update threw: window {__instance.windowid}, option {option}, mapTravel {mapTravel}, "
                    + $"sprites {(sprites == null ? "null" : sprites.Length.ToString())} (null: {string.Join(" ", nulls.ToArray())}), "
                    + $"boxes {(boxes == null ? "null" : boxes.Length.ToString())}, tempanim {Traverse.Create(__instance).Field("tempanim").GetValue() != null}, "
                    + $"cursor {MainManager.instance.cursor != null}. " + __exception);
            }
            return __exception;
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

        private static int ButtonCount() => (warpOn() ? 1 : 0) + (mapOn() ? 1 : 0);

        // Where button n of total sits: centred, two apart for five; 1.7 for six (1.6 touched, 1.8 cut the first off).
        private static float ButtonX(int n, int total)
        {
            float step = total <= 5 ? 2f : 1.7f;
            return -step * (total - 1) / 2f + step * n;
        }

        private static void AfterNewObject(string objname, GameObject __result)
        {
            if (__result == null || !objname.StartsWith("menuicon") || MainManager.pausemenu == null
                || MainManager.pausemenu.windowid != 0 || MainManager.battle != null
                || !int.TryParse(objname.Substring("menuicon".Length), out int n) || n > 3)
            {
                return;
            }
            int extra = ButtonCount();
            if (extra > 0)
            {
                __result.transform.localPosition = new Vector3(ButtonX(n, 4 + extra), __result.transform.localPosition.y);
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
            // All across, centred, inside the 11-wide box (the game's four sit at -3..3); already placed as they were made.
            int total = 4 + buttons.Count;
            for (int n = 0; n < 4; n++)
            {
                sprites[13 + n].transform.localPosition = new Vector3(ButtonX(n, total), 3f);
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                // Made as BuildWindow makes the other four, so IconAnim outlines and wiggles it like them.
                bool map = buttons[i] == Kind.Map;
                bool premade = !map && premadeIcon >= 0;
                Sprite look = map ? MainManager.guisprites[MapIconSprite] : premade ? MainManager.guisprites[premadeIcon] : Backdrop();
                // The button's own sprite, so the game's outline and wiggle apply; the scroll rides on it.
                SpriteRenderer icon = MainManager.NewUIObject("menuicon" + (FirstOption + i), sprites[16].transform.parent,
                    new Vector3(ButtonX(4 + i, total), 3f), Vector3.one, look).GetComponent<SpriteRenderer>();
                if (!map && !premade)
                {
                    SpriteRenderer scroll = MainManager.NewUIObject("scroll", icon.transform, Vector3.zero, Vector3.one,
                        MainManager.itemsprites[0, ScrollItem]).GetComponent<SpriteRenderer>();
                    scroll.sortingOrder = icon.sortingOrder + 1;
                }
                sprites[SpriteSlot[i]] = icon;
                icons.Add(icon);
            }
            maxField.SetValue(menu, total);
        }

        // Drawn once: a circle the size of the blue map icon, a flat ring round a flat fill, edges smoothed by a pixel.
        private static Sprite Backdrop()
        {
            if (backdrop != null)
            {
                return backdrop;
            }
            Colours();
            Sprite model = MainManager.guisprites[MapIconSprite];
            int size = Mathf.RoundToInt(Mathf.Max(model.rect.width, model.rect.height));
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size / 2f - 1f, inner = radius * (1f - RingShare), centre = (size - 1) / 2f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
                    Color c = d <= inner ? FillColor : RingColor;
                    // Smooth the ring's inner edge and the circle's outer edge over one pixel.
                    if (d > inner - 0.5f && d < inner + 0.5f)
                    {
                        c = Color.Lerp(FillColor, RingColor, d - (inner - 0.5f));
                    }
                    c.a = Mathf.Clamp01(radius + 0.5f - d);
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            backdrop = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), model.pixelsPerUnit);
            return backdrop;
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
            // The map reads option as the chosen area and draws toward its marker every frame: the pause menu's option
            // (this button's) pointed at a marker that didn't exist. -1 is the map's own "none yet".
            optionField.SetValue(menu, -1);
            // The area you stand in is visited. A new file starts in area 0 and the game marks an area only on a change
            // of area (UpdateArea), so the start was never marked: the same one field UpdateArea writes.
            int here = MainManager.instance.areaid;
            if (here >= 0 && here < MainManager.areanames.Length && !MainManager.instance.librarystuff[4, here])
            {
                MainManager.instance.librarystuff[4, here] = true;
                log.LogInfo($"[warp] area {here} ({MainManager.areanames[here]}) marked visited: you're in it, and the game never marked it");
            }
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
            if (kind == Kind.Map)
            {
                // The map is a 3D object at depth 5 on the GUI camera, in front of the pause menu: the box goes in front of it.
                confirmBox.parent = MainManager.GUICamera.transform;
                confirmBox.localPosition = new Vector3(0f, -0.5f, 2f);
                confirmBox.localEulerAngles = Vector3.zero;
            }
            else
            {
                confirmBox.parent = menu.transform;
                confirmBox.localPosition = new Vector3(0f, -0.5f, -1f);
            }
            log.LogInfo(kind == Kind.Warp ? "[warp] asking: warp to the start?" : $"[warp] asking: travel to area {area} ({MainManager.areanames[area]})?");
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
            // Warp to Start goes to the seed's start when it has one (Starting Location); map travel keeps its spots.
            KeyValuePair<string, int>? seeded = QualityOfLife.SeedStart?.Invoke();
            if (kind == Kind.Warp && seeded.HasValue && Enum.IsDefined(typeof(MainManager.Maps), seeded.Value.Key))
            {
                map = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), seeded.Value.Key);
                entity = seeded.Value.Value;
            }
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
        internal static Vector3 SavePointSpot(MainManager.Maps map, int entity)
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
