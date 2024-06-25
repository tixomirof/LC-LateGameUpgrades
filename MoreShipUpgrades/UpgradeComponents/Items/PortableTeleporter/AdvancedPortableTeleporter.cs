﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.UpgradeComponents.Interfaces;

namespace MoreShipUpgrades.UpgradeComponents.Items.PortableTeleporter
{
    internal class AdvancedPortableTeleporter : BasePortableTeleporter, IDisplayInfo, IItemWorldBuilding
    {
        internal const string ITEM_NAME = "Advanced Portable Teleporter";

        internal const string WORLD_BUILDING_TEXT = "A newer, ostensibly-safer Teleportation Remote made by PortaCorp. PortaCorp was a subsidiary of Shipping Solutions Interplanetary," +
            " which was acquired by The Company in 2420, along with their entire stock of PortaCorp Teleportation Remotes. Looking to distance itself from the many safety scandals incurred" +
            " by earlier adopters of the 'Remote Teleportation' technology, PortaCorp marketed its line of Teleportation Remotes with claims of their comparative ruggedness and reliability.";
        bool KeepScanNode
        {
            get
            {
                return UpgradeBus.Instance.PluginConfiguration.STORE_WHEELBARROW_SCAN_NODE;
            }
        }
        public override void Start()
        {
            base.Start();
            breakChance = UpgradeBus.Instance.PluginConfiguration.ADV_CHANCE_TO_BREAK.Value;
            keepItems = UpgradeBus.Instance.PluginConfiguration.ADV_KEEP_ITEMS_ON_TELE.Value;
            if (!KeepScanNode) LguScanNodeProperties.RemoveScanNode(gameObject);
        }
        public string GetDisplayInfo()
        {
            return string.Format(AssetBundleHandler.GetInfoFromJSON("Advanced Portable Tele"), (int)(UpgradeBus.Instance.PluginConfiguration.ADV_CHANCE_TO_BREAK.Value * 100));
        }

        public string GetWorldBuildingText()
        {
            return WORLD_BUILDING_TEXT;
        }
    }
}
