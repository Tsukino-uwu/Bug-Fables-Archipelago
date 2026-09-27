using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: ScriptEngine's FileSystemWatcher throws in this game's Mono, so this polls our DLL once a second and
    // sets ScriptEngine's private shouldReload flag. Inert unless ScriptEngine is loaded. Its state goes to one line in
    // BepInEx/bugfablesap-reload.txt (the loaded build's hash as copy-dev prints it, or what a new copy waits for), so
    // a reload is checked by reading one file, never by watching the log.
    internal sealed class DevReload
    {
        private readonly ManualLogSource log;
        private readonly string dllPath;
        private readonly object scriptEngine;
        private readonly FieldInfo shouldReload;
        private DateTime lastWrite;
        private float untilPoll;
        private bool requested;

        private DevReload(ManualLogSource log, string dllPath, object scriptEngine, FieldInfo shouldReload)
        {
            this.log = log;
            this.dllPath = dllPath;
            this.scriptEngine = scriptEngine;
            this.shouldReload = shouldReload;
            lastWrite = File.GetLastWriteTimeUtc(dllPath);
            loaded = Hash(dllPath);
            Status("loaded " + loaded);
        }

        private readonly string loaded;
        private static readonly string statusPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-reload.txt");

        // The first 12 hex digits of the DLL's SHA-256, upper case: what copy-dev.ps1 prints.
        private static string Hash(string path)
        {
            try
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (FileStream file = File.OpenRead(path))
                {
                    return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").Substring(0, 12);
                }
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private void Status(string text)
        {
            try
            {
                File.WriteAllText(statusPath, text + " (" + DateTime.Now.ToString("HH:mm:ss") + ")\n");
            }
            catch (Exception e)
            {
                log.LogWarning("DevReload: couldn't write " + statusPath + ": " + e.Message);
            }
        }

        internal static DevReload TryCreate(ManualLogSource log)
        {
            string dll = Path.Combine(Path.Combine(Paths.BepInExRootPath, "scripts"), "BugFablesAP.dll");
            if (!File.Exists(dll))
            {
                return null;
            }
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = asm.GetType("ScriptEngine.ScriptEngine", false);
                if (type == null)
                {
                    continue;
                }
                FieldInfo field = type.GetField("shouldReload", BindingFlags.Instance | BindingFlags.NonPublic);
                UnityEngine.Object instance = UnityEngine.Object.FindObjectOfType(type);
                if (field == null || instance == null)
                {
                    log.LogWarning("DevReload: ScriptEngine found but its shouldReload field or instance is not; reload stays manual (F6).");
                    return null;
                }
                log.LogInfo("DevReload: polling BepInEx/scripts/BugFablesAP.dll for changes.");
                return new DevReload(log, dll, instance, field);
            }
            return null;
        }

        private bool waitingReported;

        internal void Tick()
        {
            if (requested)
            {
                return;
            }
            untilPoll -= Time.unscaledDeltaTime;
            if (untilPoll > 0f)
            {
                return;
            }
            untilPoll = 1f;
            DateTime now = File.Exists(dllPath) ? File.GetLastWriteTimeUtc(dllPath) : lastWrite;
            if (now != lastWrite)
            {
                // Never mid-scene, dialogue or battle: a reload there orphans the stand-ins the old plugin made.
                MainManager mm = MainManager.instance;
                if (mm != null && (mm.inevent || mm.message || MainManager.battle != null))
                {
                    if (!waitingReported)
                    {
                        waitingReported = true;
                        string what = MainManager.battle != null ? "battle" : mm.inevent ? "scene" : "talk";
                        Status($"waiting for the {what} to end (loaded {loaded}, a new copy on disk)");
                        log.LogInfo("DevReload: BugFablesAP.dll changed; waiting for the scene, talk or battle to end.");
                    }
                    return;
                }
                requested = true;
                Status($"reloading (was {loaded})");
                shouldReload.SetValue(scriptEngine, true);
                log.LogInfo("DevReload: BugFablesAP.dll changed; asked ScriptEngine to reload.");
            }
        }
    }
}
