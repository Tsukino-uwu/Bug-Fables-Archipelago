using System;
using System.Collections.Generic;
using System.IO;
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
        private const string Header = "map\twidth\theight\torigin_x\torigin_y\tx_step_x\tx_step_y\tz_step_x\tz_step_y\n";
        // Far from anything else in the scene, so a camera sees the copy alone.
        private static readonly Vector3 WorldAway = new Vector3(0f, -5000f, 0f);
        private static readonly Vector3 RoomAway = new Vector3(0f, -10000f, 0f);

        // roomshot all: one room a frame, so the game never stalls for the whole list.
        private static readonly Queue<string> pending = new Queue<string>();
        private static float allPitch;
        private static StringBuilder allTable;
        private static int allShot;

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
                Shot? shot = Shoot(renderers, Quaternion.identity, 2048f, 0f,
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
        internal static string Room(ManualLogSource log, string name, float pitch)
        {
            string message = Room(log, name, pitch, RoomPixelsPerUnit,
                Path.Combine(Paths.BepInExRootPath, "bugfablesap-room-" + name + ".png"), out Shot? shot);
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
                out Shot? shot);
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
            out Shot? shot)
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
                var renderers = new List<Renderer>();
                foreach (Renderer r in (main != null ? main : room.transform).GetComponentsInChildren<Renderer>())
                {
                    // The game parks unused objects high above a map, out of reach.
                    if (r.bounds.center.y - RoomAway.y < 90f && !(r is ParticleSystemRenderer))
                    {
                        renderers.Add(r);
                    }
                }
                shot = Shoot(renderers, Quaternion.Euler(pitch, 0f, 0f), 0f, pixelsPerUnit, png,
                    p => new Vector3(p.x, 0f, p.y) + RoomAway);
                if (shot == null)
                {
                    return $"{name}: nothing to render";
                }
                string message = $"{name}: {Path.GetFileName(png)} ({shot.Value.Width}x{shot.Value.Height}), "
                    + $"{renderers.Count} renderers" + (main != null ? " under " + main.name : "")
                    + $", ambient {RenderSettings.ambientLight}";
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
        private static Shot? Shoot(List<Renderer> renderers, Quaternion rotation, float width, float pixelsPerUnit,
            string png, Func<Vector2, Vector3> point)
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
                cam.cullingMask = layers;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.depthTextureMode = DepthTextureMode.Depth;
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
