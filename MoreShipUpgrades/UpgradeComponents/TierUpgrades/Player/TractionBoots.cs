﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UI.TerminalNodes;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades.Player
{
    internal class TractionBoots : TierUpgrade
    {
        internal const string UPGRADE_NAME = "Traction Boots";
        internal const string PRICES_DEFAULT = "100,150,250";

        internal override void Start()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = GetConfiguration().TRACTION_BOOTS_OVERRIDE_NAME;
            base.Start();
        }

        public static float ComputeAdditionalTractionForce()
        {
            LategameConfiguration config = GetConfiguration();
            return 1f + ((config.TRACTION_BOOTS_INITIAL_INCREASE + (GetUpgradeLevel(UPGRADE_NAME) * config.TRACTION_BOOTS_INCREMENTAL_INCREASE)) / 100f);
        }

        public static float GetAdditionalTractionForce(float defaultValue)
        {
            if (!GetConfiguration().TRACTION_BOOTS_ENABLED) return defaultValue;
            if (!GetActiveUpgrade(UPGRADE_NAME)) return defaultValue;
            float additionalTraction = ComputeAdditionalTractionForce();
            return Mathf.Clamp(additionalTraction * defaultValue, defaultValue, float.MaxValue);
        }

        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            static float infoFunction(int level)
            {
                LategameConfiguration config = GetConfiguration();
                return config.TRACTION_BOOTS_INITIAL_INCREASE.Value + (level * config.TRACTION_BOOTS_INCREMENTAL_INCREASE.Value);
            }
            const string infoFormat = "LVL {0} - ${1} - Increases the player's traction to the ground by {2}%\n";
            return Tools.GenerateInfoForUpgrade(infoFormat, initialPrice, incrementalPrices, infoFunction);
        }
        public override bool CanInitializeOnStart
        {
            get
            {
                LategameConfiguration config = GetConfiguration();
                string[] prices = config.TRACTION_BOOTS_PRICES.Value.Split(',');
                return config.TRACTION_BOOTS_PRICE.Value <= 0 && prices.Length == 1 && (prices[0].Length == 0 || prices[0] == "0");
            }
        }

        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, GetConfiguration().TRACTION_BOOTS_ITEM_PROGRESSION_ITEMS.Value.Split(","));
        }
        public new static void RegisterUpgrade()
        {
            SetupGenericPerk<TractionBoots>(UPGRADE_NAME);
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            LategameConfiguration configuration = GetConfiguration();

            return UpgradeBus.Instance.SetupMultiplePurchasableTerminalNode(UPGRADE_NAME,
                                                configuration.SHARED_UPGRADES.Value || !configuration.TRACTION_BOOTS_INDIVIDUAL.Value,
                                                configuration.TRACTION_BOOTS_ENABLED.Value,
                                                configuration.TRACTION_BOOTS_PRICE.Value,
                                                UpgradeBus.ParseUpgradePrices(configuration.TRACTION_BOOTS_PRICES.Value),
                                                configuration.OVERRIDE_UPGRADE_NAMES ? configuration.TRACTION_BOOTS_OVERRIDE_NAME : "");
        }
    }
}
