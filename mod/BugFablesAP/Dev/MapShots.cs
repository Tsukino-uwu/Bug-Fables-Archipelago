using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BugFablesAP
{
    // Dev-only (the console's worldmap and roomshot): pictures of the pause menu's world map and of the rooms, each
    // with where the game's coordinates land in it, to the BepInEx folder (game art: never the repo).
    internal static class MapShots
    {
        private const int MaxSide = 4096;
        private const float RoomPixelsPerUnit = 24f;
        private const float AllPixelsPerUnit = 12f;
        // The game parks unused objects this high above a map, out of reach.
        private const float Parked = 90f;
        // From straight above, the view starts this high over the room's highest entity: ceilings and roofs over it,
        // which the game's own camera never shows, are left out.
        private const float CeilingMargin = 5f;
        // Over the ceiling by this much, the view is cut even through what reaches below it.
        private const float ClipAbove = 10f;
        private const string OutlineShader = "Outlined/Silhouette Only";
        // Unlit: the pause menu's world map draws with it, so the game has it.
        private const string FlatShader = "Unlit/Transparent Cutout";
        private const string Header = "map\twidth\theight\torigin_x\torigin_y\tx_step_x\tx_step_y\tz_step_x\tz_step_y\n";
        // Far from anything else in the scene, so a camera sees the copy alone.
        private static readonly Vector3 WorldAway = new Vector3(0f, -5000f, 0f);
        private static readonly Vector3 RoomAway = new Vector3(0f, -10000f, 0f);

        // roomshot all: one room a frame, so the game never stalls for the whole list.
        private static readonly Queue<string> pending = new Queue<string>();
        private static float allPitch;
        private static StringBuilder allTable;
        private static int allShot;
        // roomshot's switches after the map: list (log the objects drawn), rawwater (the game's own water shader).
        internal static HashSet<string> Tries = new HashSet<string>();

        private struct Shot
        {
            internal int Width, Height;
            internal Vector2 Origin, XStep, ZStep;

            internal string Row(string name)
            {
                return string.Join("\t", new[] { name, Width.ToString(), Height.ToString(), F(Origin.x), F(Origin.y),
                    F(XStep.x), F(XStep.y), F(ZStep.x), F(ZStep.y) }) + "\n";
            }

            private static string F(float v) => v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string RoomsFolder => Path.Combine(Paths.BepInExRootPath, "bugfablesap-rooms");

        // A seed's scenery_hidden and scenery_present (map, path in the map), from a slot_data file (roomstates).
        private static HashSet<(string, string)> sceneryHidden = new HashSet<(string, string)>();
        private static HashSet<(string, string)> sceneryPresent = new HashSet<(string, string)>();

        // roomstates <file>: the scenery lists of a slot_data file (dev-scripts/live-slot-data.py's), for the rooms'
        // pictures to show them as a seed does.
        internal static string LoadStates(string file)
        {
            if (!File.Exists(file))
            {
                return "roomstates: no file " + file;
            }
            Newtonsoft.Json.Linq.JObject data = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(file));
            HashSet<(string, string)> Read(string key) => new HashSet<(string, string)>(
                (data[key] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray())
                .Select(e => ((string)e["map"], (string)e["entity"])));
            sceneryHidden = Read("scenery_hidden");
            sceneryPresent = Read("scenery_present");
            return $"roomstates: {sceneryHidden.Count} scenery hidden, {sceneryPresent.Count} shown, from {file}";
        }

        internal static string World(ManualLogSource log)
        {
            Object prefab = Resources.Load("Prefabs/Objects/map");
            if (prefab == null)
            {
                return "worldmap: no Prefabs/Objects/map";
            }
            // As PauseMenu sets it under the GUI camera, whose view runs along +z.
            var map = (GameObject)Object.Instantiate(prefab, WorldAway + new Vector3(0f, 0.5f, 5f),
                Quaternion.Euler(90f, 180f, 0f));
            try
            {
                var info = new StringBuilder("[dev] worldmap:");
                Animator anim = map.GetComponent<Animator>();
                if (anim != null)
                {
                    // Played through as the pause menu plays it, in steps, so the paper's bones end opened.
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    anim.Play("Open", 0, 0f);
                    for (int i = 0; i < 100; i++)
                    {
                        anim.Update(0.05f);
                    }
                }
                var renderers = new List<Renderer>(map.GetComponentsInChildren<Renderer>(true));
                foreach (Renderer r in renderers)
                {
                    if (r is SkinnedMeshRenderer skinned)
                    {
                        skinned.updateWhenOffscreen = true;
                    }
                    foreach (Material m in r.sharedMaterials)
                    {
                        Texture t = m != null ? m.mainTexture : null;
                        info.Append("\n  ").Append(PathOf(r.transform, map.transform)).Append(" <")
                            .Append(r.GetType().Name).Append("> shader ").Append(m != null ? m.shader.name : "none")
                            .Append(" texture ").Append(t != null ? $"{t.name} {t.width}x{t.height}" : "none");
                        if (t is Texture2D t2)
                        {
                            SpriteDump.Save(t2, Path.Combine(Paths.BepInExRootPath,
                                "bugfablesap-worldmap-texture-" + t2.name + ".png"));
                        }
                    }
                }
                // The pause map's pips stand at (x, 2, z) in the map's own frame.
                Shot? shot = Shoot(renderers, Quaternion.identity, 2048f, 0f, null,
                    Path.Combine(Paths.BepInExRootPath, "bugfablesap-worldmap.png"),
                    p => map.transform.TransformPoint(new Vector3(p.x, 2f, p.y)));
                if (shot == null)
                {
                    return "worldmap: nothing to render";
                }
                File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "bugfablesap-worldmap.tsv"),
                    Header + shot.Value.Row("World"));
                log.LogInfo(info.ToString());
                return $"worldmap: bugfablesap-worldmap.png ({shot.Value.Width}x{shot.Value.Height}) and .tsv written";
            }
            finally
            {
                Object.Destroy(map);
            }
        }

        // roomshot <map> [pitch]: one room to bugfablesap-room-<map>.png and .tsv.
        internal static string Room(ManualLogSource log, string name, float pitch, bool list)
        {
            string message = Room(log, name, pitch, RoomPixelsPerUnit,
                Path.Combine(Paths.BepInExRootPath, "bugfablesap-room-" + name + ".png"), list, out Shot? shot);
            if (shot != null)
            {
                File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "bugfablesap-room-" + name + ".tsv"),
                    Header + shot.Value.Row(name));
            }
            return "roomshot " + message;
        }

        // roomshot all [pitch]: every map with a prefab, one a frame (Tick), to bugfablesap-rooms/<map>.png and one
        // rooms.tsv.
        internal static string All(float pitch)
        {
            if (pending.Count > 0)
            {
                return $"roomshot all: {pending.Count} maps still to go";
            }
            Directory.CreateDirectory(RoomsFolder);
            allTable = new StringBuilder(Header);
            allPitch = pitch;
            allShot = 0;
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                pending.Enqueue(map.ToString());
            }
            return $"roomshot all: {pending.Count} maps queued, one a frame";
        }

        internal static void Tick(ManualLogSource log)
        {
            if (pending.Count == 0)
            {
                return;
            }
            string name = pending.Dequeue();
            string message = Room(log, name, allPitch, AllPixelsPerUnit, Path.Combine(RoomsFolder, name + ".png"),
                false, out Shot? shot);
            if (shot != null)
            {
                allTable.Append(shot.Value.Row(name));
                allShot++;
            }
            else
            {
                log.LogInfo("[dev] roomshot all: " + message);
            }
            if (pending.Count == 0)
            {
                File.WriteAllText(Path.Combine(RoomsFolder, "rooms.tsv"), allTable.ToString());
                Resources.UnloadUnusedAssets();
                log.LogInfo($"[dev] roomshot all: {allShot} rooms -> {RoomsFolder}");
            }
        }

        // A room as the game builds it from its prefab, its insides hidden as from outside, none of its scripts or
        // sounds run: seen from pitch degrees (90, straight down; less, more from the front).
        private static string Room(ManualLogSource log, string name, float pitch, float pixelsPerUnit, string png,
            bool list, out Shot? shot)
        {
            shot = null;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Maps/" + name);
            if (prefab == null)
            {
                return $"{name}: no prefab";
            }
            MapControl control = prefab.GetComponent<MapControl>();
            bool wasActive = prefab.activeSelf;
            GameObject room;
            prefab.SetActive(false);
            try
            {
                room = Object.Instantiate(prefab, prefab.transform.position + RoomAway, prefab.transform.rotation);
            }
            finally
            {
                prefab.SetActive(wasActive);
            }
            Color ambient = RenderSettings.ambientLight;
            bool fog = RenderSettings.fog;
            try
            {
                // Flag-switched scenery as a seed shows it on a new file: the game's own check with no flags set
                // (MainManager.CheckIfCanExist: a requirement unmet takes it away, or moves one kept to its other
                // spot), then the seed's own lists (roomstates) on top, as the mod's KeptOpen applies them.
                foreach (ConditionChecker c in room.GetComponentsInChildren<ConditionChecker>(true))
                {
                    string path = PathOf(c.transform, room.transform, false);
                    if (sceneryHidden.Contains((name, path)))
                    {
                        c.gameObject.SetActive(false);
                    }
                    else if (!sceneryPresent.Contains((name, path)) && c.requires != null && c.requires.Length > 0
                        && c.requires[0] > -1)
                    {
                        if (!c.dontdelete)
                        {
                            c.gameObject.SetActive(false);
                        }
                        else if (c.activepos.magnitude > 0.1f)
                        {
                            if (c.worldpos)
                            {
                                c.transform.position = c.activepos + RoomAway;
                            }
                            else
                            {
                                c.transform.localPosition = c.activepos;
                            }
                        }
                    }
                }
                foreach (MonoBehaviour b in room.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    Object.DestroyImmediate(b);
                }
                foreach (AudioSource a in room.GetComponentsInChildren<AudioSource>(true))
                {
                    Object.DestroyImmediate(a);
                }
                int left = room.GetComponentsInChildren<MonoBehaviour>(true).Length;
                if (left > 0)
                {
                    return $"{name}: {left} scripts kept, not shown";
                }
                if (control != null && control.hideinsides && control.insides != null)
                {
                    foreach (GameObject inside in control.insides)
                    {
                        Transform copy = inside != null
                            ? room.transform.Find(PathOf(inside.transform, prefab.transform, false)) : null;
                        if (copy != null)
                        {
                            copy.gameObject.SetActive(false);
                        }
                    }
                }
                room.SetActive(true);
                if (control != null)
                {
                    RenderSettings.ambientLight = control.globallight;
                }
                RenderSettings.fog = false;
                Transform main = control != null && control.mainmesh != null
                    ? room.transform.Find(PathOf(control.mainmesh, prefab.transform, false)) : null;
                float? ceiling = null;
                if (Mathf.Approximately(pitch, 90f) && Enum.IsDefined(typeof(MainManager.Maps), name))
                {
                    float top = float.MinValue;
                    foreach (Vector3 p in EntityDump.Positions((int)Enum.Parse(typeof(MainManager.Maps), name)))
                    {
                        if (p.y < Parked)
                        {
                            top = Mathf.Max(top, p.y);
                        }
                    }
                    if (top > float.MinValue)
                    {
                        ceiling = top + CeilingMargin;
                    }
                }
                // What lies wholly over the ceiling (a roof, a cave's ceiling) is switched off in this copy, the rest
                // kept whole: a slice through a log or a rock would show its dark inside.
                var renderers = new List<Renderer>();
                var listing = new StringBuilder();
                Shader flat = Shader.Find(FlatShader);
                foreach (Renderer r in (main != null ? main : room.transform).GetComponentsInChildren<Renderer>())
                {
                    Bounds b = r.bounds;
                    if (b.center.y - RoomAway.y >= Parked || r is ParticleSystemRenderer
                        || (ceiling != null && b.min.y - RoomAway.y > ceiling.Value))
                    {
                        r.enabled = false;
                        continue;
                    }
                    // The black outline shell the game draws round an object from the side shows as a black blob
                    // from above: left out of this copy, and an object that is only a shell with it.
                    Material[] materials = r.sharedMaterials;
                    Material[] own = Array.FindAll(materials, m => m == null || m.shader.name != OutlineShader);
                    if (own.Length == 0)
                    {
                        r.enabled = false;
                        continue;
                    }
                    if (own.Length < materials.Length)
                    {
                        r.sharedMaterials = own;
                    }
                    // The game's water shader draws white from above: its texture's own colour instead, unlit (the
                    // world map's own shader), so the light doesn't wash it out.
                    if (!Tries.Contains("rawwater") && flat != null)
                    {
                        for (int i = 0; i < own.Length; i++)
                        {
                            if (own[i] != null && own[i].shader.name.Contains("Water")
                                && own[i].mainTexture is Texture2D waterTexture)
                            {
                                own[i] = new Material(flat) { mainTexture = WaterFill(waterTexture) };
                                r.sharedMaterials = own;
                            }
                        }
                    }
                    renderers.Add(r);
                    if (list)
                    {
                        Material m = r.sharedMaterial;
                        listing.Append("\n  ").Append(PathOf(r.transform, room.transform)).Append(" <")
                            .Append(r.GetType().Name).Append("> ").Append(m != null ? m.name : "none").Append(" / ")
                            .Append(m != null ? m.shader.name : "none")
                            .Append(m != null && m.HasProperty("_Color") ? " colour " + m.color : "")
                            .Append($" x {b.center.x - RoomAway.x:F1} z {b.center.z - RoomAway.z:F1}, ")
                            .Append($"y {b.min.y - RoomAway.y:F1} to {b.max.y - RoomAway.y:F1}");
                        foreach (Material water in r.sharedMaterials)
                        {
                            if (water == null || !water.shader.name.Contains("Water"))
                            {
                                continue;
                            }
                            // Which of the usual colour and texture names the water's material has, and their values.
                            foreach (string colour in new[] { "_Color", "_TintColor", "_WaterColor", "_DeepColor",
                                "_ShallowColor", "_FoamColor", "_EdgeColor", "_OutlineColor", "_BaseColor" })
                            {
                                if (water.HasProperty(colour))
                                {
                                    listing.Append($"\n    {colour} {water.GetColor(colour)}");
                                }
                            }
                            foreach (string texture in new[] { "_MainTex", "_NoiseTex", "_FoamTex", "_BumpMap",
                                "_DepthTex", "_Ramp" })
                            {
                                Texture t = water.HasProperty(texture) ? water.GetTexture(texture) : null;
                                if (t != null)
                                {
                                    listing.Append($"\n    {texture} {t.name} {t.width}x{t.height}");
                                    if (t is Texture2D t2)
                                    {
                                        SpriteDump.Save(t2, Path.Combine(Paths.BepInExRootPath,
                                            $"bugfablesap-water-{water.name}{texture}.png"));
                                    }
                                }
                            }
                            listing.Append($"\n    render queue {water.renderQueue}, keywords "
                                + string.Join(" ", water.shaderKeywords));
                        }
                    }
                }
                // A room whose floor, walls and ceiling are one model still needs a slice, higher up.
                shot = Shoot(renderers, Quaternion.Euler(pitch, 0f, 0f), 0f, pixelsPerUnit,
                    ceiling + ClipAbove + RoomAway.y, png, p => new Vector3(p.x, 0f, p.y) + RoomAway);
                if (list)
                {
                    log.LogInfo($"[dev] roomshot {name}, renderers drawn:" + listing);
                }
                if (shot == null)
                {
                    return $"{name}: nothing to render";
                }
                string message = $"{name}: {Path.GetFileName(png)} ({shot.Value.Width}x{shot.Value.Height}), "
                    + $"{renderers.Count} renderers" + (main != null ? " under " + main.name : "")
                    + $", ambient {RenderSettings.ambientLight}"
                    + (ceiling != null ? $", seen from {ceiling.Value:F1} up" : "");
                log.LogInfo("[dev] roomshot " + message);
                return message;
            }
            finally
            {
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;
                Object.Destroy(room);
            }
        }

        // Renders the renderers' bounds from an orthographic camera turned by rotation, width pixels wide (or at
        // pixelsPerUnit), to png; the shot says where game points (x, z) of 0 and 1 land, from the picture's top left.
        // A ceiling (straight down only) puts the camera at that height, so what stands above it is behind the camera.
        private static Shot? Shoot(List<Renderer> renderers, Quaternion rotation, float width, float pixelsPerUnit,
            float? ceiling, string png, Func<Vector2, Vector3> point)
        {
            if (renderers.Count == 0)
            {
                return null;
            }
            Bounds bounds = renderers[0].bounds;
            int layers = 0;
            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
                layers |= 1 << r.gameObject.layer;
            }
            var holder = new GameObject("mapshot camera");
            RenderTexture rt = null;
            try
            {
                Camera cam = holder.AddComponent<Camera>();
                cam.enabled = false;
                float reach = bounds.extents.magnitude + 10f;
                cam.transform.rotation = rotation;
                cam.transform.position = bounds.center - cam.transform.forward * reach;
                Vector2 low = new Vector2(float.MaxValue, float.MaxValue), high = -low;
                foreach (Vector3 corner in Corners(bounds))
                {
                    Vector3 local = cam.transform.InverseTransformPoint(corner);
                    low = Vector2.Min(low, local);
                    high = Vector2.Max(high, local);
                }
                Vector2 size = high - low;
                cam.transform.position += cam.transform.rotation * new Vector3((low.x + high.x) / 2f,
                    (low.y + high.y) / 2f, 0f);
                float scale = width > 0f ? width / size.x : pixelsPerUnit;
                scale = Mathf.Min(scale, MaxSide / Mathf.Max(size.x, size.y));
                int w = Mathf.Max(1, Mathf.RoundToInt(size.x * scale));
                int h = Mathf.Max(1, Mathf.RoundToInt(size.y * scale));
                cam.orthographic = true;
                cam.orthographicSize = size.y / 2f;
                cam.aspect = size.x / size.y;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 2f * reach;
                if (ceiling != null)
                {
                    Vector3 at = cam.transform.position;
                    cam.transform.position = new Vector3(at.x, ceiling.Value, at.z);
                    cam.farClipPlane = ceiling.Value - bounds.min.y + 1f;
                }
                cam.cullingMask = layers;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default,
                    8);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                var picture = new Texture2D(w, h, TextureFormat.ARGB32, false);
                picture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                picture.Apply();
                RenderTexture.active = previous;
                cam.targetTexture = null;
                File.WriteAllBytes(png, picture.EncodeToPNG());
                Object.Destroy(picture);

                // Through the viewport: once the target is released, screen points are the game window's.
                Vector2 Pixel(Vector2 p)
                {
                    Vector3 viewport = cam.WorldToViewportPoint(point(p));
                    return new Vector2(viewport.x * w, (1f - viewport.y) * h);
                }
                Vector2 origin = Pixel(Vector2.zero);
                return new Shot
                {
                    Width = w, Height = h, Origin = origin, XStep = Pixel(Vector2.right) - origin,
                    ZStep = Pixel(Vector2.up) - origin,
                };
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.ReleaseTemporary(rt);
                }
                Object.Destroy(holder);
            }
        }

        private static readonly Dictionary<Texture2D, Texture2D> waterFills = new Dictionary<Texture2D, Texture2D>();

        // One pixel of a water texture's own colour: the mean of its coloured pixels, its white crests left out.
        private static Texture2D WaterFill(Texture2D texture)
        {
            if (waterFills.TryGetValue(texture, out Texture2D fill) && fill != null)
            {
                return fill;
            }
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Vector4 sum = Vector4.zero;
            int count = 0;
            foreach (Color32 c in copy.GetPixels32())
            {
                if (c.a > 128 && Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b) > 40)
                {
                    sum += new Vector4(c.r, c.g, c.b, 255f);
                    count++;
                }
            }
            Object.Destroy(copy);
            Color32 mean = count == 0 ? new Color32(255, 255, 255, 255)
                : new Color32((byte)(sum.x / count), (byte)(sum.y / count), (byte)(sum.z / count), 255);
            fill = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            fill.SetPixels32(new[] { mean });
            fill.Apply();
            waterFills[texture] = fill;
            return fill;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
            }
        }

        // A transform's path under root, with root's own name or without it (as Transform.Find takes it).
        private static string PathOf(Transform t, Transform root, bool withRoot = true)
        {
            if (t == root)
            {
                return withRoot ? t.name : "";
            }
            string parent = PathOf(t.parent, root, withRoot);
            return parent.Length == 0 ? t.name : parent + "/" + t.name;
        }
    }
}
