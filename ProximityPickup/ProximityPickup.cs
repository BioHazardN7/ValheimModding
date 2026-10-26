using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ProximityPickup
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class ProximityPickupPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "BioHazardN7.ProximityPickup";
        public const string PluginName = "ProximityPickup";
        public const string PluginVersion = "1.0.0";

        private const string DefaultPrefabs =
            "Pickable_DolmenTreasure, Pickable_DvergrMineTreasure, Pickable_ForestCryptRandom, " +
            "Pickable_Item, Pickable_MorkHallaTreasure, Pickable_MountainCaveRandom, Pickable_SunkenCryptRandom";

        internal static ProximityPickupPlugin Instance;

        private sealed class Tracked
        {
            public ZNetView View;
            public int Id;
            public float NextTry;
        }

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _radius;
        private ConfigEntry<int> _maxPerFrame;
        private ConfigEntry<float> _retryDelay;
        private ConfigEntry<string> _prefabList;
        private ConfigEntry<bool> _debugLog;

        private readonly HashSet<int> _prefabHashes = new HashSet<int>();
        private int _prefabCount;

        // Only objects whose prefab is in the list are ever stored here
        private readonly List<Tracked> _tracked = new List<Tracked>();
        private readonly HashSet<int> _trackedIds = new HashSet<int>();

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            _enabled = Config.Bind("General", "Enabled", true,
                "Enable or disable the mod.");

            _radius = Config.Bind("General", "Radius", 5f,
                new ConfigDescription(
                    "Radius (in meters) around the player in which listed objects are picked up automatically.",
                    new AcceptableValueRange<float>(0.5f, 50f)));

            _maxPerFrame = Config.Bind("General", "MaxPerFrame", 3,
                new ConfigDescription(
                    "Maximum number of objects interacted with per frame. Prevents spikes when many objects enter the radius at once.",
                    new AcceptableValueRange<int>(1, 50)));

            _retryDelay = Config.Bind("General", "RetryDelay", 3f,
                new ConfigDescription(
                    "Seconds to wait before interacting with the same object again (protects against network spam).",
                    new AcceptableValueRange<float>(0.5f, 30f)));

            _prefabList = Config.Bind("General", "Prefabs", DefaultPrefabs,
                "Comma-separated list of prefab names to pick up automatically. " +
                "Names are written without \"(Clone)\"; case is ignored.");

            _debugLog = Config.Bind("Debug", "DebugLog", false,
                "Log every registered object (prefab name, attached components) " +
                "and the result of each interaction. Useful for diagnosing objects that do not react.");

            _prefabList.SettingChanged += (_, __) => OnPrefabListChanged();
            ParsePrefabs();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(ProximityPickupPlugin).Assembly);

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Tracking {_prefabCount} prefab(s).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------------------------
        // Registration: called once for every ZNetView that is spawned (Harmony postfix).
        // Cost per spawned object: one integer hash lookup.
        // ---------------------------------------------------------------------------------
        internal void Register(ZNetView nview)
        {
            if (nview == null || _prefabHashes.Count == 0 || Application.isBatchMode)
                return;

            var zdo = nview.GetZDO();
            if (zdo == null || !_prefabHashes.Contains(zdo.GetPrefab()))
                return;

            int id = nview.GetInstanceID();
            if (!_trackedIds.Add(id))
                return;

            _tracked.Add(new Tracked { View = nview, Id = id });

            if (_debugLog.Value)
                Logger.LogInfo($"Registered '{GetPrefabName(nview.gameObject)}'; components: {DescribeComponents(nview)}");
        }

        private void OnPrefabListChanged()
        {
            ParsePrefabs();

            // Rare event: rebuild the registry once from objects that already exist
            _tracked.Clear();
            _trackedIds.Clear();
            foreach (var nview in UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
                Register(nview);
        }

        private void ParsePrefabs()
        {
            _prefabHashes.Clear();
            _prefabCount = 0;

            var parts = (_prefabList.Value ?? string.Empty)
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var name = part.Trim();
                if (name.Length == 0) continue;
                if (_prefabHashes.Add(StableHash(name)))
                    _prefabCount++;
            }
        }

        // ---------------------------------------------------------------------------------
        // Per-frame check: iterates only the (small) list of matching objects.
        // ---------------------------------------------------------------------------------
        private void Update()
        {
            if (!_enabled.Value || _tracked.Count == 0)
                return;

            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || player.InCutscene() || player.IsTeleporting())
                return;

            // Do not interfere while the inventory, menu, chat or console is open
            if (InventoryGui.IsVisible() || Menu.IsVisible() ||
                (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible())
                return;

            Vector3 center = player.transform.position;
            float radius = _radius.Value;
            float sqrRadius = radius * radius;
            float now = Time.time;
            int done = 0;
            int max = _maxPerFrame.Value;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                var t = _tracked[i];
                var nview = t.View;

                // Object was destroyed (picked up, unloaded, ...) -> drop it from the registry
                if (nview == null)
                {
                    RemoveAt(i);
                    continue;
                }

                if (t.NextTry > now)
                    continue;

                if ((nview.transform.position - center).sqrMagnitude > sqrRadius)
                    continue;

                if (!nview.IsValid())
                    continue;

                string prefabName = _debugLog.Value ? GetPrefabName(nview.gameObject) : null;

                bool result = TryInteract(nview, player, prefabName);

                if (_debugLog.Value)
                    Logger.LogInfo($"Interact('{prefabName}') returned {result}");

                t.NextTry = now + _retryDelay.Value;

                if (++done >= max)
                    break;
            }
        }

        private void RemoveAt(int index)
        {
            int last = _tracked.Count - 1;
            _trackedIds.Remove(_tracked[index].Id);
            _tracked[index] = _tracked[last];
            _tracked.RemoveAt(last);
        }

        /// <summary>Same as pressing E: Pickable first, otherwise any other interactable on the object.</summary>
        private bool TryInteract(ZNetView nview, Player player, string prefabName)
        {
            var pickable = nview.GetComponentInChildren<Pickable>();
            if (pickable != null)
                return pickable.Interact(player, false, false);

            var interactable = nview.GetComponentInChildren<Interactable>();
            if (interactable != null)
                return interactable.Interact(player, false, false);

            if (_debugLog.Value)
                Logger.LogWarning($"'{prefabName}' has no Pickable or Interactable component.");
            return false;
        }

        private static string DescribeComponents(ZNetView nview)
        {
            var seen = new HashSet<string>();
            var sb = new StringBuilder();
            foreach (var c in nview.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string typeName = c.GetType().Name;
                if (!seen.Add(typeName)) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(typeName);
            }
            return sb.ToString();
        }

        // Same algorithm Valheim uses for prefab hashes (kept local to avoid an extra assembly reference)
        private static int StableHash(string str)
        {
            unchecked
            {
                int num = 5381;
                int num2 = num;
                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    num = ((num << 5) + num) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    num2 = ((num2 << 5) + num2) ^ str[i + 1];
                }
                return num + num2 * 1566083941;
            }
        }

        private static string GetPrefabName(GameObject go)
        {
            string name = go.name;
            int idx = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return idx >= 0 ? name.Substring(0, idx).Trim() : name;
        }
    }

    /// <summary>Notifies the plugin whenever a network object is spawned.</summary>
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class ZNetViewAwakePatch
    {
        private static void Postfix(ZNetView __instance)
        {
            ProximityPickupPlugin.Instance?.Register(__instance);
        }
    }
}
