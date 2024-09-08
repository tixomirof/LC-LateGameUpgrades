﻿using GameNetcodeStuff;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.TerminalNodes;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades
{
    public class BackMuscles : TierUpgrade, IUpgradeWorldBuilding
    {
        internal float alteredWeight = 1f;
        internal static BackMuscles Instance;
        public const string UPGRADE_NAME = "Back Muscles";
        public const string PRICES_DEFAULT = "600,700,800";
        internal const string WORLD_BUILDING_TEXT = "\n\nCompany-issued hydraulic girdles which are only awarded to high-performing {0} who can afford to opt in." +
            " Highly valued by all employees of The Company for their combination of miraculous health-preserving benefits and artificial, intentionally-implemented scarcity." +
            " Sardonically called the 'Back Muscles Upgrade' by some. Comes with a user manual, which mostly contains minimalistic ads for girdle maintenance contractors." +
            " Most of the phone numbers don't work anymore.\n\n";

        public enum UpgradeMode
        {
            ReduceWeight,
            ReduceCarryInfluence,
            ReduceCarryStrain,
        }
        public static UpgradeMode CurrentUpgradeMode
        {
            get
            {
                return GetConfiguration().BACK_MUSCLES_UPGRADE_MODE;
            }
        }
        void Awake()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = GetConfiguration().BACK_MUSCLES_OVERRIDE_NAME;
            Instance = this;
        }
        public override void Increment()
        {
            base.Increment();
            UpdatePlayerWeight();
        }

        public override void Load()
        {
            base.Load();
            UpdatePlayerWeight();
        }
        public override void Unwind()
        {
            base.Unwind();
            UpdatePlayerWeight();
        }
        public static float DecreaseStrain(float defaultWeight)
        {
            return DecreaseValue(defaultWeight, GetConfiguration().BACK_MUSCLES_ENABLED, UpgradeMode.ReduceCarryStrain, 1f);
        }

        public static float DecreaseCarryLoss(float defaultWeight)
        {
            return DecreaseValue(defaultWeight, GetConfiguration().BACK_MUSCLES_ENABLED, UpgradeMode.ReduceCarryInfluence, 1f);
        }

        public static float DecreasePossibleWeight(float defaultWeight)
        {
            return DecreaseValue(defaultWeight, GetConfiguration().BACK_MUSCLES_ENABLED, UpgradeMode.ReduceWeight, 0f);
        }

        public static float DecreaseValue(float defaultWeight, bool enabled, UpgradeMode intendedMode, float lowerBound)
        {
            if (!enabled) return defaultWeight;
            if (CurrentUpgradeMode != intendedMode) return defaultWeight;
            if (!GetActiveUpgrade(UPGRADE_NAME)) return defaultWeight;
            LategameConfiguration config = GetConfiguration();
            return Mathf.Max(defaultWeight * (config.CARRY_WEIGHT_REDUCTION.Value - (GetUpgradeLevel(UPGRADE_NAME) * config.CARRY_WEIGHT_INCREMENT.Value)), lowerBound);
        }

        public static void UpdatePlayerWeight()
        {
            if (CurrentUpgradeMode != UpgradeMode.ReduceWeight) return;
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player.ItemSlots.Length == 0) return;

            Instance.alteredWeight = 1f;
            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject obj = player.ItemSlots[i];
                if (obj == null) continue;

                Instance.alteredWeight += Mathf.Clamp(DecreasePossibleWeight(obj.itemProperties.weight - 1f), 0f, 10f);
            }
            player.carryWeight = Instance.alteredWeight;
            if (player.carryWeight < 1f) { player.carryWeight = 1f; }
        }

        public string GetWorldBuildingText(bool shareStatus = false)
        {
            return string.Format(WORLD_BUILDING_TEXT, shareStatus ? "departments" : "employees");
        }

        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            static float infoFunction(int level)
            {
                LategameConfiguration config = GetConfiguration();
                return (config.CARRY_WEIGHT_REDUCTION.Value - (level * config.CARRY_WEIGHT_INCREMENT.Value)) * 100;
            }
            string infoFormat;
            switch (CurrentUpgradeMode)
            {
                case UpgradeMode.ReduceWeight:
                    {
                        infoFormat = AssetBundleHandler.GetInfoFromJSON(UPGRADE_NAME);
                        break;
                    }
                case UpgradeMode.ReduceCarryInfluence:
                    {
                        infoFormat = "LVL {0} - ${1} - Reduces the weight's influence on player's running speed by {2}%\n";
                        break;
                    }
                case UpgradeMode.ReduceCarryStrain:
                    {
                        infoFormat = "LVL {0} - ${1} - Reduces the weight's influence on player's stamina consumption while running by {2}%\n";
                        break;
                    }
                default:
                    {
                        infoFormat = "Undefined";
                        break;
                    }
            }
            return Tools.GenerateInfoForUpgrade(infoFormat, initialPrice, incrementalPrices, infoFunction);
        }
        public override bool CanInitializeOnStart
        {
            get
            {
                LategameConfiguration config = GetConfiguration();
                string[] prices = config.BACK_MUSCLES_UPGRADE_PRICES.Value.Split(',');
                return config.BACK_MUSCLES_PRICE.Value <= 0 && prices.Length == 1 && (prices[0].Length == 0 || prices[0] == "0");
            }
        }

        public new static void RegisterUpgrade()
        {
            SetupGenericPerk<BackMuscles>(UPGRADE_NAME);
        }
        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, GetConfiguration().BACK_MUSCLES_ITEM_PROGRESSION_ITEMS.Value.Split(","));
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            LategameConfiguration configuration = GetConfiguration();

            return UpgradeBus.Instance.SetupMultiplePurchasableTerminalNode(UPGRADE_NAME,
                                                configuration.SHARED_UPGRADES.Value || !configuration.BACK_MUSCLES_INDIVIDUAL.Value,
                                                configuration.BACK_MUSCLES_ENABLED.Value,
                                                configuration.BACK_MUSCLES_PRICE.Value,
                                                UpgradeBus.ParseUpgradePrices(configuration.BACK_MUSCLES_UPGRADE_PRICES.Value),
                                                configuration.OVERRIDE_UPGRADE_NAMES ? configuration.BACK_MUSCLES_OVERRIDE_NAME : "");
        }
    }
}
