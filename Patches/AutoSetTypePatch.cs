using HarmonyLib;
using ItemStatsSystem;
using System;

namespace YABetterReload.Patches
{
    [HarmonyPatch(typeof(ItemSetting_Gun), "AutoSetTypeInInventory")]
    internal static class AutoSetTypePatch
    {
        private static void Postfix(ItemSetting_Gun __instance, Inventory inventory, ref bool __result)
        {
            Item? ammoItem;
            if (__instance == null || __result || !ReloaderCore.IsPlayerInventory(inventory) || !ReloaderCore.LocateCompatibleAmmo(__instance, out ammoItem))
                return;
            __instance.SetTargetBulletType(ammoItem);
            __result = true;
            return;
        }
    }
}
