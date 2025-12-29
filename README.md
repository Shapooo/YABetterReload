# YABetterReload - Better Reload Experience for Duckov

[中文](#中文说明) | [English](#english-description)

---

## 中文说明

**YABetterReload** 是一款针对《逃离鸭科夫》（Duckov）的体验增强 Mod。它通过重构底层的弹药检索逻辑，解决了原版游戏在复杂库存情况下无法识别子弹的问题，并优化了 UI 显示的准确性。

### 🚀 核心功能
* **全域弹药检索**：换弹时不再局限于玩家主背包。系统会自动搜索：
    * **宠物背包**（Pet Inventory）中的弹药。
    * **嵌套容器**：存放在背包内各种容器（如弹药箱、挂袋）里的弹药。
* **UI 同步**：通过 Patch 枪械状态机，确保 `BulletCountHUD`（子弹计数器）在各种搜索场景下都能实时、准确地刷新显示数量。

## 📦 安装
- Steam 创意工坊安装：[创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3634058107)
- 本地安装：在[发布页面](https://github.com/Shapooo/YABetterReload/releases)下载压缩包后，将文件解压到 `Duckov_Data/Mods` 文件夹(mac系统的相关文件夹位于 Duckov/Duckov.app/Contents/Mods/)

---

## English Description

**YABetterReload** is an experience enhancement mod for *Escape from Duckov*. It reconstructs the underlying ammo retrieval logic to solve the issue of the original game being unable to identify bullets in complex inventory structures.

### 🚀 Key Features
* **Global Ammo Search**: Reloading is no longer limited to the player's main inventory. The system automatically scans:
    * **Pet Inventories**: Ammo carried by your companions.
    * **Nested Containers**: Ammo stored within rigs, boxes, or other containers inside your backpack.
* **UI Sync**: Patches the firearm state machine to ensure the `BulletCountHUD` (Ammo Counter) updates accurately across all search scenarios.


---

## 📦 Installation
- Install from Steam workshop: [worksop](https://steamcommunity.com/sharedfiles/filedetails/?id=3634058107)
- Install from local: download the release from [Release](https://github.com/Shapooo/YABetterReload/releases), then compress that to `Duckov_Data/Mods` 文件夹(compressed to Duckov/Duckov.app/Contents/Mods/ on Mac)

