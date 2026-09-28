using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    internal static partial class QualityOfLife
    {
        // The first spider fight (Event6) can't be won and ends by itself on turn 3: with Skip cutscenes it ends, the same
        // way, as soon as the player could act. The second fight (two enemies, flagvar 11 at 2) is a real one.
        private static readonly MethodInfo exitBattle = AccessTools.Method(typeof(BattleControl), "ExitBattle");
        private static readonly FieldInfo battleInEvent = AccessTools.Field(typeof(BattleControl), "inevent");
        private static readonly FieldInfo battleAction = AccessTools.Field(typeof(BattleControl), "action");

        private static class SpiderHook
        {
            [HarmonyPatch(typeof(BattleControl), "CheckEvent")]
            [HarmonyPrefix]
            private static bool BeforeCheckEvent(BattleControl __instance)
            {
                MainManager mm = MainManager.instance;
                if (!SkipCutscenes.Value || SettingsOn == null || !SettingsOn() || MainManager.lastevent != 6
                    || !mm.flags[GameFlags.PermitEvent] || mm.flags[GameFlags.LeifFollows] || (mm.flagvar[11] != 0 && mm.flagvar[11] != 1)
                    || __instance.enemydata == null || __instance.enemydata.Length != 1 || __instance.enemydata[0].animid != 2
                    || (bool)battleInEvent.GetValue(__instance) || (bool)battleAction.GetValue(__instance))
                {
                    return true;
                }
                exitBattle.Invoke(__instance, null);
                log.LogInfo("[qol] the first spider fight (Event6) ended at its start, as its third turn would");
                return false;
            }
        }

        // The trapdoor scene lands the party on its own spots; once it ends, the party enters the fall room again the way the
        // opened trapdoor leads in (the door room's way down), as any door arrival does.
        private const int TrapdoorEvent = 5;
        private static bool trapdoorLanding;

        private static void TickTrapdoorLanding(MainManager mm, string here)
        {
            // The scene's second half (the landing talk in the fall room) is replaced: it ends on the black screen right after
            // the fall, before its own placing (which at speed left the party far left, the camera swinging after them).
            if (here == "SnakemouthFallRoom" && mm.inevent && MainManager.lastevent == TrapdoorEvent)
            {
                CutTrapdoorScene();
            }
            // In the door room only once the scene was skipped (flag 14 set, no scene running).
            bool skipped = here == "SnakemouthDoorRoom" && mm.flags[GameFlags.TrapdoorFall];
            if (here != "SnakemouthFallRoom" && !skipped)
            {
                if (here != "SnakemouthDoorRoom")
                {
                    trapdoorLanding = false;
                }
                return;
            }
            if (MainManager.player == null || mm.inevent || mm.message || mm.minipause || MainManager.battle != null
                || mm.intransition || MainManager.roomtransition)
            {
                return;
            }
            trapdoorLanding = false;
            Vector3[] door = DoorInto(MainManager.Maps.SnakemouthFallRoom, "SnakemouthDoorRoom");
            if (door == null)
            {
                log.LogWarning("[qol] after the trapdoor scene: no door from SnakemouthDoorRoom into the fall room; the party stays where the scene left it");
                return;
            }
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)MainManager.Maps.SnakemouthFallRoom, MainManager.player.transform.position, door[1], door[2]));
            log.LogInfo("[qol] after the trapdoor scene" + (skipped ? " (skipped)" : "") + ": entering the fall room through the door room's way down");
        }

        private static void CutTrapdoorScene()
        {
            MainManager.events.StopCoroutine("Event" + TrapdoorEvent);
            // The scene turned the party's gravity off and forced their animations; the landing's transfer needs them normal.
            foreach (EntityControl member in MainManager.GetPartyEntities() ?? new EntityControl[0])
            {
                if (member == null)
                {
                    continue;
                }
                member.LockRigid(false);
                if (member.rigid != null)
                {
                    member.rigid.useGravity = true;
                }
                member.overrideanim = false;
                member.overrridejump = false;
                member.animstate = 0;
            }
            MainManager.map?.RestoreLimit(false);
            endEvent?.Invoke(null, null);
            MainManager.ChangeMusic("Cave0");
            log.LogInfo("[qol] the trapdoor scene ended after the fall (its landing talk replaced by the door arrival)");
        }

        private static class EventHook
        {
            [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
            [HarmonyPrefix]
            private static bool BeforeStartEvent(int id)
            {
                if (id == TrapdoorEvent && randomizerOn() && MainManager.map != null && MainManager.map.mapid.ToString() == "SnakemouthDoorRoom")
                {
                    trapdoorLanding = true;
                }
                if (id == OpeningEvent && randomizerOn() && MainManager.map != null
                    && MainManager.map.mapid.ToString() == OpeningMap)
                {
                    // Also once done: its trigger stays until the map reloads, and running the scene then crashes.
                    openingPending = !MainManager.instance.flags[GameFlags.PermitEvent];
                    endEvent?.Invoke(null, null);
                    log.LogInfo(openingPending ? "[qol] Event16 (the opening) skipped: the mod does what it leaves behind on the next free frame"
                        : "[qol] Event16 (the opening) refused: already done");
                    return false;
                }
                Scene scene = SceneFor(id);
                if (scene == null || scene.Flags == null || !SkipCutscenes.Value || SettingsOn == null || !SettingsOn()
                    || (scene.OnlyWhileUnset >= 0 && MainManager.instance.flags[scene.OnlyWhileUnset]))
                {
                    return true;
                }
                foreach (int flag in scene.Flags)
                {
                    MainManager.instance.flags[flag] = true;
                }
                if (scene.Discovery >= 0)
                {
                    MainManager.UpdateJounal(MainManager.Library.Discovery, scene.Discovery);
                }
                // A trigger freezes the player (minipause) and only the scene's end undoes it: end it the game's way.
                endEvent?.Invoke(null, null);
                log.LogInfo($"[qol] skipped Event{id} on {scene.Map}: set flags {string.Join(", ", scene.Flags.Select(f => f.ToString()).ToArray())}, "
                    + (scene.Discovery >= 0 ? $"discovery {scene.Discovery} now {MainManager.instance.librarystuff[(int)MainManager.Library.Discovery, scene.Discovery]}, " : "")
                    + (endEvent != null ? "ended it the game's way" : "EndEvent NOT found: the player may stay frozen"));
                return false;
            }
        }

        private static Scene SceneFor(int id)
        {
            string map = MainManager.map == null ? null : MainManager.map.mapid.ToString();
            return map == null ? null : Scenes.FirstOrDefault(s => s.Event == id && s.Map == map);
        }

        private static bool InFastScene()
        {
            // A battle a scene starts is played at the game's own speed.
            if (!SkipCutscenes.Value || !MainManager.instance.inevent || MainManager.battle != null || MainManager.instance.inbattle)
            {
                return false;
            }
            Scene scene = SceneFor(MainManager.lastevent);
            return scene != null && scene.Flags == null;
        }

        // The mod's hold-ups ask for ItemSwap.EmptyLine as their follow-up: answer |end|, which skips the wait for a press.
        private static class LineHook
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.GetDialogueText), typeof(int))]
            [HarmonyPrefix]
            private static bool BeforeGetLine(int id, ref string __result)
            {
                if (id != ItemSwap.EmptyLine)
                {
                    return true;
                }
                __result = "|end|";
                return false;
            }
        }
    }
}
