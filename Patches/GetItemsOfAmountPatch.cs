using HarmonyLib;
using ItemStatsSystem;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace YABetterReload.Patches
{
    [HarmonyPatch(typeof(ItemExtensions), "GetItemsOfAmount")]
    internal static class GetItemsOfAmountPatch
    {
        private static bool Prefix(Inventory inventory, int itemTypeID, int amount, ref UniTask<List<Item>> __result)
        {
            if (inventory == null || !ReloaderCore.IsPlayerInventory(inventory) || !ReloaderCore.IsAmmoItem(itemTypeID))
                return true;
            __result = ReloaderCore.GetAmmosOfAmount(inventory, itemTypeID, amount);
            return false;
        }
    }
}
