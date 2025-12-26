using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace YABetterReload
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string HARMONY_ID = "com.yourname.yatbetterreload"; // Changed back to user's specified ID
        private Harmony? _harmony;

        public void Awake()
        {
            Debug.Log("YABetterReload: Awake - Mod is loading.");
        }

        public void OnEnable()
        {
            Debug.Log("YABetterReload: OnEnable - Enabling mod and applying patches.");
            
            // Null check for Harmony instance to prevent re-initialization if OnEnable is called multiple times
            if (_harmony != null)
            {
                Debug.LogWarning("YABetterReload: Harmony instance already exists. OnEnable called multiple times without OnDisable?");
                return;
            }

            try
            {
                // Create and apply patches
                _harmony = new Harmony(HARMONY_ID);
                _harmony.PatchAll();

                // Log all patched methods
                var patchedMethods = _harmony.GetPatchedMethods().ToList();
                Debug.Log($"YABetterReload: Harmony patched a total of {patchedMethods.Count} methods.");
                foreach (var patchedMethod in patchedMethods)
                {
                    Debug.Log($"YABetterReload: Patched -> {patchedMethod.FullDescription()}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"YABetterReload: Error applying Harmony patches: {ex}");
                // Ensure harmony instance is null if patching failed
                _harmony = null; 
            }
        }

        public void OnDisable()
        {
            Debug.Log("YABetterReload: OnDisable - Disabling mod and removing patches.");

            // Unpatch all methods and release the Harmony instance
            if (_harmony != null)
            {
                _harmony.UnpatchAll(HARMONY_ID);
                _harmony = null;
                Debug.Log("YABetterReload: All patches have been removed.");
            }
            else
            {
                Debug.LogWarning("YABetterReload: Harmony instance was null. No patches to remove?");
            }
        }
    }
}
