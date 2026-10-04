using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Music Shuffle (slot_data music_map, jingle_map). The game's music player keeps its own track, muted, so everything
    // that reads or saves what's playing (the victory fanfare's check, Samira's list, the track resumed after a battle)
    // stays the game's. A second source plays the seed's track in its place and follows the player's volume, fades,
    // pitch and pauses, as a music zone plays over a muted player. Jingles are swapped where they're played and stopped.
    internal static class MusicShuffle
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        private static AudioSource voice;
        // The game's clip the voice stands in for, and the track it plays.
        private static AudioClip following;
        private static int playedId = -1;
        private static bool starting;
        private static bool paused;
        private static bool muted;
        // The game's clip Samira started: played as it is until the player moves on to another track.
        private static AudioClip samiraClip;
        // Where each track's voice was when the game left it, for the game's resume after a battle.
        private static readonly Dictionary<string, float> resumeAt = new Dictionary<string, float>();
        private static readonly HashSet<string> missing = new HashSet<string>();

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> randomizerEnabled)
        {
            log = logger;
            connection = conn;
            randomizerOn = randomizerEnabled;
            Hooks.Install(typeof(SoundHooks), "music",
                "jingles play as the game has them, and the factory elevator's crossfade sounds the game's own song");
        }

        internal static void Disable()
        {
            Release();
            if (voice != null)
            {
                UnityEngine.Object.Destroy(voice.gameObject);
            }
            voice = null;
        }

        // In LateUpdate, after the game's coroutines: a track the game switched this frame is muted before it's heard.
        internal static void LateTick()
        {
            AudioSource game = MainManager.music != null && MainManager.music.Length > 0 ? MainManager.music[0] : null;
            string track = game != null ? Played(game.clip) : null;
            if (track == null)
            {
                Release();
                return;
            }
            if (following != game.clip && !Follow(game, track))
            {
                Release();
                return;
            }
            game.mute = muted = true;
            voice.volume = game.volume;
            voice.pitch = game.pitch;
            if (game.isPlaying && !voice.isPlaying)
            {
                if (starting)
                {
                    // The game sets a resumed track's time a frame after its clip, as it starts it: the voice resumes
                    // its own.
                    voice.time = game.time > 0.5f && resumeAt.TryGetValue(following.name, out float at)
                        && at < voice.clip.length ? at : 0f;
                    voice.Play();
                }
                else if (paused)
                {
                    voice.UnPause();
                }
                else
                {
                    voice.Play();
                }
                starting = paused = false;
            }
            else if (!game.isPlaying && voice.isPlaying)
            {
                voice.Pause();
                paused = true;
            }
            // The game's LoopMusic, for the voice's own track.
            float[][] loops = MainManager.musicloop;
            if (loops != null && playedId >= 0 && playedId < loops.Length && loops[playedId][0] != 0f
                && voice.time >= loops[playedId][0])
            {
                voice.time = loops[playedId][1];
            }
        }

        // The track to play in place of the game's clip; null plays the game's own.
        private static string Played(AudioClip clip)
        {
            Dictionary<string, string> tracks = connection?.MusicMap;
            if (clip == null || tracks == null || tracks.Count == 0 || randomizerOn == null || !randomizerOn())
            {
                samiraClip = null;
                return null;
            }
            if (SamiraPlaying())
            {
                samiraClip = clip;
                return null;
            }
            if (clip == samiraClip)
            {
                return null;
            }
            samiraClip = null;
            return tracks.TryGetValue(clip.name, out string played) && played != clip.name ? played : null;
        }

        // Samira's own event plays the song picked from her list; her notes float while it plays.
        private static bool SamiraPlaying()
        {
            NPCControl samira = MainManager.map != null ? MainManager.map.samira : null;
            return samira != null && samira.internaltransform != null && samira.internaltransform.Length != 0
                && samira.internaltransform[0] != null;
        }

        private static bool Follow(AudioSource game, string track)
        {
            AudioClip clip = Enum.IsDefined(typeof(MainManager.Musics), track)
                ? Resources.Load<AudioClip>("Audio/Music/" + track) : null;
            if (clip == null)
            {
                if (missing.Add(track))
                {
                    log.LogWarning($"[music] {track} not found: {game.clip.name} plays as itself");
                }
                return false;
            }
            Remember();
            if (voice == null)
            {
                var holder = new GameObject("BugFablesAP.Music");
                UnityEngine.Object.DontDestroyOnLoad(holder);
                voice = holder.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = true;
                voice.priority = game.priority;
                voice.spatialBlend = game.spatialBlend;
                voice.outputAudioMixerGroup = game.outputAudioMixerGroup;
                voice.ignoreListenerPause = game.ignoreListenerPause;
            }
            voice.Stop();
            voice.clip = clip;
            starting = true;
            paused = false;
            following = game.clip;
            playedId = (int)Enum.Parse(typeof(MainManager.Musics), track);
            log.LogInfo($"[music] {game.clip.name} plays as {track}");
            return true;
        }

        private static void Remember()
        {
            if (following != null && voice != null && voice.clip != null)
            {
                resumeAt[following.name] = voice.time;
            }
        }

        private static void Release()
        {
            if (following != null)
            {
                Remember();
                voice.Stop();
                voice.clip = null;
                following = null;
                playedId = -1;
            }
            if (muted)
            {
                AudioSource game = MainManager.music != null && MainManager.music.Length > 0
                    ? MainManager.music[0] : null;
                if (game != null)
                {
                    game.mute = false;
                }
                muted = false;
            }
        }

        private static AudioClip Jingle(AudioClip clip, bool playing)
        {
            Dictionary<string, string> jingles = connection?.JingleMap;
            if (clip == null || jingles == null || jingles.Count == 0 || randomizerOn == null || !randomizerOn()
                || !jingles.TryGetValue(clip.name, out string played) || played == clip.name)
            {
                return clip;
            }
            AudioClip swapped = Resources.Load<AudioClip>("Audio/Sounds/" + played);
            if (swapped == null)
            {
                if (missing.Add(played))
                {
                    log.LogWarning($"[music] jingle {played} not found: {clip.name} plays as itself");
                }
                return clip;
            }
            if (playing)
            {
                log.LogInfo($"[music] jingle {clip.name} plays as {played}");
            }
            return swapped;
        }

        // A seamless change starts the next song on a sound slot in step with the playing one and fades across: made for
        // the factory's two versions of one song (the elevator, its only use). With either swapped it is a plain fade,
        // which the voice follows; else that slot would sound the game's own song.
        private static bool KeepSeamless(AudioClip next, int id)
        {
            Dictionary<string, string> tracks = connection?.MusicMap;
            if (tracks == null || tracks.Count == 0 || randomizerOn == null || !randomizerOn())
            {
                return true;
            }
            AudioClip playing = MainManager.music != null && id >= 0 && id < MainManager.music.Length
                ? MainManager.music[id].clip : null;
            bool Swapped(AudioClip clip) =>
                clip != null && tracks.TryGetValue(clip.name, out string played) && played != clip.name;
            return !Swapped(next) && !Swapped(playing);
        }

        // Every PlaySound, and every StopSound by name or clip, ends in the last two (a stop by slot needs no swap);
        // stopping swaps the same way, so the game's own stop by name ("Gameover") stops the jingle it started.
        private static class SoundHooks
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ChangeMusic), typeof(AudioClip), typeof(float),
                typeof(int), typeof(bool))]
            [HarmonyPrefix]
            private static void BeforeChangeMusic(AudioClip musicclip, int id, ref bool seamless)
            {
                if (seamless && !KeepSeamless(musicclip, id))
                {
                    seamless = false;
                    log.LogInfo($"[music] {musicclip?.name}: the seamless switch made a plain fade (shuffled)");
                }
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.PlaySound), typeof(AudioClip), typeof(int),
                typeof(float), typeof(float), typeof(bool))]
            [HarmonyPrefix]
            private static void BeforePlay(ref AudioClip soundclip)
            {
                soundclip = Jingle(soundclip, playing: true);
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.StopSound), typeof(AudioClip), typeof(float))]
            [HarmonyPrefix]
            private static void BeforeStop(ref AudioClip clip)
            {
                clip = Jingle(clip, playing: false);
            }
        }
    }
}
