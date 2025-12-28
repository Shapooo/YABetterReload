using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YABetterReload
{
    internal class ReloaderCore
    {
        private static readonly ConcurrentDictionary<int, int> _ammoCountCache = new ConcurrentDictionary<int, int>();
        private static readonly ConcurrentDictionary<string, Dictionary<int, BulletTypeInfo>> _ammoTypesByCaliberCache = new ConcurrentDictionary<string, Dictionary<int, BulletTypeInfo>>();
        private static readonly ConcurrentDictionary<int, List<Item>> _ammoLocationsCache = new ConcurrentDictionary<int, List<Item>>();
        private static bool _cacheDirty = true;
        internal static Inventory? PlayerInventory
        {
            get => LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory;
        }
        internal static Inventory PetInventory => PetProxy.PetInventory;

        internal static bool IsPlayerInventory(Inventory inventory)
        {
            return inventory != null &&
                (inventory == ReloaderCore.PlayerInventory || inventory == ReloaderCore.PetInventory);
        }

        private static void UpdateCache()
        {
            if (_cacheDirty) { return; }
            ReloaderCore._ammoCountCache.Clear();
            ReloaderCore._ammoTypesByCaliberCache.Clear();
            ReloaderCore._ammoLocationsCache.Clear();
            foreach (Item ammo in ReloaderCore. GetAllAmmoItems())
            {
                int ammoTypeId = ammo.TypeID;
                int stackCount = ammo.StackCount <= 0 ? 0 : ammo.StackCount;
                ReloaderCore._ammoCountCache.AddOrUpdate(ammoTypeId, stackCount, (Func<int, int, int>)((key, oldValue) => oldValue + stackCount));

                if (!ReloaderCore._ammoLocationsCache.ContainsKey(ammoTypeId))
                    ReloaderCore._ammoLocationsCache[ammoTypeId] = new List<Item>();
                ReloaderCore._ammoLocationsCache[ammoTypeId].Add(ammo);

                string? key1 = ammo.Constants?.GetString("Caliber".GetHashCode(), null);
                if (!string.IsNullOrEmpty(key1))
                {
                    if (!ReloaderCore._ammoTypesByCaliberCache.ContainsKey(key1))
                        ReloaderCore._ammoTypesByCaliberCache[key1] = new Dictionary<int, BulletTypeInfo>();
                    if (!ReloaderCore._ammoTypesByCaliberCache[key1].ContainsKey(ammoTypeId))
                        ReloaderCore._ammoTypesByCaliberCache[key1][ammoTypeId] = new BulletTypeInfo()
                        {
                            bulletTypeID = ammoTypeId,
                            count = 0
                        };
                    ReloaderCore._ammoTypesByCaliberCache[key1][ammoTypeId].count += stackCount;
                }
            }
            _cacheDirty = false;
        }

        static ReloaderCore()
        {
            if (ReloaderCore.PlayerInventory != null)
                ReloaderCore.PlayerInventory.onContentChanged += new Action<Inventory, int>(ReloaderCore.OnInventoryChanged);
            if (ReloaderCore.PetInventory != null)
                ReloaderCore.PetInventory.onContentChanged += new Action<Inventory, int>(ReloaderCore.OnInventoryChanged);
        }

        private static void OnInventoryChanged(Inventory inventory, int slot)
        {
            ReloaderCore._cacheDirty = true;
        }

        internal static bool LocateCompatibleAmmo(ItemSetting_Gun gunConfig, out Item? ammoItem)
        {
            ammoItem = null;
            if (gunConfig == null)
                return false;
            string? key = gunConfig.Item?.Constants?.GetString("Caliber".GetHashCode(), null);
            if (string.IsNullOrEmpty(key))
                return false;

            ReloaderCore.UpdateCache();
            Dictionary<int, BulletTypeInfo> source;
            if (ReloaderCore._ammoTypesByCaliberCache.TryGetValue(key, out source))
            {
                KeyValuePair<int, BulletTypeInfo> keyValuePair = source.FirstOrDefault<KeyValuePair<int, BulletTypeInfo>>();
                List<Item> objList;
                if (keyValuePair.Value != null && ReloaderCore._ammoLocationsCache.TryGetValue(keyValuePair.Key, out objList) && objList.Count > 0)
                {
                    ammoItem = objList[0];
                    return true;
                }
            }
            return false;
        }

        internal static int GetCachedAmmoCount(int ammoTypeId)
        {
            ReloaderCore.UpdateCache();
            int num;
            return ReloaderCore._ammoCountCache.TryGetValue(ammoTypeId, out num) ? num : 0;
        }

        private static IEnumerable<Item> TraverseInventory(Inventory inventory)
        {
            if (inventory != null)
            {
                HashSet<Item> processedItems = new HashSet<Item>();
                foreach (Item item in inventory)
                {
                    if (item == null) continue;
                    yield return item;
                    if (processedItems.Add(item))
                        Debug.LogWarning("Dup item {}", item);
                    if (!ReloaderCore.IsGunItem(item) && item.Slots != null && item.Slots.Count > 0)
                    {
                        foreach (Item slotItem in ReloaderCore.TraverseSlot(item))
                        {
                            if (slotItem != null)
                                yield return slotItem;
                        }
                    }
                }
            }
        }

        private static IEnumerable<Item> TraverseSlot(Item item)
        {
            if (item == null || ReloaderCore.IsGunItem(item))
                yield break;

            foreach (Slot slot in item.Slots)
            {
                Item slotItem = slot.Content;
                if (slotItem != null)
                {
                    yield return slotItem;
                    foreach (Item item1 in ReloaderCore.TraverseSlot(slotItem))
                    {
                        yield return item1;
                    }
                }
            }
        }

        private static IEnumerable<Item> GetAllAmmoItems()
        {
            HashSet<Item> processedItems = new HashSet<Item>();
            foreach (Inventory inventory in ReloaderCore.AllInventory())
            {
                foreach (Item item in ReloaderCore.TraverseInventory(inventory))
                {
                    if (item != null && IsAmmoItem(item))
                    {
                        if (processedItems.Add(item))
                            Debug.LogWarning("Duplicate item {}", item);
                        yield return item;
                    }
                }
            }
        }

        private static IEnumerable<Inventory> AllInventory()
        {
            if (ReloaderCore.PlayerInventory != null)
                yield return ReloaderCore.PlayerInventory;
            if (ReloaderCore.PetInventory != null)
                yield return ReloaderCore.PetInventory;
        }

        internal static bool IsAmmoItem(Item item)
        {
            int itemTypeId = item.TypeID;
            return IsAmmoItem(itemTypeId);
        }

        internal static bool IsAmmoItem(int itemTypeId)
        {
            ItemMetaData metaData = ItemAssetsCollection.GetMetaData(itemTypeId);
            if (metaData.tags == null)
                return false;
            foreach (UnityEngine.Object tag in metaData.tags)
            {
                if (tag == GameplayDataSettings.Tags.Bullet)
                    return true;
            }
            return false;
        }

        private static bool IsGunItem(Item item)
        {
            return item != null && item.Tags != null && item.Tags.Contains("Gun");
        }
    }
}
