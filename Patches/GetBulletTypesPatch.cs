using HarmonyLib;
using ItemStatsSystem;
using System;
using System.Collections.Generic;

namespace YABetterReload.Patches
{
    [HarmonyPatch(typeof(ItemSetting_Gun), "GetBulletTypesInInventory")]
    internal static class GetBulletTypesPatch
    {
        private static bool Prefix(ItemSetting_Gun __instance, Inventory inventory, ref Dictionary<int, BulletTypeInfo> __result)
        {
            if (!ReloaderCore.IsPlayerInventory(inventory) || __instance == null || inventory == null)
                return true;

            string? caliber = __instance.Item?.Constants?.GetString("Caliber".GetHashCode(), null);
            if (string.IsNullOrEmpty(caliber))
                return true;

            Dictionary<int, BulletTypeInfo> resDict = ReloaderCore.GetCachedAmmoTypesByCaliber(caliber);
            if (resDict == null) return true;
            __result = resDict;
            return false;
        }
    }
}
