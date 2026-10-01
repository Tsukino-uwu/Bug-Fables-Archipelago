using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Save crystals by the confirm button, as an NPC is talked to: the game only starts one from an attack's hitbox,
    // which Shuffle Field Moves can take away. And the Gameplay page's Healing crystals: every save crystal but the red
    // ones yellow (save and heal).
    internal static class SaveCrystals
    {
        internal static ConfigEntry<bool> AllHeal;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Func<bool> settingsOn;

        // Talking range as for an NPC: the game's check (distance under the entity's radius), with the radius most NPCs
        // have, since a crystal's own is 0.
        private const float Reach = 1.6f;

        private static MapControl scanned;
        private static readonly List<NPCControl> crystals = new List<NPCControl>();
        private static float cooldown;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on, Func<bool> settings)
        {
            log = logger;
            randomizerOn = on;
            settingsOn = settings;
            AllHeal = config.Bind("Gameplay", "HealingCrystals", false,
                "On: every save crystal but the red ones is yellow, so it heals HP and TP as well as saving. Off: as "
                + "the game has them. Switch it on the Gameplay page.");
            if (Hooks.Install(typeof(Colour), "crystals", "Healing crystals does nothing"))
            {
                log.LogInfo(
                    "[crystals] installed on NPCControl.SetUp; the confirm press through FieldMoves' DoJump prefix");
            }
        }

        internal static void Disable()
        {
            crystals.Clear();
            scanned = null;
        }

        // Tint and heal both read data[2] == 0; red DeadLander crystals (data[1] >= 10) do something else and stay.
        [HarmonyPatch(typeof(NPCControl), "SetUp")]
        private static class Colour
        {
            [HarmonyPrefix]
            private static void BeforeSetUp(NPCControl __instance)
            {
                if (AllHeal == null || !AllHeal.Value || settingsOn == null || !settingsOn()
                    || !IsSaveCrystal(__instance) || __instance.data[2] == 0)
                {
                    return;
                }
                __instance.data[2] = 0;
            }
        }

        private static bool IsSaveCrystal(NPCControl npc) =>
            npc != null && npc.entitytype == NPCControl.NPCType.Object
            && npc.objecttype == NPCControl.ObjectTypes.SavePoint
            && npc.data != null && npc.data.Length > 2 && npc.data[1] < 10;

        // The crystal the confirm button would use now, or null.
        private static NPCControl InReach()
        {
            MainManager mm = MainManager.instance;
            PlayerControl player = MainManager.player;
            if (randomizerOn == null || !randomizerOn() || mm == null || player == null || MainManager.map == null
                || MainManager.battle != null || MainManager.timeddemo || player.submarine || !MainManager.FreePlayer()
                || player.entity == null || !player.entity.onground)
            {
                return null;
            }
            // A person to talk to comes first, as the game's own confirm does.
            if (player.npc.Count > 0 && player.npc[0] != null
                && (player.npc[0].entitytype == NPCControl.NPCType.NPC
                    || player.npc[0].entitytype == NPCControl.NPCType.SemiNPC))
            {
                return null;
            }
            if (scanned != MainManager.map)
            {
                scanned = MainManager.map;
                crystals.Clear();
                foreach (NPCControl npc in MainManager.map.GetComponentsInChildren<NPCControl>(true))
                {
                    if (npc.objecttype == NPCControl.ObjectTypes.SavePoint)
                    {
                        crystals.Add(npc);
                    }
                }
            }
            NPCControl best = null;
            float bestDistance = Reach;
            foreach (NPCControl npc in crystals)
            {
                if (npc == null || !npc.gameObject.activeInHierarchy || !IsSaveCrystal(npc)
                    || npc.insideid != mm.insideid)
                {
                    continue;
                }
                float d = MainManager.GetDistance(npc.transform.position, player.transform.position, ignoreY: false);
                if (d < bestDistance)
                {
                    best = npc;
                    bestDistance = d;
                }
            }
            return best;
        }

        // The "?" the game shows over the player next to something to check.
        internal static void Tick()
        {
            if (cooldown > 0f)
            {
                cooldown -= MainManager.framestep;
            }
            if (InReach() != null)
            {
                MainManager.player.entity.emoticonid = 1;
                MainManager.player.entity.emoticoncooldown = 2f;
            }
        }

        // Called for a confirm press that would jump: the same steps as the game's hit on a crystal, the prompt last.
        internal static bool TryUse()
        {
            NPCControl crystal = cooldown > 0f ? null : InReach();
            if (crystal == null)
            {
                return false;
            }
            cooldown = 20f;
            crystal.entity.anim.Play("BounceUp");
            crystal.entity.PlaySound("Save", 0.5f);
            bool heals = crystal.data[2] == 0;
            if (heals)
            {
                MainManager.Heal();
            }
            MainManager.HitPart(crystal.transform.position + Vector3.up / 2f);
            crystal.Interact("save");
            log.LogInfo($"[crystals] confirm at {crystal.name}: {(heals ? "healed, " : "")}save prompt");
            return true;
        }
    }
}
