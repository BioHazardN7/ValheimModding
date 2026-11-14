using HarmonyLib;

namespace ExtendedLocalization
{
    // Runs after the game (or any other mod) has loaded a language table. Rebuilds our merged token set:
    // English.yml first, then the file of the language selected in the game settings on top. The result does
    // not depend on the order of SetupLanguage calls or on which of them came last.
    // NOTE: never touch Localization.instance in here: during the game's own constructor it is still null,
    // and reading it would start a second initialization.
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class SetupLanguagePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Localization __instance, string language)
        {
            string selected = TranslationStore.GetSelectedLanguage(__instance, language);
            Plugin.CurrentLanguage = selected;
            TranslationStore.Rebuild(__instance, selected);
        }
    }

    // The universal part: every token lookup of every Localization instance goes through this method, so our
    // tokens win no matter how many instances or tables exist, and no matter who clears or replaces them.
    // Cost: one dictionary lookup per translated word (the game caches whole strings, so this is rare).
    [HarmonyPatch(typeof(Localization), "Translate", new[] { typeof(string) })]
    internal static class TranslatePatch
    {
        private static bool _loggedFirstHit;

        [HarmonyPrefix]
        private static bool Prefix(string word, ref string __result)
        {
            string value;
            if (word != null && TranslationStore.Overrides.TryGetValue(word, out value))
            {
                __result = value;
                if (!_loggedFirstHit && Plugin.VerboseLogging.Value)
                {
                    _loggedFirstHit = true;
                    Plugin.Log.LogInfo("Translate patch is active: first token served from the override table was '" + word + "'.");
                }
                return false;
            }

            return true;
        }
    }
}
