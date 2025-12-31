using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace YABetterReload
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private bool _loaded = false;
        public void Awake()
        {
            Debug.Log("YABetterReload: Awake - Mod is loading.");
        }

        public void OnEnable()
        {
            if (HarmonyLoader.LoadAllPatches())
            {
                ReloaderCore.SubscribeEvents();
                _loaded = true;
                Debug.Log("YABetterReload: OnEnable - Enable mod and apply patches successfully.");
                return;
            }
            Debug.Log("YABetterReload: OnEnable - Enable mod and apply patches failed.");
        }

        public void OnDisable()
        {
            Debug.Log("YABetterReload: OnDisable - Disabling mod and removing patches.");
            ReloaderCore.UnsubscribeEvents();
            HarmonyLoader.UnloadAllPatches();
        }
    }
}
