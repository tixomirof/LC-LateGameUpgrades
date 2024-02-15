﻿using HarmonyLib;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using UnityEngine;

namespace MoreShipUpgrades.Patches.NetworkManager
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatcher
    {
        private static LguLogger logger = new LguLogger(nameof(GameNetworkManagerPatcher));
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        private static void ResetUpgradeBus()
        {
            logger.LogDebug("Resetting the Upgrade Bus due to disconnecting...");
            BaseUpgrade[] upgradeObjects = Object.FindObjectsOfType<BaseUpgrade>();
            foreach (BaseUpgrade upgrade in upgradeObjects)
            {
                Object.Destroy(upgrade.gameObject);
            }
            UpgradeBus.Instance.ResetAllValues();
        }
    }
}
