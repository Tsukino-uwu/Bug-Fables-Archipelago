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
        // From the front, the view is cut this far before the entity nearest the camera.
        private const float FrontMargin = 4f;
        // Rooms seen straight down whatever the pitch, each picked by look (2026-10-13): a round town, checks behind
        // a mountain, a room whose front wall hides it, two tall narrow rooms.
        private static readonly HashSet<string> FromAbove = new HashSet<string>
        {
            "BeehiveMainArea", "MysteryIsland", "PowerPlant", "HoneyFactoryCore", "BeehiveScannerRoom",
        };
        // Rooms that keep the game's outlines from the front, picked by look: the Sand Castle's flat bright yellow
        // reads only with its line art. Elsewhere they turn some objects black.
        private const string Outlined = "SandCastle";
        // Rooms drawn without their water: a crystal switch raises and lowers it, and the room shows best without.
        private static readonly HashSet<string> Drained = new HashSet<string> { "StreamMountain2", "StreamMountain4" };
        // Rooms cut above a height from the front, each picked by look (2026-10-13): their dark lid hid the room's back
        // and left the top of its walls floating over it.
        private static readonly Dictionary<string, float> Capped = new Dictionary<string, float>
        {
            ["CaveOfTrials"] = 8f, ["UpperSnekEntrance"] = 12f, ["BroodmotherLair"] = 8f,
            ["WaspKingdomThrone"] = 8f, ["FactoryProcessingPuzzle1"] = 6f,
        };
        // Rooms seen whole from the front, each picked by look: the view's cut took the front of their ground.
        private static readonly HashSet<string> Uncut = new HashSet<string>
        {
            "SnakemouthTreasureRoom", "DesertBadlands", "DesertJumpPuzzle",
        };
        // Objects left out, each picked by look: the cloud floor under Barren Lands Cloud's pillars.
        private static readonly HashSet<(string, string)> Hidden = new HashSet<(string, string)>
        {
            ("BarrenLandsCloud", "Base/Plane"),
        };
        // Flags shown as set in a room, each picked by look: the Sand Castle risen (flag 280: Castle (1) needs it, the
        // small Castle and the Tablet go).
        private static readonly Dictionary<string, int[]> FlagsSet = new Dictionary<string, int[]>
        {
            ["DesertSandCastle"] = new[] { 280 },
        };
        private const string OutlineShader = "Outlined/Silhouette Only";
        private const string DefaultMaterial = "Default-Material";
        // A shaft of light: a tall glowing cylinder reaching far under the floor, dark ends below a room seen tilted.
        private const string LightBeam = "LightBeam";
        // A black shell's colour channels, at most; a black area this many pixels from its middle to its edge, at
        // least, is too thick for an outline.
        private const byte BlackShell = 6;
        private const int ThickBlack = 4;
        // Unlit: the pause menu's world map draws with it, so the game has it.
        private const string FlatShader = "Unlit/Transparent Cutout";
        private const string Header =
            "map\twidth\theight\torigin_x\torigin_y\tx_step_x\tx_step_y\tz_step_x\tz_step_y\ty_step_x\ty_step_y"
            + "\tcam_neg_x\tcam_neg_z\tcam_pos_x\tcam_pos_z\tcenter_x\tcenter_z\ttether\n";
        // Far from anything else in the scene, so a camera sees the copy alone.
        private static readonly Vector3 WorldAway = new Vector3(0f, -5000f, 0f);
        private static readonly Vector3 RoomAway = new Vector3(0f, -10000f, 0f);

        // roomshot all: one room a frame, so the game never stalls for the whole list.
        private static readonly Queue<string> pending = new Queue<string>();
        private static float allPitch;
        private static StringBuilder allTable;
        private static int allShot;
        // roomshot's switches after the map, to find what a picture needs: list (log what is drawn and skipped),
        // rawwater (the game's own water shader), hide=<path> (leave that object out; + for a space), cap=<height> (cut
        // above it), nocut (no cut before the nearest entity), noclear (black kept).
        internal static HashSet<string> Tries = new HashSet<string>();

        private struct Shot
        {
            internal int Width, Height;
            internal Vector2 Origin, XStep, ZStep, YStep;
            internal float[] Extra;

            internal string Row(string name)
            {
                return string.Join("\t", new[] { name, Width.ToString(), Height.ToString(), F(Origin.x), F(Origin.y),
                    F(XStep.x), F(XStep.y), F(ZStep.x), F(ZStep.y), F(YStep.x), F(YStep.y) }
                    .Concat((Extra ?? new float[7]).Select(F)).ToArray()) + "\n";
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
                Shot? shot = Shoot(renderers, 0, Quaternion.identity, 2048f, 0f, new List<Vector3>(), 0f,
                    Path.Combine(Paths.BepInExRootPath, "bugfablesap-worldmap.png"),
                    p => map.transform.TransformPoint(p + new Vector3(0f, 2f, 0f)));
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
            return $"roomshot all: {pending.Count} maps queued, one a frame, with {sceneryHidden.Count} scenery hidden"
                + $" and {sceneryPresent.Count} shown (roomstates)";
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
        // sounds run: seen from pitch degrees (90, straight down; less, more from the front; a room with no sky always
        // straight down).
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
            UnityEngine.Rendering.AmbientMode ambientMode = RenderSettings.ambientMode;
            bool fog = RenderSettings.fog;
            // Lit by its own lights only: the title screen's sun doubled every room's and washed the dim ones out.
            List<Light> others = Object.FindObjectsOfType<Light>().Where(l => l.enabled).ToList();
            var made = new List<Object>();
            try
            {
                others.ForEach(l => l.enabled = false);
                // Flag-switched scenery as a seed shows it on a new file: the game's own check with no flags set but
                // the room's own in FlagsSet (MainManager.CheckIfCanExist: a requirement unmet, or every limit flag
                // set, takes it away, or moves one kept to its other spot), then the seed's own lists (roomstates) on
                // top, as the mod's KeptOpen applies them.
                int[] set = FlagsSet.TryGetValue(name, out int[] flags) ? flags : new int[0];
                foreach (ConditionChecker c in room.GetComponentsInChildren<ConditionChecker>(true))
                {
                    string path = PathOf(c.transform, room.transform, false);
                    if (list)
                    {
                        log.LogInfo($"[dev] roomshot {name}: {path} requires "
                            + string.Join(",", (c.requires ?? new int[0]).Select(f => f.ToString()).ToArray())
                            + " limit " + string.Join(",", (c.limit ?? new int[0]).Select(f => f.ToString()).ToArray())
                            + (c.dontdelete ? " (moves)" : ""));
                    }
                    if (sceneryHidden.Contains((name, path)))
                    {
                        c.gameObject.SetActive(false);
                    }
                    else if (!sceneryPresent.Contains((name, path))
                        && ((c.requires != null && c.requires.Length > 0 && c.requires[0] > -1
                                && !c.requires.All(set.Contains))
                            || (c.limit != null && c.limit.Length > 0 && c.limit[0] > -1 && c.limit.All(set.Contains))))
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
                if (list)
                {
                    log.LogInfo($"[dev] roomshot {name}: scripts " + string.Join(", ", room
                        .GetComponentsInChildren<MonoBehaviour>(true).GroupBy(b => b.GetType().Name)
                        .Select(g => g.Key + " " + g.Count()).ToArray()));
                }
                // The colours and draw order the room's own scripts give its objects on Start (MaterialColor,
                // RenderQueue), before the scripts go.
                foreach (MaterialColor c in room.GetComponentsInChildren<MaterialColor>(true))
                {
                    Renderer r = c.GetComponent<Renderer>();
                    if (r != null && c.materialid < r.sharedMaterials.Length)
                    {
                        Material[] own = r.materials;
                        made.AddRange(own);
                        own[c.materialid].color = c.color;
                    }
                }
                foreach (RenderQueue q in room.GetComponentsInChildren<RenderQueue>(true))
                {
                    Renderer r = q.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Material[] own = r.materials;
                        made.AddRange(own);
                        foreach (int i in q.materials.Length > 0 ? q.materials : Enumerable.Range(0, own.Length))
                        {
                            own[i].renderQueue = q.queue;
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
                    if (list)
                    {
                        log.LogInfo($"[dev] roomshot {name}: ambient mode {RenderSettings.ambientMode}, sky "
                            + $"{RenderSettings.ambientSkyColor}, equator {RenderSettings.ambientEquatorColor}, ground "
                            + $"{RenderSettings.ambientGroundColor}");
                    }
                    RenderSettings.ambientLight = control.globallight;
                }
                RenderSettings.fog = false;
                Transform main = control != null && control.mainmesh != null
                    ? room.transform.Find(PathOf(control.mainmesh, prefab.transform, false)) : null;
                float? ceiling = null;
                // A water droplet starts at a cave's ceiling, not where the party stands.
                List<Vector3> spots = Enum.IsDefined(typeof(MainManager.Maps), name)
                    ? EntityDump.Positions((int)Enum.Parse(typeof(MainManager.Maps), name), "Dropplet")
                        .Where(p => p.y < Parked).ToList()
                    : new List<Vector3>();
                if (spots.Count > 0)
                {
                    ceiling = spots.Max(p => p.y) + CeilingMargin;
                }
                if (FromAbove.Contains(name))
                {
                    pitch = 90f;
                }
                bool down = Mathf.Approximately(pitch, 90f);
                // What lies wholly over the ceiling (a roof, a cave's ceiling) is switched off in this copy, the rest
                // kept whole: a slice through a log or a rock would show its dark inside. From the front, only what
                // lies over the view's cut: below it stands the back wall's own machinery.
                // Every object of the room is drawn (a lake can sit beside the main model, not under it); the shot is
                // framed by the main model's, so a far backdrop doesn't shrink the room.
                var renderers = new List<Renderer>();
                var framing = new List<Renderer>();
                int layers = 0;
                var listing = new StringBuilder();
                Shader flat = Shader.Find(FlatShader);
                string capTry = Tries.FirstOrDefault(t => t.StartsWith("cap="));
                float cap = capTry != null && float.TryParse(capTry.Substring(4),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                    out float tried) ? tried
                    : Capped.TryGetValue(name, out float picked) ? picked : float.PositiveInfinity;
                foreach (Renderer r in room.transform.GetComponentsInChildren<Renderer>())
                {
                    Bounds b = r.bounds;
                    // The black outline shell the game draws round an object from the side shows as a black blob
                    // from above: left out of this copy, and an object that is only a shell with it.
                    // So is a builder's placeholder, still on Unity's own default material (a plain white cube).
                    Material[] materials = r.sharedMaterials;
                    Material[] own = !down && name.StartsWith(Outlined) ? materials
                        : Array.FindAll(materials, m => m == null || m.shader.name != OutlineShader);
                    string path = PathOf(r.transform, room.transform, false);
                    string skipped = !r.enabled ? "off"
                        : Hidden.Contains((name, path)) || Tries.Contains("hide=" + path.Replace(' ', '+')) ? "hidden"
                        : b.min.y - RoomAway.y >= Parked ? "parked"
                        : b.min.y - RoomAway.y > cap ? "capped"
                        : r is ParticleSystemRenderer || r is TrailRenderer
                            || own.Any(m => m != null && m.name.StartsWith(LightBeam)) ? "effect"
                        : ceiling != null && b.min.y - RoomAway.y > ceiling.Value + (down ? 0f : ClipAbove)
                            ? "over the ceiling"
                        : own.Length == 0 ? "outline shell"
                        : own.All(m => m != null && m.name == DefaultMaterial) ? "placeholder"
                        : Drained.Contains(name) && own.Any(m => m != null && m.shader.name.Contains("Water"))
                            ? "drained" : null;
                    if (skipped != null)
                    {
                        if (list)
                        {
                            listing.Append("\n  (" + skipped + ") ").Append(PathOf(r.transform, room.transform))
                                .Append($" y {b.min.y - RoomAway.y:F1} to {b.max.y - RoomAway.y:F1}");
                        }
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
                    if (!down && r is MeshRenderer model)
                    {
                        FacingOnly(model, Quaternion.Euler(pitch, 0f, 0f) * Vector3.back, cap + RoomAway.y, made);
                    }
                    renderers.Add(r);
                    layers |= 1 << r.gameObject.layer;
                    if (main == null || r.transform.IsChildOf(main))
                    {
                        framing.Add(r);
                    }
                    if (list)
                    {
                        Material m = r.sharedMaterial;
                        listing.Append("\n  ").Append(PathOf(r.transform, room.transform)).Append(" <")
                            .Append(r.GetType().Name).Append("> ").Append(m != null ? m.name : "none").Append(" / ")
                            .Append(m != null ? m.shader.name : "none")
                            .Append(m != null && m.HasProperty("_Color") ? " colour " + m.color : "")
                            .Append($" x {b.center.x - RoomAway.x:F1} z {b.center.z - RoomAway.z:F1}, ")
                            .Append($"y {b.min.y - RoomAway.y:F1} to {b.max.y - RoomAway.y:F1}");
                        MeshFilter shape = r.GetComponent<MeshFilter>();
                        if (shape != null && shape.sharedMesh != null && !shape.sharedMesh.isReadable)
                        {
                            listing.Append(", unreadable");
                        }
                        if (r.sharedMaterials.Length > 1)
                        {
                            listing.Append(", materials ").Append(string.Join(", ", r.sharedMaterials.Select(x =>
                                x == null ? "none" : x.name + " / " + x.shader.name
                                    + (x.HasProperty("_Color") ? " " + x.color : "")
                                    + (x.mainTexture != null ? " tex " + x.mainTexture.name : ""))));
                        }
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
                // The game's camera stands inside the room: the view starts just before the entity nearest it (from
                // straight down, the highest), so what stands before them all (a roof, a dome, a front wall) is cut.
                shot = Shoot(framing.Count > 0 ? framing : renderers, layers, Quaternion.Euler(pitch, 0f, 0f), 0f,
                    pixelsPerUnit, spots.Select(p => p + RoomAway).ToList(),
                    down ? CeilingMargin + ClipAbove
                        : Tries.Contains("nocut") || Uncut.Contains(name) ? float.PositiveInfinity : FrontMargin,
                    png, p => p + RoomAway);
                // Where the game's camera may go (MainManager.CamLimiter, LimitRadius): the room it ever shows.
                if (shot != null && control != null)
                {
                    Shot taken = shot.Value;
                    taken.Extra = new[] { control.camlimitneg.x, control.camlimitneg.z, control.camlimitpos.x,
                        control.camlimitpos.z, control.centralpoint.x, control.centralpoint.z, control.tetherdistance };
                    shot = taken;
                }
                if (list)
                {
                    listing.Append("\n  components: ").Append(string.Join(", ", room
                        .GetComponentsInChildren<Component>(true).GroupBy(c => c.GetType().Name)
                        .Select(g => g.Key + " " + g.Count()).ToArray()));
                    listing.Append($"\n  game camera field of view {MainManager.MainCamera?.fieldOfView}");
                    foreach (Light l in Object.FindObjectsOfType<Light>())
                    {
                        listing.Append($"\n  light {PathOf(l.transform, l.transform.root)} {l.type} {l.color}")
                            .Append($" x{l.intensity} on {l.enabled} at {l.transform.position - RoomAway}");
                    }
                    log.LogInfo($"[dev] roomshot {name}, renderers drawn:" + listing);
                }
                if (shot == null)
                {
                    return $"{name}: nothing to render";
                }
                string message = $"{name}: {Path.GetFileName(png)} ({shot.Value.Width}x{shot.Value.Height}), "
                    + $"{renderers.Count} renderers" + (main != null ? " under " + main.name : "")
                    + $", ambient {RenderSettings.ambientLight}"
                    + (down && ceiling != null ? $", seen from {ceiling.Value:F1} up" : $", from {pitch} degrees");
                log.LogInfo("[dev] roomshot " + message);
                return message;
            }
            finally
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;
                others.ForEach(l => l.enabled = true);
                Object.Destroy(room);
                made.ForEach(Object.Destroy);
            }
        }

        // Renders the renderers' bounds from an orthographic camera turned by rotation, width pixels wide (or at
        // pixelsPerUnit), to png; the shot says where game points (x, z) of 0 and 1 land, from the picture's top left.
        // The view starts margin before the nearest of the spots: what stands before that is left out.
        // The shot is framed by the renderers given and sees the layers given besides theirs. What nothing fills is
        // left clear: each pixel's alpha comes from the picture over black and over white, so a see-through effect
        // that never writes alpha (a pit's depths, a glow) still shows.
        private static Shot? Shoot(List<Renderer> renderers, int layers, Quaternion rotation, float width,
            float pixelsPerUnit, List<Vector3> spots, float margin, string png, Func<Vector3, Vector3> point)
        {
            if (renderers.Count == 0)
            {
                return null;
            }
            Bounds bounds = renderers[0].bounds;
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
                if (spots.Count > 0)
                {
                    cam.nearClipPlane = Mathf.Max(cam.nearClipPlane,
                        spots.Min(p => Vector3.Dot(p - cam.transform.position, cam.transform.forward)) - margin);
                }
                cam.cullingMask = layers;
                cam.clearFlags = CameraClearFlags.SolidColor;
                rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default,
                    8);
                cam.targetTexture = rt;
                Color32[] onBlack = Render(cam, rt, Color.black);
                Color32[] onWhite = Render(cam, rt, Color.white);
                cam.targetTexture = null;
                var pixels = new Color32[onBlack.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 b = onBlack[i], c = onWhite[i];
                    int a = Mathf.Clamp(255 - (c.r - b.r + c.g - b.g + c.b - b.b) / 3, 0, 255);
                    if (a > 0)
                    {
                        pixels[i] = new Color32((byte)Mathf.Min(255, b.r * 255 / a),
                            (byte)Mathf.Min(255, b.g * 255 / a), (byte)Mathf.Min(255, b.b * 255 / a), (byte)a);
                    }
                }
                if (!Tries.Contains("noclear"))
                {
                    ClearBlackShell(pixels, w, h);
                }
                var picture = new Texture2D(w, h, TextureFormat.ARGB32, false);
                picture.SetPixels32(pixels);
                File.WriteAllBytes(png, picture.EncodeToPNG());
                Object.Destroy(picture);

                // Through the viewport: once the target is released, screen points are the game window's.
                Vector2 Pixel(Vector3 p)
                {
                    Vector3 viewport = cam.WorldToViewportPoint(point(p));
                    return new Vector2(viewport.x * w, (1f - viewport.y) * h);
                }
                Vector2 origin = Pixel(Vector3.zero);
                return new Shot
                {
                    Width = w, Height = h, Origin = origin, XStep = Pixel(Vector3.right) - origin,
                    ZStep = Pixel(Vector3.forward) - origin, YStep = Pixel(Vector3.up) - origin,
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

        // A room's black shell (the dark round a cave, which the game's own camera sees only at its edges) reaches the
        // picture's edge: what is black from there on is cleared. An outline inside the room is walled off by colour.
        private static void ClearBlackShell(Color32[] pixels, int w, int h)
        {
            bool Dark(Color32 p) => p.a == 0 || (p.r <= BlackShell && p.g <= BlackShell && p.b <= BlackShell);
            var queue = new Queue<int>();
            var seen = new bool[pixels.Length];
            for (int x = 0; x < w; x++)
            {
                queue.Enqueue(x);
                queue.Enqueue((h - 1) * w + x);
            }
            for (int y = 0; y < h; y++)
            {
                queue.Enqueue(y * w);
                queue.Enqueue(y * w + w - 1);
            }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                if (seen[i] || !Dark(pixels[i]))
                {
                    continue;
                }
                seen[i] = true;
                pixels[i] = new Color32(0, 0, 0, 0);
                int x = i % w;
                if (x > 0)
                {
                    queue.Enqueue(i - 1);
                }
                if (x < w - 1)
                {
                    queue.Enqueue(i + 1);
                }
                if (i >= w)
                {
                    queue.Enqueue(i - w);
                }
                if (i < pixels.Length - w)
                {
                    queue.Enqueue(i + w);
                }
            }
            // So is a black area too thick for an outline, wherever it is (one walled in by a lit edge), cleared where
            // it lies: spreading along the outlines joined to it would clear them all.
            int full = (2 * ThickBlack + 1) * (2 * ThickBlack + 1);
            int[] dark = Summed(w, h, i => Dark(pixels[i]));
            int[] inner = Summed(w, h, i => Box(dark, w, h, i, ThickBlack) == full);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (Dark(pixels[i]) && Box(inner, w, h, i, ThickBlack) > 0)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
            }
        }

        // A copy of a model with only its faces turned toward the camera: the game's camera inside a room sees the
        // fronts of its ceiling and walls; from outside, their backs (black) stood before it. Faces wholly above cap go
        // too, none sliced.
        private static void FacingOnly(MeshRenderer r, Vector3 toCamera, float cap, List<Object> made)
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
            {
                return;
            }
            Matrix4x4 world = r.transform.localToWorldMatrix;
            float turned = world.determinant < 0f ? -1f : 1f;
            Vector3[] v = mesh.vertices;
            Mesh cut = Object.Instantiate(mesh);
            made.Add(cut);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] t = mesh.GetTriangles(s);
                var kept = new List<int>(t.Length);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = world.MultiplyPoint3x4(v[t[i]]);
                    Vector3 b = world.MultiplyPoint3x4(v[t[i + 1]]), c = world.MultiplyPoint3x4(v[t[i + 2]]);
                    Vector3 face = Vector3.Cross(b - a, c - a);
                    if (turned * Vector3.Dot(face, toCamera) > 0f && Mathf.Min(a.y, b.y, c.y) <= cap)
                    {
                        kept.Add(t[i]);
                        kept.Add(t[i + 1]);
                        kept.Add(t[i + 2]);
                    }
                }
                cut.SetTriangles(kept, s);
            }
            filter.sharedMesh = cut;
        }

        // A summed-area table of a pixel test, a row and a column wider, for counts over boxes.
        private static int[] Summed(int w, int h, Func<int, bool> test)
        {
            var sums = new int[(w + 1) * (h + 1)];
            for (int y = 0; y < h; y++)
            {
                int row = 0;
                for (int x = 0; x < w; x++)
                {
                    row += test(y * w + x) ? 1 : 0;
                    sums[(y + 1) * (w + 1) + x + 1] = sums[y * (w + 1) + x + 1] + row;
                }
            }
            return sums;
        }

        // How many pixels within r of pixel i (a box, cut at the picture's edge) the table counted.
        private static int Box(int[] sums, int w, int h, int i, int r)
        {
            int x0 = Math.Max(0, i % w - r), x1 = Math.Min(w, i % w + r + 1);
            int y0 = Math.Max(0, i / w - r), y1 = Math.Min(h, i / w + r + 1);
            return sums[y1 * (w + 1) + x1] - sums[y0 * (w + 1) + x1] - sums[y1 * (w + 1) + x0]
                + sums[y0 * (w + 1) + x0];
        }

        private static Color32[] Render(Camera cam, RenderTexture rt, Color background)
        {
            cam.backgroundColor = background;
            cam.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var picture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            picture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            Color32[] pixels = picture.GetPixels32();
            Object.Destroy(picture);
            return pixels;
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
