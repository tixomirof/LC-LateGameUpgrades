﻿using MoreShipUpgrades.Configuration.Interfaces.TierUpgrades;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UI.TerminalNodes;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades.Items.Shotgun
{
    public class HollowPoint : TierUpgrade, IUpgradeWorldBuilding
    {
        internal const string UPGRADE_NAME = "Hollow Point";
        internal const string DEFAULT_PRICES = "800,1000";
        internal const string WORLD_BUILDING_TEXT = "\n\nBy trading expedition notes and a few ration tickets with another crew during sale, you have acquired a set of instructions on how to make something called 'Cruelty Slugs'. The Company really shouldn't know about these... please keep this to yourselves.\n\n";
        public string GetWorldBuildingText(bool shareStatus = false)
        {
            return WORLD_BUILDING_TEXT;
        }
        internal override void Start()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = GetConfiguration().HollowPointConfiguration.OverrideName;
            base.Start();
        }
        public override bool CanInitializeOnStart
        {
            get
            {
                ITierUpgradeConfiguration upgradeConfig = GetConfiguration().HollowPointConfiguration;
                string[] prices = upgradeConfig.Prices.Value.Split(',');
                return prices.Length == 0 || (prices.Length == 1 && (prices[0].Length == 0 || prices[0] == "0"));
            }
        }
        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            static float infoFunction(int level)
            {
                ITierEffectUpgradeConfiguration<int> config = GetConfiguration().HollowPointConfiguration;
                return config.InitialEffect.Value + (level * config.IncrementalEffect.Value);
            }
            const string infoFormat = "LVL {0} - ${1} - Damage dealt by the shotgun is increased by {2}\n";
            return Tools.GenerateInfoForUpgrade(infoFormat, initialPrice, incrementalPrices, infoFunction);
        }
        public static int ComputeHollowPointDamageBoost()
        {
            ITierEffectUpgradeConfiguration<int> upgradeConfig = GetConfiguration().HollowPointConfiguration;
            return upgradeConfig.InitialEffect + (GetUpgradeLevel(UPGRADE_NAME) * upgradeConfig.IncrementalEffect);
        }
        public static int GetHollowPointDamageBoost(int defaultValue)
        {
            if (!GetConfiguration().HollowPointConfiguration.Enabled) return defaultValue;
            if (!GetActiveUpgrade(UPGRADE_NAME)) return defaultValue;
            int additionalDamage = ComputeHollowPointDamageBoost();
            return defaultValue + additionalDamage;
        }
        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, GetConfiguration().HollowPointConfiguration.ItemProgressionItems.Value.Split(","));
        }
        public new static void RegisterUpgrade()
        {
            GameObject prefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab(UPGRADE_NAME);
            prefab.AddComponent<HollowPoint>();
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(prefab);
            Plugin.networkPrefabs[UPGRADE_NAME] = prefab;
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            return UpgradeBus.Instance.SetupMultiplePurchaseableTerminalNode(UPGRADE_NAME, GetConfiguration().HollowPointConfiguration, Plugin.networkPrefabs[UPGRADE_NAME]);
        }

    }
}