﻿using GameNetcodeStuff;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.Items
{
    /// <summary>
    /// <para>Item which allows players holding the item to breath underwater, however their vision will be blocked by the model as it will be placed on their head.</para>
    /// </summary>
    internal class DivingKit : GrabbableObject, IDisplayInfo
    {
        /// <summary>
        /// Local player of Network Manager
        /// </summary>
        private PlayerControllerB localPlayer;
        /// <summary>
        /// Instance which controls the drowning timer of the player
        /// </summary>
        private StartOfRound roundInstance;

        public string GetDisplayInfo()
        {
            string hands = UpgradeBus.instance.cfg.DIVEKIT_TWO_HANDED ? "two" : "one";
            return $"DIVING KIT - ${UpgradeBus.instance.cfg.DIVEKIT_PRICE}\n\n" +
                $"Breath underwater.\n" +
                $"Weights {Mathf.RoundToInt((UpgradeBus.instance.cfg.DIVEKIT_WEIGHT - 1) * 100)} lbs and is {hands} handed.";
        }

        public override void Start()
        {
            base.Start();
            localPlayer = UpgradeBus.instance.GetLocalPlayer();
            roundInstance = StartOfRound.Instance;
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
    }
}
