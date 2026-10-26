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
        public static void Apply(Localization localization, string language)
        {
            if (!Plugin.Enabled.Value || localization == null || string.IsNullOrEmpty(language))
            {
                return;
            }

            string path = FindFile(language);
            if (path == null)
            {
                if (Plugin.VerboseLogging.Value)
                {
                    Plugin.Log.LogInfo("No translation file for language '" + language + "'.");
                }
                return;
            }

            Dictionary<string, string> entries;
            try
            {
                entries = Parse(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Failed to read " + Path.GetFileName(path) + ": " + e.Message);
                return;
            }

            int applied = 0;
            foreach (KeyValuePair<string, string> entry in entries)
            {
                if (!SetToken(localization, entry.Key, entry.Value))
                {
                    Plugin.Log.LogError("Cannot write to the game's translation table: neither Localization.AddWord " +
                                        "nor the field m_translations was found (game version changed?).");
                    return;
                }

                applied++;
                if (Plugin.VerboseLogging.Value)
                {
                    Plugin.Log.LogInfo("  $" + entry.Key + " = " + entry.Value);
                }
            }

            Plugin.Log.LogInfo("Applied " + applied + " token(s) from " + Path.GetFileName(path) + ".");
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
