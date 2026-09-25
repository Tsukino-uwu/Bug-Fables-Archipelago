using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: ScriptEngine's FileSystemWatcher throws in this game's Mono, so this polls our DLL once a second and
    // sets ScriptEngine's private shouldReload flag. Inert unless ScriptEngine is loaded.
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
                        log.LogInfo("DevReload: BugFablesAP.dll changed; waiting for the scene, talk or battle to end.");
                    }
                    return;
                }
                requested = true;
                shouldReload.SetValue(scriptEngine, true);
                log.LogInfo("DevReload: BugFablesAP.dll changed; asked ScriptEngine to reload.");
            }
        }
    }
}
