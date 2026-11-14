using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace ExtendedLocalization
{
    internal static class TranslationStore
    {
        // The merged result (English.yml, then the selected language on top). TranslatePatch reads it for every
        // lookup. It is replaced as a whole, never modified in place, and only from the main thread.
        internal static Dictionary<string, string> Overrides = new Dictionary<string, string>(StringComparer.Ordinal);

        private sealed class CachedFile
        {
            public DateTime LastWrite;
            public long Length;
            public Dictionary<string, string> Entries;
        }

        private static readonly Dictionary<string, CachedFile> Files = new Dictionary<string, CachedFile>(StringComparer.OrdinalIgnoreCase);

        public static void Rebuild(Localization localization, string selected)
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);

            if (Plugin.Enabled.Value && localization != null)
            {
                AddFile(merged, "English");
                if (!string.IsNullOrEmpty(selected) && !string.Equals(selected, "English", StringComparison.OrdinalIgnoreCase))
                {
                    AddFile(merged, selected);
                }
            }

            Overrides = merged;

            if (localization != null)
            {
                WriteToTable(localization, merged);
                EvictCaches(localization);
            }
        }

        private static void AddFile(Dictionary<string, string> merged, string language)
        {
            string path = FindFile(language);
            if (path == null)
            {
                if (Plugin.VerboseLogging.Value)
                {
                    Plugin.Log.LogInfo("No translation file for language '" + language + "'.");
                }
                return;
            }

            Dictionary<string, string> entries = GetEntries(path);
            if (entries == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> entry in entries)
            {
                merged[entry.Key] = entry.Value;
            }
        }

        // Files are parsed only when they are new or changed on disk, not on every SetupLanguage call.
        private static Dictionary<string, string> GetEntries(string path)
        {
            CachedFile cached;
            Files.TryGetValue(path, out cached);

            try
            {
                DateTime write = File.GetLastWriteTimeUtc(path);
                long length = new FileInfo(path).Length;
                if (cached != null && cached.LastWrite == write && cached.Length == length)
                {
                    return cached.Entries;
                }

                Dictionary<string, string> entries = Parse(path);
                Files[path] = new CachedFile { LastWrite = write, Length = length, Entries = entries };

                Plugin.Log.LogInfo("Loaded " + entries.Count + " token(s) from " + Path.GetFileName(path) + ".");
                if (Plugin.VerboseLogging.Value)
                {
                    foreach (KeyValuePair<string, string> entry in entries)
                    {
                        Plugin.Log.LogInfo("  $" + entry.Key + " = " + entry.Value);
                    }
                }

                return entries;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to read " + Path.GetFileName(path) + ": " + e.Message);
                return cached != null ? cached.Entries : null;
            }
        }

        // Also writes the tokens into the table of this instance, for code that reads the table directly.
        private static void WriteToTable(Localization localization, Dictionary<string, string> tokens)
        {
            foreach (KeyValuePair<string, string> entry in tokens)
            {
                if (!SetToken(localization, entry.Key, entry.Value))
                {
                    Plugin.Log.LogWarning("Cannot write to the game's translation table (neither Localization.AddWord " +
                                          "nor the field m_translations was found); only the lookup patch is used.");
                    return;
                }
            }
        }

        private static FieldInfo _cacheField;
        private static MethodInfo _evictAll;
        private static FieldInfo _instanceField;
        private static bool _cacheResolved;

        // The game caches finished strings (also "[token]" results), so a string that was localized before our
        // tokens were known would stay wrong. Dropping the cache makes the next lookup go through TranslatePatch.
        private static void EvictCaches(Localization localization)
        {
            if (!_cacheResolved)
            {
                _cacheResolved = true;
                _cacheField = AccessTools.Field(typeof(Localization), "m_cache");
                _instanceField = AccessTools.Field(typeof(Localization), "m_instance");
                if (_cacheField != null)
                {
                    _evictAll = AccessTools.Method(_cacheField.FieldType, "EvictAll");
                }
            }

            EvictCache(localization);
            if (_instanceField != null)
            {
                // Reading the field does not start an initialization, unlike Localization.instance.
                EvictCache(_instanceField.GetValue(null) as Localization);
            }
        }

        private static void EvictCache(Localization localization)
        {
            if (localization == null || _cacheField == null || _evictAll == null)
            {
                return;
            }

            try
            {
                object cache = _cacheField.GetValue(localization);
                if (cache != null)
                {
                    _evictAll.Invoke(cache, null);
                }
            }
            catch (Exception)
            {
                // The cache is only an optimization; ignore problems here.
            }
        }

        // The language chosen in the game settings, not the one passed to the current SetupLanguage call.
        internal static string GetSelectedLanguage(Localization localization, string fallback)
        {
            try
            {
                string selected = localization.GetSelectedLanguage();
                if (!string.IsNullOrEmpty(selected))
                {
                    return selected;
                }
            }
            catch (Exception)
            {
                // fall through to the field / fallback below
            }

            FieldInfo field = AccessTools.Field(typeof(Localization), "m_language");
            string value = field != null ? field.GetValue(localization) as string : null;
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static MethodInfo _addWord;
        private static FieldInfo _translations;
        private static bool _resolved;

        // AddWord is not public in every game version, so it is found by reflection.
        // If it does not exist, the private translation dictionary is used directly.
        private static void Resolve()
        {
            if (_resolved)
            {
                return;
            }
            _resolved = true;

            _addWord = AccessTools.Method(typeof(Localization), "AddWord", new[] { typeof(string), typeof(string) });
            _translations = AccessTools.Field(typeof(Localization), "m_translations");

            if (Plugin.VerboseLogging.Value)
            {
                Plugin.Log.LogInfo("AddWord found: " + (_addWord != null) + ", m_translations found: " + (_translations != null));
            }
        }

        // The game stores tokens without the leading '$'. Existing tokens are replaced, unknown ones are added.
        private static bool SetToken(Localization localization, string key, string value)
        {
            Resolve();

            if (_addWord != null)
            {
                _addWord.Invoke(localization, new object[] { key, value });
                return true;
            }

            Dictionary<string, string> table = _translations != null
                ? _translations.GetValue(localization) as Dictionary<string, string>
                : null;
            if (table != null)
            {
                table[key] = value;
                return true;
            }

            return false;
        }

        internal static bool IsTranslationFile(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase);
        }

        private static string[] GetTranslationFiles()
        {
            var files = new List<string>();
            foreach (string file in Directory.GetFiles(Plugin.TranslationsDir))
            {
                if (IsTranslationFile(file))
                {
                    files.Add(file);
                }
            }
            return files.ToArray();
        }

        private static string FindFile(string language)
        {
            foreach (string file in GetTranslationFiles())
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(file), language, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
            return null;
        }

        private static Dictionary<string, string> Parse(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int split = line.IndexOfAny(new[] { '=', ':' });
                if (split <= 0)
                {
                    Warn(path, i + 1, "no ':' found, line skipped");
                    continue;
                }

                string key = line.Substring(0, split).Trim();
                string value = line.Substring(split + 1).Trim();

                if (key.StartsWith("$", StringComparison.Ordinal))
                {
                    key = key.Substring(1);
                }

                if (key.Length == 0 || key.IndexOf(' ') >= 0)
                {
                    Warn(path, i + 1, "invalid token name, line skipped");
                    continue;
                }

                bool doubleQuoted = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
                value = Unquote(value);
                if (doubleQuoted)
                {
                    value = value.Replace("\\\"", "\"");
                }
                value = value.Replace("\\n", "\n");

                if (result.ContainsKey(key))
                {
                    Warn(path, i + 1, "duplicate token '" + key + "', the last value is used");
                }
                result[key] = value;
            }

            return result;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                {
                    return value.Substring(1, value.Length - 2);
                }
            }
            return value;
        }

        private static void Warn(string path, int lineNumber, string message)
        {
            Plugin.Log.LogWarning(Path.GetFileName(path) + ":" + lineNumber + " " + message);
        }
    }
}
