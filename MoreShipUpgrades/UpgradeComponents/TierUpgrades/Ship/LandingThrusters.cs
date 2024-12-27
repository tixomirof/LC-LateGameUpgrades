﻿using MoreShipUpgrades.Configuration;
using MoreShipUpgrades.Configuration.Custom;
using MoreShipUpgrades.Configuration.Interfaces.TierUpgrades;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UI.TerminalNodes;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades
{
    internal class LandingThrusters : TierUpgrade, IUpgradeWorldBuilding
    {
        internal const string UPGRADE_NAME = "Landing Thrusters";
        internal const string DEFAULT_PRICES = "300,250,450,650";
        internal const string WORLD_BUILDING_TEXT = "\n\nOptimization procedure for your Ship's in-atmosphere thrusters that makes quicker landings possible" +
            " by ordering the autopilot to commit to a longer freefall. Technically more dangerous, but it'll be fine.";

        public string GetWorldBuildingText(bool shareStatus = false)
        {
            return WORLD_BUILDING_TEXT;
        }
        void Awake()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = GetConfiguration().LandingThrustersConfiguration.OverrideName;
        }
        public static float GetInteractMutliplier()
        {
            LandingThrusterUpgradeConfiguration config = GetConfiguration().LandingThrustersConfiguration;
            if (!config.Enabled) return 1f;
            if (!config.AffectLanding) return 1f;
            if (!GetActiveUpgrade(UPGRADE_NAME)) return 1f;
            return 1f + Mathf.Max(0f, (config.InitialEffect + (GetUpgradeLevel(UPGRADE_NAME) * config.IncrementalEffect)) / 100f);
        }
        public static float GetLandingSpeedMultiplier()
        {
            LandingThrusterUpgradeConfiguration config = GetConfiguration().LandingThrustersConfiguration;
            if (!config.Enabled) return 1f;
            if (!GetActiveUpgrade(UPGRADE_NAME)) return 1f;
            return 1f + Mathf.Max(0f, (config.InitialEffect + (GetUpgradeLevel(UPGRADE_NAME) * config.IncrementalEffect)) / 100f);
        }
        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            static float infoFunction(int level)
            {
                LandingThrusterUpgradeConfiguration config = GetConfiguration().LandingThrustersConfiguration;
                return config.InitialEffect.Value + (level * config.IncrementalEffect.Value);
            }
            const string infoFormat = "LVL {0} - ${1} - Increases the ship's landing speed by {2}%\n";
            return Tools.GenerateInfoForUpgrade(infoFormat, initialPrice, incrementalPrices, infoFunction);
        }

        public override bool CanInitializeOnStart
        {
            get
            {
                ITierUpgradeConfiguration upgradeConfig = GetConfiguration().LandingThrustersConfiguration;
                string[] prices = upgradeConfig.Prices.Value.Split(',');
                return prices.Length == 0 || (prices.Length == 1 && (prices[0].Length == 0 || prices[0] == "0"));
            }
        }

        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, GetConfiguration().LandingThrustersConfiguration.ItemProgressionItems.Value.Split(","));
        }
        public new static void RegisterUpgrade()
        {
            SetupGenericPerk<LandingThrusters>(UPGRADE_NAME);
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            return UpgradeBus.Instance.SetupMultiplePurchaseableTerminalNode(UPGRADE_NAME, GetConfiguration().LandingThrustersConfiguration);
        }
    }
}
