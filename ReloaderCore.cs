using Cysharp.Threading.Tasks;
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
    internal static class ReloaderCore
    {
        private static readonly ConcurrentDictionary<int, int> _ammoCountCache = new ConcurrentDictionary<int, int>();
        private static readonly ConcurrentDictionary<string, Dictionary<int, BulletTypeInfo>> _ammoTypesByCaliberCache = new ConcurrentDictionary<string, Dictionary<int, BulletTypeInfo>>();
        private static readonly ConcurrentDictionary<int, List<Item>> _ammoLocationsCache = new ConcurrentDictionary<int, List<Item>>();
        private static float _lastCacheUpdateTime = 0.0f;
        private const float CACHE_UPDATE_INTERVAL = 1f;
        private static bool _cacheDirty = true;
        internal static Inventory? PlayerInventory
        {
            get => LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory;
        }
        internal static Inventory PetInventory => PetProxy.PetInventory;

        public static void SubscribeEvents()
        {
            UnsubscribeEvents();
            Debug.Log("YABetterReload: Subscribe inventory change events");

            if (PlayerInventory != null)
                PlayerInventory.onContentChanged += OnInventoryChanged;
            if (PetInventory != null)
                PetInventory.onContentChanged += OnInventoryChanged;
        }

        public static void UnsubscribeEvents()
        {
            Debug.Log("YABetterReload: Unsubscribe inventory change events");
            if (PlayerInventory != null)
                PlayerInventory.onContentChanged -= OnInventoryChanged;

            if (PetInventory != null)
                PetInventory.onContentChanged -= OnInventoryChanged;
        }

        private static void OnInventoryChanged(Inventory inventory, int slot)
        {
            _cacheDirty = true;
        }

        private static void UpdateCache()
        {
            float time = Time.time;
            if (!_cacheDirty && time - _lastCacheUpdateTime < CACHE_UPDATE_INTERVAL) { return; }
            _ammoCountCache.Clear();
            _ammoTypesByCaliberCache.Clear();
            _ammoLocationsCache.Clear();
            foreach (Item ammo in GetAllAmmoItems())
            {
                int ammoTypeId = ammo.TypeID;
                int stackCount = ammo.StackCount <= 0 ? 0 : ammo.StackCount;
                _ammoCountCache.AddOrUpdate(ammoTypeId, stackCount, (Func<int, int, int>)((key, oldValue) => oldValue + stackCount));

                if (!_ammoLocationsCache.ContainsKey(ammoTypeId))
                    _ammoLocationsCache[ammoTypeId] = new List<Item>();
                _ammoLocationsCache[ammoTypeId].Add(ammo);

                string? key1 = ammo.Constants?.GetString("Caliber".GetHashCode(), null);
                if (!string.IsNullOrEmpty(key1))
                {
                    if (!_ammoTypesByCaliberCache.ContainsKey(key1))
                        _ammoTypesByCaliberCache[key1] = new Dictionary<int, BulletTypeInfo>();
                    if (!_ammoTypesByCaliberCache[key1].ContainsKey(ammoTypeId))
                        _ammoTypesByCaliberCache[key1][ammoTypeId] = new BulletTypeInfo()
                        {
                            bulletTypeID = ammoTypeId,
                            count = 0
                        };
                    _ammoTypesByCaliberCache[key1][ammoTypeId].count += stackCount;
                }
            }
            _cacheDirty = false;
        }

        internal static bool LocateCompatibleAmmo(ItemSetting_Gun gunConfig, out Item? ammoItem)
        {
            ammoItem = null;
            if (gunConfig == null)
                return false;
            string? key = gunConfig.Item?.Constants?.GetString("Caliber".GetHashCode(), null);
            if (string.IsNullOrEmpty(key))
                return false;

            UpdateCache();
            Dictionary<int, BulletTypeInfo> source;
            if (_ammoTypesByCaliberCache.TryGetValue(key, out source))
            {
                KeyValuePair<int, BulletTypeInfo> keyValuePair = source.FirstOrDefault<KeyValuePair<int, BulletTypeInfo>>();
                List<Item> objList;
                if (keyValuePair.Value != null && _ammoLocationsCache.TryGetValue(keyValuePair.Key, out objList) && objList.Count > 0)
                {
                    ammoItem = objList[0];
                    return true;
                }
            }
            return false;
        }

        internal static Dictionary<int, BulletTypeInfo> GetCachedAmmoTypesByCaliber(string caliber)
        {
            UpdateCache();
            Dictionary<int, BulletTypeInfo> dictionary;
            return _ammoTypesByCaliberCache.TryGetValue(caliber, out dictionary) ? new Dictionary<int, BulletTypeInfo>(dictionary) : new Dictionary<int, BulletTypeInfo>();
        }

        internal static int GetCachedAmmoCount(int ammoTypeId)
        {
            UpdateCache();
            int num;
            return _ammoCountCache.TryGetValue(ammoTypeId, out num) ? num : 0;
        }

        internal static async UniTask<List<Item>> GetAmmosOfAmount(
          Inventory inventory,
          int ammoTypeId,
          int requiredAmount)
        {
            if (!IsPlayerInventory(inventory))
                return new List<Item>();
            UpdateCache();
            List<Item> result = new List<Item>();
            List<Item> ammoLocations;
            if (requiredAmount <= 0 || !_ammoLocationsCache.TryGetValue(ammoTypeId, out ammoLocations))
                return result;
            int gatheredAmount = 0;
            List<UniTask<Item>> splitTasks = new List<UniTask<Item>>();
            foreach (Item obj in ammoLocations)
            {
                Item item = obj;
                if (item != null && gatheredAmount < requiredAmount)
                {
                    int remaining = requiredAmount - gatheredAmount;
                    int stackSize = item.StackCount > 0 ? item.StackCount : 0;
                    if (stackSize > 0)
                    {
                        if (stackSize > remaining)
                        {
                            splitTasks.Add(item.Split(remaining));
                            gatheredAmount += remaining;
                        }
                        else
                        {
                            item.Detach();
                            result.Add(item);
                            gatheredAmount += stackSize;
                        }

                    }
                }
                else
                    break;
            }
            if (splitTasks.Count > 0)
            {
                Item[] splitResults = await UniTask.WhenAll<Item>(splitTasks);
                Item[] objArray = splitResults;
                for (int index = 0; index < objArray.Length; ++index)
                {
                    Item splitItem = objArray[index];
                    if (splitItem != null)
                        result.Add(splitItem);
                }
            }
            _cacheDirty = true;
            return result;
        }

        private static IEnumerable<Item> TraverseAmmoInInventory(Inventory inventory)
        {
            if (inventory != null)
            {
                foreach (Item item in inventory)
                {
                    if (item == null) continue;
                    if (IsAmmoItem(item)) yield return item;
                    foreach (Item slotItem in TraverseAmmoInSlot(item))
                    {
                        if (slotItem != null)
                            yield return slotItem;
                    }

                }
            }
        }

        private static IEnumerable<Item> TraverseAmmoInSlot(Item item)
        {
            if (item == null || IsGunItem(item) || item.Slots == null || item.Slots.Count <= 0)
                yield break;

            foreach (Slot slot in item.Slots)
            {
                Item slotItem = slot.Content;
                if (slotItem != null)
                {
                    if (IsAmmoItem(slotItem)) yield return slotItem;
                    foreach (Item item1 in TraverseAmmoInSlot(slotItem))
                    {
                        yield return item1;
                    }
                }
            }
        }

        private static IEnumerable<Item> GetAllAmmoItems()
        {
            foreach (Inventory inventory in AllInventory())
            {
                foreach (Item item in TraverseAmmoInInventory(inventory))
                {
                        yield return item;

                }
            }
        }

        private static IEnumerable<Inventory> AllInventory()
        {
            if (PlayerInventory != null)
                yield return PlayerInventory;
            if (PetInventory != null)
                yield return PetInventory;
        }

        internal static bool IsPlayerInventory(Inventory inventory)
        {
            return inventory != null &&
                (inventory == PlayerInventory || inventory == PetInventory);
        }

        internal static bool IsAmmoItem(Item item)
        {
            return item != null && item.GetBool("IsBullet");
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
