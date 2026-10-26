using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExtendedLocalization
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "BioHazardN7.ExtendedLocalization";
        public const string PluginName = "ExtendedLocalization";
        public const string PluginVersion = "1.0.0";

        private const long ReloadDelayTicks = TimeSpan.TicksPerMillisecond * 500;

        internal static ManualLogSource Log;
        internal static string TranslationsDir;
        internal static string CurrentLanguage;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> HotReload;
        internal static ConfigEntry<bool> VerboseLogging;

        private Harmony _harmony;
        private FileSystemWatcher _watcher;
        private volatile bool _reloadPending;
        private long _lastChangeTicks;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Enable or disable the mod. Takes effect the next time the game language is loaded.");
            HotReload = Config.Bind("General", "Hot reload", true,
                "Reload translation files automatically when they change on disk.");
            VerboseLogging = Config.Bind("General", "Verbose logging", false,
                "Log every applied token and every parsing problem in detail.");

            TranslationsDir = Path.Combine(Paths.ConfigPath, PluginName);
            Directory.CreateDirectory(TranslationsDir);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            if (HotReload.Value)
            {
                StartWatcher();
            }
        }

        private void Update()
        {
            if (!_reloadPending)
            {
                return;
            }

            long last = Volatile.Read(ref _lastChangeTicks);
            if (DateTime.UtcNow.Ticks - last < ReloadDelayTicks)
            {
                return;
            }

            _reloadPending = false;
            ReloadCurrentLanguage();
        }

        private void OnDestroy()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }
        }

        private void StartWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(TranslationsDir)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    IncludeSubdirectories = false
                };
                _watcher.Changed += OnFileEvent;
                _watcher.Created += OnFileEvent;
                _watcher.Deleted += OnFileEvent;
                _watcher.Renamed += OnFileEvent;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Log.LogWarning("Could not start the file watcher, hot reload is disabled: " + e.Message);
            }
        }

        // Runs on a thread-pool thread: only set flags here, the real work happens in Update().
        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            if (!TranslationStore.IsTranslationFile(e.FullPath))
            {
                return;
            }

            Volatile.Write(ref _lastChangeTicks, DateTime.UtcNow.Ticks);
            _reloadPending = true;
        }

        private static void ReloadCurrentLanguage()
        {
            string language = CurrentLanguage;
            if (string.IsNullOrEmpty(language) || Localization.instance == null)
            {
                return;
            }

            Log.LogInfo("Translation files changed, reloading language '" + language + "'.");

            // Reloads the original game table; our postfix then re-applies the files from disk.
            Localization.instance.SetupLanguage(language);
        }
    }
}
