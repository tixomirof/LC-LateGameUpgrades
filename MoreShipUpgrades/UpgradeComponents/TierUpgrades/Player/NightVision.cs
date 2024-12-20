﻿using GameNetcodeStuff;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using Newtonsoft.Json;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using MoreShipUpgrades.Input;
using UnityEngine.UI;
using MoreShipUpgrades.Misc.Util;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using LethalLib;
using MoreShipUpgrades.UI.TerminalNodes;

namespace MoreShipUpgrades.UpgradeComponents.TierUpgrades.Player
{
    internal class NightVision : TierUpgrade, IPlayerSync, IUpgradeWorldBuilding
    {
        float nightBattery;
        PlayerControllerB client;
        public bool batteryExhaustion;
        internal GameObject nightVisionPrefab;
        internal bool nightVisionActive = false;
        internal float nightVisRange;
        internal float nightVisIntensity;
        internal Color nightVisColor;
        public static NightVision Instance { get; internal set; }

        public const string SIMPLE_UPGRADE_NAME = "Night Vision";
        public const string UPGRADE_NAME = "NV Headset Batteries";
        public const string PRICES_DEFAULT = "300,400,500";
        internal const string WORLD_BUILDING_TEXT = "\n\nService package for your crew's Night Vision Headset that optimizes the function of its capacitor," +
            " leading to improved uptime and shorter recharge period.\n\n";

        private static readonly LguLogger logger = new(UPGRADE_NAME);

        public string GetWorldBuildingText(bool shareStatus = false)
        {
            return WORLD_BUILDING_TEXT;
        }
        void Awake()
        {
            Instance = this;
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = GetConfiguration().NIGHT_VISION_OVERRIDE_NAME;
            nightVisionPrefab = AssetBundleHandler.GetItemObject("Night Vision").spawnPrefab;
        }
        internal override void Start()
        {
            base.Start();
            LategameConfiguration config = GetConfiguration();
            transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Tools.ConvertValueToColor(config.NIGHT_VIS_UI_BAR_COLOR.LocalValue, Color.green);
            transform.GetChild(0).GetChild(1).GetComponent<Text>().color = Tools.ConvertValueToColor(config.NIGHT_VIS_UI_TEXT_COLOR.LocalValue, Color.white);
            transform.GetChild(0).GetChild(2).GetComponent<Image>().color = Tools.ConvertValueToColor(config.NIGHT_VIS_UI_BAR_COLOR.LocalValue, Color.green);
            transform.GetChild(0).gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (client == null) { return; }

            LategameConfiguration config = GetConfiguration();
            float maxBattery = config.NIGHT_BATTERY_MAX.Value + ((GetUpgradeLevel(UPGRADE_NAME) + 1) * config.NIGHT_VIS_BATTERY_INCREMENT.Value);

            if (nightVisionActive)
            {
                nightBattery -= Time.deltaTime * (config.NIGHT_VIS_DRAIN_SPEED.Value - ((GetUpgradeLevel(UPGRADE_NAME) + 1) * config.NIGHT_VIS_DRAIN_INCREMENT.Value));
                nightBattery = Mathf.Clamp(nightBattery, 0f, maxBattery);
                transform.GetChild(0).gameObject.SetActive(true);

                if (nightBattery <= 0f)
                {
                    TurnOff(true);
                }
            }
            else if (!batteryExhaustion)
            {
                nightBattery += Time.deltaTime * (config.NIGHT_VIS_REGEN_SPEED.Value + ((GetUpgradeLevel(UPGRADE_NAME) + 1) * config.NIGHT_VIS_REGEN_INCREMENT.Value));
                nightBattery = Mathf.Clamp(nightBattery, 0f, maxBattery);

                if (nightBattery >= maxBattery)
                {
                    transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    transform.GetChild(0).gameObject.SetActive(true);
                }
            }
            // this ensures the vanilla behaviour for the night vision light remains
            client.nightVision.enabled = client.isInsideFactory || nightVisionActive;

            float scale = nightBattery / maxBattery;
            transform.GetChild(0).GetChild(0).localScale = new Vector3(scale, 1, 1);
        }

        public void Toggle()
        {
            if (!GetActiveUpgrade("Night Vision")) return;
            if (UpgradeBus.Instance.GetLocalPlayer().inTerminalMenu) return;
            nightVisionActive = !nightVisionActive;
            if (client == null) { client = GameNetworkManager.Instance.localPlayerController; }

            if (nightVisionActive)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        private void TurnOff(bool exhaust = false)
        {
            nightVisionActive = false;
            client.nightVision.color = nightVisColor;
            client.nightVision.range = nightVisRange;
            client.nightVision.intensity = nightVisIntensity;
            if (exhaust)
            {
                batteryExhaustion = true;
                StartCoroutine(BatteryRecovery());
            }
        }

        private void TurnOn()
        {
            nightVisColor = client.nightVision.color;
            nightVisRange = client.nightVision.range;
            nightVisIntensity = client.nightVision.intensity;

            LategameConfiguration config = GetConfiguration();
            client.nightVision.color = Tools.ConvertValueToColor(config.NIGHT_VIS_COLOR.LocalValue, Color.green);
            client.nightVision.range = config.NIGHT_VIS_RANGE.Value + (GetUpgradeLevel(UPGRADE_NAME) * config.NIGHT_VIS_RANGE_INCREMENT.Value);
            client.nightVision.intensity = config.NIGHT_VIS_INTENSITY.Value + (GetUpgradeLevel(UPGRADE_NAME) * config.NIGHT_VIS_INTENSITY_INCREMENT.Value);
            nightBattery -= config.NIGHT_VIS_STARTUP.Value; // 0.1f
        }

        private IEnumerator BatteryRecovery()
        {
            yield return new WaitForSeconds(GetConfiguration().NIGHT_VIS_EXHAUST.Value);
            batteryExhaustion = false;
        }

        public override void Increment()
        {
            base.Increment();
            LguStore.Instance.UpdateLGUSaveServerRpc(GameNetworkManager.Instance.localPlayerController.playerSteamId, Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new SaveInfo())));
        }

        public override void Load()
        {
            base.Load();
            if (!GetActiveUpgrade("Night Vision")) return;
            EnableOnClient();
        }
        public override void Unwind()
        {
            base.Unwind();
            if (!GetActiveUpgrade("Night Vision")) return;
            DisableOnClient();
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnableNightVisionServerRpc()
        {
            logger.LogDebug("Enabling night vision for all clients...");
            EnableNightVisionClientRpc();
        }

        [ClientRpc]
        private void EnableNightVisionClientRpc()
        {
            logger.LogDebug("Request to enable night vision on this client received.");
            EnableOnClient();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnNightVisionItemOnDeathServerRpc(Vector3 position)
        {
            GameObject go = Instantiate(nightVisionPrefab, position + Vector3.up, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            logger.LogInfo("Request to spawn night vision goggles received.");
        }
        public void EnableOnClient()
        {
            if (client == null) { client = GameNetworkManager.Instance.localPlayerController; }
            transform.GetChild(0).gameObject.SetActive(true);
            UpgradeBus.Instance.activeUpgrades["Night Vision"] = true;
            LguStore.Instance.UpdateLGUSaveServerRpc(client.playerSteamId, Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new SaveInfo())));
            HUDManager.Instance.chatText.text += $"\n<color=#FF0000>Press {Keybinds.NvgAction.GetBindingDisplayString()} to toggle Night Vision!!!</color>";
        }

        public void DisableOnClient()
        {
            nightVisionActive = false;
            client.nightVision.color = nightVisColor;
            client.nightVision.range = nightVisRange;
            client.nightVision.intensity = nightVisIntensity;

            transform.GetChild(0).gameObject.SetActive(false);
            UpgradeBus.Instance.activeUpgrades["Night Vision"] = false;
            LguStore.Instance.UpdateLGUSaveServerRpc(client.playerSteamId, Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new SaveInfo())));
            client = null;
        }

        public static string GetNightVisionInfo(int level, int price)
        {
            LategameConfiguration config = GetConfiguration();
            float regenAdjustment = Mathf.Clamp(config.NIGHT_VIS_REGEN_SPEED.Value + (config.NIGHT_VIS_REGEN_INCREMENT.Value * level), 0, 1000);
            float drainAdjustment = Mathf.Clamp(config.NIGHT_VIS_DRAIN_SPEED.Value - (config.NIGHT_VIS_DRAIN_INCREMENT.Value * level), 0, 1000);
            float batteryLife = config.NIGHT_BATTERY_MAX.Value + (config.NIGHT_VIS_BATTERY_INCREMENT.Value * level);

            string drainTime = "infinite";
            if (drainAdjustment != 0) drainTime = ((batteryLife - (batteryLife * config.NIGHT_VIS_STARTUP.Value)) / drainAdjustment).ToString("F2");

            string regenTime = "infinite";
            if (regenAdjustment != 0) regenTime = (batteryLife / regenAdjustment).ToString("F2");

            return string.Format(AssetBundleHandler.GetInfoFromJSON(UPGRADE_NAME), level, price, drainTime, regenTime);
        }

        public override string GetDisplayInfo(int initialPrice = -1, int maxLevels = -1, int[] incrementalPrices = null)
        {
            StringBuilder stringBuilder = new();
            LategameConfiguration config = GetConfiguration();
            float drain = (config.NIGHT_BATTERY_MAX.Value - (config.NIGHT_BATTERY_MAX.Value * config.NIGHT_VIS_STARTUP.Value)) / config.NIGHT_VIS_DRAIN_SPEED.Value;
            float regen = config.NIGHT_BATTERY_MAX.Value / config.NIGHT_VIS_REGEN_SPEED.Value;
            stringBuilder.Append($"The affected item (Night vision Googles) has a base drain time to empty of {drain} seconds and regeneration time to full of {regen} seconds.\n\n");
            stringBuilder.Append(GetNightVisionInfo(1, initialPrice));
            for (int i = 0; i < maxLevels; i++)
                stringBuilder.Append(GetNightVisionInfo(i + 2, incrementalPrices[i]));
            return stringBuilder.ToString();
        }
        public override bool CanInitializeOnStart
        {
            get
            {
                LategameConfiguration config = GetConfiguration();
                string[] prices = config.NIGHT_VISION_UPGRADE_PRICES.Value.Split(',');
                return config.NIGHT_VISION_PRICE.Value <= 0 && prices.Length == 1 && (prices[0].Length == 0 || prices[0] == "0");
            }
        }

        public new static (string, string[]) RegisterScrapToUpgrade()
        {
            return (UPGRADE_NAME, GetConfiguration().NIGHT_VISION_ITEM_PROGRESSION_ITEMS.Value.Split(","));
        }
        public new static void RegisterUpgrade()
        {
            SetupGenericPerk<NightVision>(UPGRADE_NAME);
        }
        public new static CustomTerminalNode RegisterTerminalNode()
        {
            LategameConfiguration configuration = GetConfiguration();
            int[] prices = UpgradeBus.ParseUpgradePrices(configuration.NIGHT_VISION_UPGRADE_PRICES.Value);

            return UpgradeBus.Instance.SetupMultiplePurchasableTerminalNode(UPGRADE_NAME,
                                                configuration.SHARED_UPGRADES.Value || !configuration.NIGHT_VISION_INDIVIDUAL.Value,
                                                configuration.NIGHT_VISION_ENABLED.Value,
                                                prices.Length > 0 ? prices[0] : 0,
                                                prices.Length > 1 ? prices[1..] : [],
                                                configuration.OVERRIDE_UPGRADE_NAMES ? configuration.NIGHT_VISION_OVERRIDE_NAME : "");
        }

        public void ResetPlayerAttribute()
        {
            if (client == null) return;
            DisableOnClient();
        }
    }
}