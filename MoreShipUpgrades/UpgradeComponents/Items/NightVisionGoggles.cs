﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using MoreShipUpgrades.UpgradeComponents.TierUpgrades;
using System.Linq;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.Items
{
    internal class NightVisionGoggles : LategameItem, IItemWorldBuilding, IDisplayInfo
    {
        internal const string ITEM_NAME = "Night Vision Goggles";
        internal const string WORLD_BUILDING_TEXT = "Very old military surplus phosphor lens modules, retrofitted for compatibility with modern Company-issued helmets" +
            " and offered to employees of The Company on a subscription plan. The base package comes with cheap batteries, but premium subscriptions offer regular issuances" +
            " of higher-quality energy solutions, ranging from hobby grade to industrial application power banks. The modules also come with DRM that prevents the user from improvising" +
            " with other kinds of batteries.";

        protected override bool KeepScanNode
        {
            get
            {
                return true;
            }
        }

        public string GetDisplayInfo()
        {
            string grantStatus = UpgradeBus.Instance.PluginConfiguration.NIGHT_VISION_INDIVIDUAL.Value || !UpgradeBus.Instance.PluginConfiguration.SHARED_UPGRADES.Value ? "one" : "all";
            string loseOnDeath = UpgradeBus.Instance.PluginConfiguration.LOSE_NIGHT_VIS_ON_DEATH.Value ? "be" : "not be";
            return string.Format(AssetBundleHandler.GetInfoFromJSON("Night Vision"), grantStatus, loseOnDeath);
        }

        public string GetWorldBuildingText()
        {
            return WORLD_BUILDING_TEXT;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) return;
            if (BaseUpgrade.GetActiveUpgrade(NightVision.UPGRADE_NAME))
            {
                HUDManager.Instance.chatText.text += "<color=#FF0000>Night vision is already active!</color>";
                return;
            }
            if (UpgradeBus.Instance.terminalNodes.First((x) => x.OriginalName == NightVision.UPGRADE_NAME).SharedUpgrade)
            {
                NightVision.Instance.EnableNightVisionServerRpc();
            }
            else
            {
                UpgradeBus.Instance.UpgradeObjects[NightVision.UPGRADE_NAME].GetComponent<NightVision>().EnableOnClient();
            }
            playerHeldBy.DespawnHeldObject();
        }

        public static new void LoadItem()
        {
            Item nightVisionItem = AssetBundleHandler.GetItemObject("Night Vision");
            if (nightVisionItem == null) return;

            nightVisionItem.creditsWorth = UpgradeBus.Instance.PluginConfiguration.NIGHT_VISION_PRICE.Value;
            nightVisionItem.spawnPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            nightVisionItem.itemId = 492014;
            NightVisionGoggles visScript = nightVisionItem.spawnPrefab.AddComponent<NightVisionGoggles>();
            visScript.itemProperties = nightVisionItem;
            visScript.grabbable = true;
            visScript.useCooldown = 2f;
            visScript.grabbableToEnemies = true;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(nightVisionItem.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add(ITEM_NAME, nightVisionItem);

            ItemManager.SetupStoreItem(nightVisionItem);
        }
    }
}
