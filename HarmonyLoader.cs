using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace YABetterReload
{
    internal static class HarmonyLoader
    {
        private const string HARMONY_ID = "com.shapooo.yatbetterreload";
        private static object? _harmonyInstance;

        private static bool LoadHarmony()
        {
            if (_harmonyInstance != null)
            {
                Debug.LogWarning("YABetterReload: Harmony instance already exists. OnEnable called multiple times without OnDisable?");
                return true;
            }

            try
            {
                var harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                if (harmonyType == null)
                {
                    if (!FindHarmonyLibLocally(out var harmonyAssembly))
                    {
                        Debug.LogError("YABetterReload: HarmonyLib not found. Please ensure Harmony is installed.");
                        return false;
                    }

                    harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
                    if (harmonyType == null)
                    {
                        Debug.LogError("YABetterReload: HarmonyLib.Harmony type not found in Harmony assembly.");
                        return false;
                    }
                }

                _harmonyInstance = Activator.CreateInstance(harmonyType, HARMONY_ID);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"YABetterReload: Error initializing Harmony: {ex}");
            }

            return false;
        }

        private static bool FindHarmonyLibLocally([NotNullWhen(true)] out Assembly? harmonyAssembly)
        {
            harmonyAssembly = null;
            try
            {
                var path = Path.GetDirectoryName(typeof(HarmonyLoader).Assembly.Location);
                if (path == null) return false;

                var targetAssemblyFile = Path.Combine(path, "0Harmony.dll");
                if (!File.Exists(targetAssemblyFile)) return false;

                try
                {
                    Debug.Log($"YABetterReload: Loading Assembly from: {targetAssemblyFile}");

                    var bytes = File.ReadAllBytes(targetAssemblyFile);
                    var targetAssembly = Assembly.Load(bytes);
                    harmonyAssembly = targetAssembly;

                    Debug.Log("YABetterReload: HarmonyLib Assembly Loaded Successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"YABetterReload: Error loading HarmonyLib assembly: {ex}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"YABetterReload: Error finding HarmonyLib assembly: {ex}");
            }

            return false;
        }

        public static bool LoadAllPatches()
        {
            if (!LoadHarmony()) return false;
            try
            {
                var patchAllMethod = _harmonyInstance!.GetType().GetMethod("PatchAll", [typeof(Assembly)]);
                patchAllMethod!.Invoke(_harmonyInstance, [typeof(HarmonyLoader).Assembly]);
                Debug.Log("YABetterReload: Harmony Patches Applied Successfully");

                var getPatchedMethods = _harmonyInstance.GetType().GetMethod("GetPatchedMethods", []);
                var result = getPatchedMethods!.Invoke(_harmonyInstance, [])!;
                var patchedMethods = ((IEnumerable<MethodBase>)result).ToList();
                foreach (var patchedMethod in patchedMethods)
                {
                    Debug.Log($"YABetterReload: Patched -> {patchedMethod.FullDescription()}");

                }
                Debug.Log($"YABetterReload: Harmony patched a total of {patchedMethods.Count} methods.");
                return true;
            }
            catch (Exception ex)
            {
                UnloadAllPatches();
                Debug.LogError($"YABetterReload: Error Applying Harmony Patches: {ex}");
            }

            return false;
        }

        public static void UnloadAllPatches()
        {
            if (_harmonyInstance == null)
            {
                Debug.LogError("YABetterReload: Harmony instance is null. Cannot remove patches.");
                return;
            }

            try
            {
                var unpatchAllMethod = _harmonyInstance.GetType().GetMethod("UnpatchAll", [typeof(string)]);
                unpatchAllMethod!.Invoke(_harmonyInstance, [HARMONY_ID]);
                Debug.Log("YABetterReload: Harmony Patches Removed Successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"YABetterReload: Error Removing Harmony Patches: {ex}");
            }

        }
    }
}
