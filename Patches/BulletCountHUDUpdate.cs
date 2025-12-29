using HarmonyLib;
using UnityEngine;

namespace YABetterReload.Patches
{
    [HarmonyPatch(typeof(BulletCountHUD), "Update")]
    internal class BulletCountHUDUpdate
    {
        private static float _lastUpdateTime = 0f;
        private static float _updateInterval = 0.5f;

        private static void Postfix(BulletCountHUD __instance)
        {
            if (Time.time - _lastUpdateTime > _updateInterval)
            {
                _lastUpdateTime = Time.time;
                Traverse.Create(__instance).Method("ChangeTotalCount").GetValue();
            }
        }
    }
}
