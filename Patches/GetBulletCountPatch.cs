using HarmonyLib;
using ItemStatsSystem;
using System;

namespace YABetterReload.Patches
{
    [HarmonyPatch(typeof(ItemSetting_Gun), "GetBulletCountofTypeInInventory")]
    internal static class GetBulletCountPatch
    {
        private static void Postfix(ItemSetting_Gun __instance, int bulletItemTypeID, Inventory inventory, ref int __result)
        {
            if (!ReloaderCore.IsPlayerInventory(inventory) || !ReloaderCore.IsPlayerInventory(inventory) || !ReloaderCore.IsAmmoItem(bulletItemTypeID))
                return;
            int cachedAmmoCount = ReloaderCore.GetCachedAmmoCount(bulletItemTypeID);
            __result = cachedAmmoCount;
        }
    }
}