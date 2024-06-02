﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.TerminalNodes;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using MoreShipUpgrades.UpgradeComponents.Items.RadarBooster;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades
{
    internal class ChargingBooster : TierUpgrade
    {
        internal const string UPGRADE_NAME = "Charging Booster";
        internal static ChargingBooster Instance { get; private set; }
        internal float chargeCooldown;
        void Awake()
        {
            Instance = this;
        }
        internal override void Start()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_OVERRIDE_NAME;
            chargeCooldown = UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_COOLDOWN.Value;
            base.Start();
        }
        public override void Load()
        {
            base.Load();
            RadarBoosterItem[] radarBoosters = FindObjectsOfType<RadarBoosterItem>();
            for(int i = 0; i < radarBoosters.Length; i++)
            {
                RadarBoosterItem radarBooster = radarBoosters[i];
                if (radarBooster.GetComponent<ChargingStationManager>() != null) continue;
                radarBooster.gameObject.AddComponent<ChargingStationManager>();
            }
        }
        public override void Increment()
        {
            base.Increment();
            chargeCooldown = Mathf.Clamp(chargeCooldown - UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_INCREMENTAL_COOLDOWN_DECREASE.Value, 0f, UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_COOLDOWN.Value);
        }
        [ServerRpc(RequireOwnership = false)]
        internal void UpdateCooldownServerRpc(NetworkBehaviourReference radarBooster)
        {
            UpdateCooldownClientRpc(radarBooster);
        }
        [ClientRpc]
        void UpdateCooldownClientRpc(NetworkBehaviourReference radarBooster)
        {
            radarBooster.TryGet(out RadarBoosterItem radar);
            if (radar == null) return;
            ChargingStationManager chargingStation = radar.GetComponent<ChargingStationManager>();
            if (chargingStation == null) return;
            chargingStation.cooldown = chargeCooldown;
        }
        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            System.Func<int, float> infoFunction = level => UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_COOLDOWN.Value - ((level+1) * UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_INCREMENTAL_COOLDOWN_DECREASE.Value);
            string infoFormat = "LVL {0} - ${1} - Radar boosters will have a recharge cooldown of {2} seconds.\n";
            
            return $"LVL 1 - ${initialPrice} -  Provides charging stations to the radar boosters. After used, goes on cooldown for {UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_COOLDOWN.Value} seconds\n" + Tools.GenerateInfoForUpgrade(infoFormat, 0, incrementalPrices.ToArray(), infoFunction, skipFirst: true);
        }
        public override bool CanInitializeOnStart
        {
            get
            {
                string[] prices = UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_PRICES.Value.Split(',');
                bool free = UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_PRICE.Value <= 0 && prices.Length == 1 && (prices[0] == "" || prices[0] == "0");
                return free;
            }
        }
        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, UpgradeBus.Instance.PluginConfiguration.CHARGING_BOOSTER_ITEM_PROGRESSION_ITEMS.Value.Split(","));
        }
        public new static void RegisterUpgrade()
        {
            SetupGenericPerk<ChargingBooster>(UPGRADE_NAME);
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            LategameConfiguration configuration = UpgradeBus.Instance.PluginConfiguration;

            return UpgradeBus.Instance.SetupMultiplePurchasableTerminalNode(UPGRADE_NAME,
                                                shareStatus: true,
                                                configuration.CHARGING_BOOSTER_ENABLED.Value,
                                                configuration.CHARGING_BOOSTER_PRICE.Value,
                                                UpgradeBus.ParseUpgradePrices(configuration.CHARGING_BOOSTER_PRICES.Value),
                                                configuration.OVERRIDE_UPGRADE_NAMES ? configuration.CHARGING_BOOSTER_OVERRIDE_NAME : "");
        }
    }
}
