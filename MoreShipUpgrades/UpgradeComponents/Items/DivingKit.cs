﻿using GameNetcodeStuff;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.Items
{
    /// <summary>
    /// <para>Item which allows players holding the item to breath underwater, however their vision will be blocked by the model as it will be placed on their head.</para>
    /// </summary>
    internal class DivingKit : LategameItem, IDisplayInfo
    {
        internal const string ITEM_NAME = "Diving Kit";
        /// <summary>
        /// Local player of Network Manager
        /// </summary>
        private PlayerControllerB localPlayer;
        /// <summary>
        /// Instance which controls the drowning timer of the player
        /// </summary>
        private StartOfRound roundInstance;

        bool KeepScanNode
        {
            get
            {
                return UpgradeBus.Instance.PluginConfiguration.DIVING_KIT_SCAN_NODE;
            }
        }

        public string GetDisplayInfo()
        {
            string hands = UpgradeBus.Instance.PluginConfiguration.DIVEKIT_TWO_HANDED.Value ? "two" : "one";
            return $"DIVING KIT - ${UpgradeBus.Instance.PluginConfiguration.DIVEKIT_PRICE.Value}\n\n" +
                $"Breath underwater.\n" +
                $"Weights {Mathf.RoundToInt((UpgradeBus.Instance.PluginConfiguration.DIVEKIT_WEIGHT.Value - 1) * 100)} lbs and is {hands} handed.";
        }

        public override void Start()
        {
            base.Start();
            localPlayer = UpgradeBus.Instance.GetLocalPlayer();
            roundInstance = StartOfRound.Instance;
            if (!KeepScanNode) LguScanNodeProperties.RemoveScanNode(gameObject);
        }
        /// <summary>
        /// Check if this item is currently grabbed by a player and if it's the local player and if so, reset their drown timer.
        /// </summary>
        public override void Update()
        {
            if (isHeld && playerHeldBy == localPlayer)
            {
                roundInstance.drowningTimer = 1f;
            }
            base.Update();
        }

        public static new void LoadItem()
        {
            Item DiveItem = AssetBundleHandler.GetItemObject("Diving Kit");
            if (DiveItem == null) return;

            DiveItem.creditsWorth = UpgradeBus.Instance.PluginConfiguration.DIVEKIT_PRICE.Value;
            DiveItem.itemId = 492015;
            DiveItem.twoHanded = UpgradeBus.Instance.PluginConfiguration.DIVEKIT_TWO_HANDED.Value;
            DiveItem.weight = UpgradeBus.Instance.PluginConfiguration.DIVEKIT_WEIGHT.Value;
            DiveItem.itemSpawnsOnGround = true;
            DivingKit diveScript = DiveItem.spawnPrefab.AddComponent<DivingKit>();
            diveScript.itemProperties = DiveItem;
            diveScript.grabbable = true;
            diveScript.grabbableToEnemies = true;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(DiveItem.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Dive", DiveItem);

            ItemManager.SetupStoreItem(DiveItem);
        }
    }
}
