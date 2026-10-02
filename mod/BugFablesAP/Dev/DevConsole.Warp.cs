using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    internal static partial class DevConsole
    {
        private static string Loc(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "loc <n>";
            }
            long id = long.Parse(parts[1]);
            if (id < LocationIdBase)
            {
                id += LocationIdBase;
            }
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            if (pickups == null || !pickups.TryGetValue(id, out ApConnection.Pickup pickup))
            {
                return $"location {id} isn't a pickup in this seed (or not connected yet)";
            }
            if (pickup.Flag < 0)
            {
                // A crystal berry or a respawning pickup has no flag to find it by.
                return $"location {id} has no flag of its own: use warp {pickup.Map} @<entity name>";
            }
            MainManager.Maps map = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), pickup.Map);
            if (MainManager.instance.flags[pickup.Flag])
            {
                // Taken already: clear its flag so the map creates it again.
                MainManager.instance.flags[pickup.Flag] = false;
            }
            return StartWarp(map, pickup.Flag) + $" (location {id}, flag {pickup.Flag})";
        }

        private static string Warp(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "warp <map> [flag]";
            }
            MainManager.Maps map = int.TryParse(parts[1], out int number)
                ? (MainManager.Maps)number
                : (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), parts[1], true);
            // @<name>: land at the map's origin, then step beside the named entity, so a trigger starts on walking in,
            // not mid-warp.
            if (parts.Length > 2 && parts[2].StartsWith("@"))
            {
                // The rest of the line: entity names can have spaces ("Crystal Berry").
                pendingName = string.Join(" ", parts.Skip(2).ToArray()).Substring(1);
                return StartWarp(map, -1) + " (to " + pendingName + ")";
            }
            int flag = parts.Length > 2 ? int.Parse(parts[2]) : -1;
            return StartWarp(map, flag);
        }

        private static string pendingName;
        private static bool landed;

        private static string StartWarp(MainManager.Maps map, int flag)
        {
            if (MainManager.player == null || MainManager.instance.inevent || MainManager.instance.message)
            {
                return "not now: no player, or an event or dialogue is running";
            }
            string skipped = SkipAutoEvents(map);
            pendingMap = (int)map;
            pendingFlag = flag;
            pendingSince = Time.realtimeSinceStartup;
            // Warp onto the entity's own spot, never beside it: TransferMap waits for a walk to its target, which never
            // ends over water. FinishWarp guards the item, then steps aside once the transition is over.
            Vector3? at = flag >= 0 ? StartPosition(map, flag) : null;
            guarded = false;
            // A plain warp lands once, where walking in through a door into the map ends; a second move after arrival
            // could come after an enemy had already touched the party.
            Vector3[] door = at.HasValue || pendingName != null ? null : QualityOfLife.DoorInto(map, null);
            landed = door != null;
            Vector3 target = landed ? door[2] : at.HasValue ? at.Value + Vector3.up * 0.5f : Vector3.zero;
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, target));
            return "warping to " + map + (landed ? " (through a door into it)" : "") + skipped;
        }


        // A spot beside the entity with room for the party and safe ground below; null when none is.
        internal static Vector3? ClearSpot(Vector3 at)
        {
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (float distance in new[] { 2.5f, 1.5f })
            {
                foreach (Vector3 side in sides)
                {
                    Vector3 spot = at + side * distance + Vector3.up * 0.5f;
                    bool blocked = Physics.OverlapSphere(spot + Vector3.up * 0.5f, 0.45f, ~0,
                        QueryTriggerInteraction.Ignore)
                        .Any(c => MainManager.player == null
                        || !c.transform.IsChildOf(MainManager.player.transform.root));
                    // Water raycasts as ground: hazards (water, spikes, pits) carry the Hazards component.
                    bool ground = Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit hit, 4f, ~0,
                        QueryTriggerInteraction.Collide)
                        && hit.collider.GetComponentInParent<Hazards>() == null && !hit.collider.isTrigger;
                    if (!blocked && ground)
                    {
                        return spot;
                    }
                }
            }
            return null;
        }

        // Entity table: fields 6-8 are the start position, field 194 the activationflag.
        private static Vector3? StartPosition(MainManager.Maps map, int flag)
        {
            TextAsset data = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
            if (data == null)
            {
                return null;
            }
            foreach (string line in data.ToString().Split('\n'))
            {
                string[] f = line.Split('}');
                if (f.Length > 194 && f[194].Trim() == flag.ToString())
                {
                    return new Vector3(Convert.ToSingle(f[6]), Convert.ToSingle(f[7]), Convert.ToSingle(f[8]));
                }
            }
            return null;
        }

        // Auto-start cutscenes run on arrival while their flag is off; out of story order they can crash, so a warp
        // marks them seen first.
        private static string SkipAutoEvents(MainManager.Maps map)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Maps/" + map);
            MapControl control = prefab == null ? null : prefab.GetComponent<MapControl>();
            if (control == null || control.autoevent == null || control.autoevent.Length == 0)
            {
                return "";
            }
            var skipped = new List<string>();
            foreach (Vector2 pair in control.autoevent)
            {
                int flag = (int)pair.x;
                if (!MainManager.instance.flags[flag])
                {
                    MainManager.instance.flags[flag] = true;
                    skipped.Add($"event {(int)pair.y} (flag {flag})");
                }
            }
            return skipped.Count == 0 ? "" : "; skipped its auto-start " + string.Join(", ", skipped.ToArray());
        }

        // The game's private end-of-event cleanup: clears inevent and minipause and resets the player.
        // The respawn-loop guard's test: lastpos and lastloadzone above the middle of the nearest water or hole, so the
        // next fall there loops.
        private static string HazardLoop()
        {
            PlayerControl player = MainManager.player;
            if (player == null)
            {
                return "hazardloop: no player";
            }
            Hazards nearest = null;
            Bounds area = default(Bounds);
            float best = float.MaxValue;
            foreach (Hazards hazard in UnityEngine.Object.FindObjectsOfType<Hazards>())
            {
                Collider c = hazard.GetComponent<Collider>();
                if (c == null)
                {
                    c = hazard.GetComponentInChildren<Collider>();
                }
                if (c == null || (hazard.type != Hazards.Type.Water && hazard.type != Hazards.Type.Hole))
                {
                    continue;
                }
                float d = (c.bounds.ClosestPoint(player.transform.position) - player.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = hazard;
                    area = c.bounds;
                }
            }
            if (nearest == null)
            {
                return "hazardloop: no water or hole on this map";
            }
            Vector3 spot = new Vector3(area.center.x, area.max.y + 1f, area.center.z);
            player.lastpos = spot;
            player.lastloadzone = spot;
            return $"lastpos and lastloadzone now above {nearest.name} ({nearest.type}) at {spot}: fall in to loop";
        }

        private static string Unstick()
        {
            System.Reflection.MethodInfo end = HarmonyLib.AccessTools.Method(typeof(EventControl), "EndEvent",
                new[] { typeof(bool) });
            if (end == null)
            {
                return "EventControl.EndEvent not found";
            }
            // Scenes run as StartCoroutine("Event" + id): stop the dead one first, or it runs on and touches what the
            // cleanup removes.
            string stopped = MainManager.events != null && MainManager.lastevent >= 0 ? "Event" + MainManager.lastevent
                : null;
            if (stopped != null)
            {
                MainManager.events.StopCoroutine(stopped);
            }
            end.Invoke(null, new object[] { false });
            if (MainManager.player != null)
            {
                MainManager.player.lockkeys = false;
                // A transfer stuck walking to an unreachable target: stop the walk and the transition it holds open.
                MainManager.player.entity?.StopForceMove();
            }
            MainManager.roomtransition = false;
            pendingMap = -1;
            // A dead cutscene can leave the camera pinned, the limits removed and the music faded out.
            MainManager.ResetCamera(true);
            MainManager.map?.RestoreLimit(false);
            MainManager.ChangeMusic();
            // It can also leave a black dimmer and the party parented to scenery with frozen physics (the boat scene).
            int freed = 0;
            foreach (EntityControl member in MainManager.GetPartyEntities() ?? new EntityControl[0])
            {
                if (member == null)
                {
                    continue;
                }
                if (member.transform.parent != null)
                {
                    member.transform.parent = null;
                    freed++;
                }
                // And gravity off with a forced animation (the trapdoor scene).
                member.LockRigid(false);
                if (member.rigid != null)
                {
                    member.rigid.useGravity = true;
                }
                member.overrideanim = false;
                member.animstate = 0;
                member.StopForceMove();
            }
            // A dialogue that died mid-line leaves message set, freezing the player: undo it as the game's dialogue end
            // does.
            MainManager mmd = MainManager.instance;
            bool talking = mmd.message || mmd.waitinput || mmd.prompt;
            mmd.message = false;
            mmd.waitinput = false;
            mmd.prompt = false;
            mmd.inlist = false;
            mmd.minipause = false;
            mmd.overridefollower = false;
            System.Reflection.FieldInfo boxField = HarmonyLib.AccessTools.Field(typeof(MainManager), "textbox");
            if (boxField != null && boxField.GetValue(boxField.IsStatic ? null : mmd) is Transform box && box != null)
            {
                DialogueAnim anim = box.GetComponent<DialogueAnim>();
                if (anim != null)
                {
                    anim.shrink = true;
                }
                UnityEngine.Object.Destroy(box.gameObject, 1f);
            }
            // That field holds only the letters; the speech box is maintextbox.
            if (MainManager.maintextbox != null)
            {
                DialogueAnim boxAnim = MainManager.maintextbox.GetComponent<DialogueAnim>();
                if (boxAnim != null)
                {
                    boxAnim.shrink = true;
                }
                UnityEngine.Object.Destroy(MainManager.maintextbox, 1f);
            }
            // An orphan Textbox(Clone) can remain under the GUI camera even so; with no dialogue running, remove it.
            int boxes = 0;
            if (MainManager.GUICamera != null)
            {
                foreach (Transform child in MainManager.GUICamera.transform)
                {
                    if (child.name == "Textbox(Clone)")
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                        boxes++;
                    }
                }
            }
            if (MainManager.player != null && MainManager.player.entity != null
                && MainManager.player.entity.rigid != null)
            {
                MainManager.player.entity.rigid.constraints = RigidbodyConstraints.FreezeRotation;
            }
            bool black = MainManager.instance.transitionobj != null && MainManager.instance.transitionobj.Length > 0
                && MainManager.instance.transitionobj[0] != null;
            if (black)
            {
                MainManager.PlayTransition(1, 0, 0.1f, Color.black);
            }
            return (stopped != null ? "stopped " + stopped + "; " : "")
                + "ran the game's end-of-event cleanup and camera, limit and music resets; inevent="
                + MainManager.instance.inevent
                + ", minipause=" + MainManager.instance.minipause + (talking ? "; closed a dead dialogue" : "")
                + (boxes > 0 ? $"; removed {boxes} leftover speech box(es)" : "")
                + $"; freed {freed} party member(s); "
                + (black ? "faded the screen back in" : "no fade left on screen");
        }

        private static float unlockAt = -1f;
        private static bool guarded;

        private static void FinishWarp()
        {
            if (unlockAt > 0f && Time.realtimeSinceStartup >= unlockAt)
            {
                unlockAt = -1f;
                if (MainManager.player != null && !open)
                {
                    MainManager.player.lockkeys = false;
                }
            }
            if (pendingMap < 0)
            {
                return;
            }
            if (Time.realtimeSinceStartup - pendingSince > 20f)
            {
                lastResult = "warp: the map never finished loading";
                shownAt = Time.realtimeSinceStartup;
                pendingMap = -1;
                return;
            }
            MapControl map = MainManager.map;
            if (map == null || (int)map.mapid != pendingMap || MainManager.player == null)
            {
                return;
            }
            // As soon as the map exists: the party lands on the item, so hold its touch until the step aside.
            if (!guarded && pendingFlag >= 0)
            {
                foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
                {
                    if (npc.activationflag == pendingFlag && npc.objecttype == NPCControl.ObjectTypes.Item)
                    {
                        npc.touchcooldown = 240f;
                        guarded = true;
                    }
                }
            }
            if (MainManager.roomtransition || MainManager.instance.intransition)
            {
                return;
            }
            string busy = landed ? null : MainManager.battle != null ? "a battle" : MainManager.instance.inevent
                ? "an event"
                : MainManager.instance.message ? "a dialogue" : null;
            if (landed || busy != null)
            {
                lastResult = "arrived on " + map.mapid
                    + (landed ? ", through a door into it" : $"; not stepped aside: {busy} started on arrival");
                shownAt = Time.realtimeSinceStartup;
                log.LogInfo("[dev] " + lastResult);
                pendingMap = -1;
                pendingName = null;
                landed = false;
                return;
            }
            List<NPCControl> entities = map.GetComponentsInChildren<NPCControl>(true).ToList();
            NPCControl target = pendingName != null ? entities.FirstOrDefault(e => e.name == pendingName)
                : pendingFlag >= 0 ? entities.FirstOrDefault(e => e.activationflag == pendingFlag) : null;
            string where = target != null ? "by " + (pendingName ?? "the entity with flag " + pendingFlag) : null;
            if (pendingName != null && target == null)
            {
                where = $"(nothing named {pendingName} here)";
            }
            pendingName = null;
            if (target == null)
            {
                target = entities.FirstOrDefault(e => e.objecttype == NPCControl.ObjectTypes.SavePoint)
                    ?? entities.FirstOrDefault(e => e.objecttype == NPCControl.ObjectTypes.DoorOtherMap);
                where = target != null ? "by " + target.name
                    + (pendingFlag >= 0 ? $" (nothing with flag {pendingFlag} here)" : "") : "at the map's origin";
            }
            if (target != null)
            {
                // First side with room and safe ground; the touch cooldown holds the pickup about 1.5 s.
                Vector3? spot = ClearSpot(target.transform.position);
                if (!spot.HasValue)
                {
                    // No safe side: stand on the entity's own spot, so the tester sees where it is.
                    spot = target.transform.position + Vector3.up * 0.5f;
                    where += "; no safe spot beside it, so on its own spot";
                }
                MainManager.player.transform.position = spot.Value;
                if (target.objecttype == NPCControl.ObjectTypes.Item)
                {
                    target.touchcooldown = Mathf.Max(target.touchcooldown, 90f);
                }
                // No walking for a second after arriving, so a held key doesn't carry the party into something.
                MainManager.player.lockkeys = true;
                unlockAt = Time.realtimeSinceStartup + 1f;
                blockUntil = Time.realtimeSinceStartup + 1.5f;
                MainManager.TeleportFollowers(true);
            }
            lastResult = "arrived on " + map.mapid + ", " + where;
            shownAt = Time.realtimeSinceStartup;
            log.LogInfo("[dev] " + lastResult);
            pendingMap = -1;
        }
    }
}
