using HarmonyLib;

namespace ExtendedLocalization
{
    [HarmonyPatch(typeof(Localization), nameof(Localization.SetupLanguage))]
    internal static class LocalizationPatch
    {
        // Runs after the game has loaded its own table for the language,
        // so our entries override existing tokens and add new ones.
        [HarmonyPostfix]
        private static void Postfix(Localization __instance, string language)
        {
            Plugin.CurrentLanguage = language;
            TranslationStore.Apply(__instance, language);
        }
    }
}
