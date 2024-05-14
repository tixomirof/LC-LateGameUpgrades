﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using System;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades
{
    internal class QuantumDisruptor : GameAttributeTierUpgrade, IServerSync
    {
        internal const string UPGRADE_NAME = "Quantum Disruptor";
        internal const string PRICES_DEFAULT = "1200,1500,1800";
        void Awake()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_OVERRIDE_NAME;
            logger = new LguLogger(UPGRADE_NAME);
            changingAttribute = GameAttribute.TIME_GLOBAL_TIME_MULTIPLIER;
            initialValue = UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_INITIAL_MULTIPLIER.Value;
            incrementalValue = UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_INCREMENTAL_MULTIPLIER.Value;
        }
        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            Func<int, float> infoFunction = level => (UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_INITIAL_MULTIPLIER.Value + level * UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_INCREMENTAL_MULTIPLIER.Value) * 100f;
            string infoFormat = "LVL {0} - ${1} - Decreases the landed moon's rotation force (time passing) by {2}%\n";
            return Tools.GenerateInfoForUpgrade(infoFormat, initialPrice, incrementalPrices, infoFunction);
        }
        internal override bool CanInitializeOnStart()
        {
            string[] prices = UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_PRICES.Value.Split(',');
            bool free = UpgradeBus.Instance.PluginConfiguration.QUANTUM_DISRUPTOR_PRICE.Value <= 0 && prices.Length == 1 && (prices[0] == "" || prices[0] == "0");
            return free;
        }
        internal new static void RegisterUpgrade()
        {
            SetupGenericPerk<QuantumDisruptor>(UPGRADE_NAME);
        }
        internal new static void RegisterTerminalNode()
        {
            LategameConfiguration configuration = UpgradeBus.Instance.PluginConfiguration;

            UpgradeBus.Instance.SetupMultiplePurchasableTerminalNode(UPGRADE_NAME,
                                                shareStatus: true,
                                                configuration.QUANTUM_DISRUPTOR_ENABLED.Value,
                                                configuration.QUANTUM_DISRUPTOR_PRICE.Value,
                                                UpgradeBus.ParseUpgradePrices(configuration.QUANTUM_DISRUPTOR_PRICES.Value),
                                                configuration.OVERRIDE_UPGRADE_NAMES ? configuration.QUANTUM_DISRUPTOR_OVERRIDE_NAME : "");
        }
    }
}
