using System;
using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using InputIOManager;
using UnityEngine;

namespace BugFablesAP
{
    // DeathLink, the Gameplay page's row: a party wipe that reaches the game's Game Over sends a death; a received one
    // strikes once play can take it (the game's own Game Over in a battle, Game Over then the last save outside one).
    // A death DeathLink caused is never sent back.
    internal static class DeathLinkGame
    {
        internal static ConfigEntry<bool> Enabled;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static ApConnection connection;
        private static Harmony harmony;

        private static MethodInfo deadParty;
        private static AccessTools.FieldRef<BattleControl, Coroutine> gameOver;
        private static FieldInfo overState;
        private static FieldInfo overSkipSetup;

        // A received death not yet struck, and who it came from.
        internal static bool Pending { get; private set; }
        // A death waiting or under way: nothing may save over the state it goes back from.
        internal static bool Busy => Pending || striking;
        private static string pendingFrom;
        // From the strike until play is back: deaths arriving meanwhile join this one.
        private static bool striking;
        private static int strikeFrames;
        // The next Game Over is the one a received death started: it sends nothing.
        private static bool nextGameOverIsLink;
        private static object linkGameOver;
        private static string waitingFor;

        internal static void Enable(ManualLogSource logger, string guid, ConfigFile config, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            Enabled = config.Bind("Gameplay", "DeathLink", false,
                "On: when your party is defeated, everyone in the room with DeathLink on is too, and their deaths reach you. "
                + "Can be switched mid-seed. Switch it on the Gameplay page.");
            Enabled.SettingChanged += (s, e) => connection.SetDeathLinkTag(Wanted());
            connection.DeathLinkWanted = Wanted;
            deadParty = AccessTools.Method(typeof(BattleControl), "DeadParty");
            FieldInfo over = AccessTools.Field(typeof(BattleControl), "gameover");
            MethodInfo gameOverMethod = AccessTools.Method(typeof(BattleControl), "GameOver");
            MethodInfo step = gameOverMethod != null ? AccessTools.EnumeratorMoveNext(gameOverMethod) : null;
            if (deadParty == null || over == null || step == null)
            {
                log.LogError($"[death] NOT installed (DeadParty {deadParty != null}, gameover {over != null}, GameOver's MoveNext {step != null}): "
                    + "DeathLink neither sends nor receives.");
                deadParty = null;
                return;
            }
            gameOver = AccessTools.FieldRefAccess<BattleControl, Coroutine>(over);
            overState = AccessTools.Field(step.DeclaringType, "<>1__state");
            overSkipSetup = AccessTools.Field(step.DeclaringType, "skipsetup");
            harmony = new Harmony(guid + ".death." + DateTime.UtcNow.Ticks);
            harmony.Patch(step, prefix: new HarmonyMethod(typeof(DeathLinkGame), nameof(BeforeGameOverStep)),
                postfix: new HarmonyMethod(typeof(DeathLinkGame), nameof(AfterGameOverStep)));
            log.LogInfo($"[death] installed on BattleControl.GameOver's steps (state {overState != null}, skipsetup {overSkipSetup != null})");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static bool Wanted() => Enabled != null && Enabled.Value && randomizerOn != null && randomizerOn();

        // The first step of a Game Over with its setup (the fade and the music), which only a party wipe starts; the
        // game's own re-shows of the menu skip the setup and are left alone.
        private static void BeforeGameOverStep(object __instance)
        {
            if (overState == null || (int)overState.GetValue(__instance) != 0 || overSkipSetup == null || (bool)overSkipSetup.GetValue(__instance))
            {
                return;
            }
            if (nextGameOverIsLink)
            {
                nextGameOverIsLink = false;
                linkGameOver = __instance;
                log.LogInfo("[death] Game Over from a received death: not sent");
                return;
            }
            if (!Wanted())
            {
                return;
            }
            string cause = (connection.SlotName ?? "Someone") + "'s party was defeated in Bug Fables.";
            if (!connection.SendDeath(cause))
            {
                log.LogInfo("[death] party defeated, but not connected with DeathLink: not sent");
            }
        }

        // The link's Game Over ends when a Retry starts the battle again; the game's Load destroys the battle instead.
        private static void AfterGameOverStep(object __instance, bool __result)
        {
            if (!__result && linkGameOver != null && ReferenceEquals(__instance, linkGameOver))
            {
                linkGameOver = null;
                EndStrike("the Game Over's menu was answered");
            }
        }

        private static void EndStrike(string why)
        {
            if (striking)
            {
                striking = false;
                log.LogInfo("[death] back in play (" + why + "): the next death counts again");
            }
        }

        internal static void Tick()
        {
            DeathLinkReceive();
            bool atTitle = MainManager.player == null && MainManager.battle == null && MainManager.map == null;
            if (atTitle && Pending)
            {
                Pending = false;
                log.LogInfo("[death] back at the title: the waiting death is dropped");
            }
            if (striking)
            {
                strikeFrames++;
                if (strikeFrames > 30 && atTitle)
                {
                    linkGameOver = null;
                    EndStrike("back at the title");
                    return;
                }
                // Outside a battle, free again after a reload's scene: a few frames, so a strike's own first frames never count.
                if (strikeFrames > 30 && MainManager.battle == null && MainManager.player != null && MainManager.FreePlayer()
                    && !MainManager.roomtransition)
                {
                    linkGameOver = null;
                    EndStrike("free after the reload");
                }
                return;
            }
            if (!Pending)
            {
                return;
            }
            if (!Wanted())
            {
                Pending = false;
                log.LogInfo("[death] DeathLink switched off: the waiting death is dropped");
                return;
            }
            TryStrike();
        }

        private static void DeathLinkReceive()
        {
            var death = connection?.TakeDeath();
            while (death != null)
            {
                string from = string.IsNullOrEmpty(death.Cause) ? death.Source + " died" : death.Cause;
                if (!Wanted())
                {
                    log.LogInfo("[death] received (" + from + ") with DeathLink off: ignored");
                }
                else if (MainManager.player == null && MainManager.battle == null)
                {
                    log.LogInfo("[death] received (" + from + ") outside a save file: ignored");
                }
                else if (striking || Pending)
                {
                    log.LogInfo("[death] received (" + from + ") while one is already " + (striking ? "under way" : "waiting") + ": joined to it");
                }
                else
                {
                    Pending = true;
                    pendingFrom = from;
                    waitingFor = null;
                    log.LogInfo("[death] received: " + from + "; strikes once play allows");
                }
                death = connection.TakeDeath();
            }
        }

        private static void Wait(string what)
        {
            if (waitingFor != what)
            {
                waitingFor = what;
                log.LogInfo("[death] waiting: " + what);
            }
        }

        private static void TryStrike()
        {
            MainManager mm = MainManager.instance;
            BattleControl battle = MainManager.battle;
            if (mm == null)
            {
                return;
            }
            if (battle != null)
            {
                if (MainManager.battlelossevent)
                {
                    Wait("a scripted fight (its loss is the story's)");
                    return;
                }
                if (battle.action || battle.alreadyending || battle.checkingdead != null || gameOver(battle) != null || mm.message || mm.pause)
                {
                    Wait("the party's turn in the battle");
                    return;
                }
                Strike();
                nextGameOverIsLink = true;
                log.LogInfo("[death] strikes in the battle (" + pendingFrom + "): the game's own Game Over");
                battle.StartCoroutine((IEnumerator)deadParty.Invoke(battle, null));
                return;
            }
            if (MainManager.player == null || MainManager.map == null || !MainManager.FreePlayer() || MainManager.roomtransition
                || mm.intransition || mm.inbattle)
            {
                Wait("a scene, a text box, a menu or a transition to end");
                return;
            }
            if (!InputIO.SaveExists(MainManager.saveslot))
            {
                Wait("a save to go back to (this file has none yet)");
                return;
            }
            Strike();
            log.LogInfo("[death] strikes on the map (" + pendingFrom + "): Game Over, then the last save");
            mm.StartCoroutine(OverworldGameOver());
        }

        private static void Strike()
        {
            Pending = false;
            striking = true;
            strikeFrames = 0;
            waitingFor = null;
        }

        // As the game's Game Over ends in Load: the music fades, the screen goes black, the Game Over sound, the last save.
        private static IEnumerator OverworldGameOver()
        {
            MainManager mm = MainManager.instance;
            mm.minipause = true;
            MainManager.player?.entity?.StopMoving(0);
            MainManager.FadeMusic(0.035f);
            MainManager.PlayTransition(0, 0, 0.05f, Color.black);
            AudioSource sound = MainManager.PlaySound("Gameover");
            if (sound != null)
            {
                sound.volume = MainManager.musicvolume;
            }
            yield return new WaitForSeconds(2.5f);
            MainManager.StopSound("Gameover", 0.002f);
            MainManager.ReloadSave();
            mm.inbattle = false;
            mm.minipause = true;
            mm.pause = false;
        }
    }
}
