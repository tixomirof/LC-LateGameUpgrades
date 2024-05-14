﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades;
using UnityEngine;
using UnityEngine.UI;

namespace MoreShipUpgrades.UpgradeComponents.OneTimeUpgrades
{
    class WalkieGPS : OneTimeUpgrade
    {
        public const string UPGRADE_NAME = "Walkie GPS";
        public static WalkieGPS instance;
        bool walkieUIActive;

        private GameObject canvas;
        private Text x, y, z, time;
        void Awake()
        {
            upgradeName = UPGRADE_NAME;
            overridenUpgradeName = UpgradeBus.Instance.PluginConfiguration.WALKIE_GPS_OVERRIDE_NAME;
            instance = this;
        }
        internal override void Start()
        {
            base.Start();
            canvas = transform.GetChild(0).gameObject;
            x = canvas.transform.GetChild(0).GetComponent<Text>();
            y = canvas.transform.GetChild(1).GetComponent<Text>();
            z = canvas.transform.GetChild(2).GetComponent<Text>();
            time = canvas.transform.GetChild(3).GetComponent<Text>();
        }
        public void Update()
        {
            if (!walkieUIActive) return;

            Vector3 pos = GameNetworkManager.Instance.localPlayerController.transform.position;
            x.text = $"X: {pos.x.ToString("F1")}";
            y.text = $"Y: {pos.y.ToString("F1")}";
            z.text = $"Z: {pos.z.ToString("F1")}";

            int num = (int)(TimeOfDay.Instance.normalizedTimeOfDay * (60f * TimeOfDay.Instance.numberOfHours)) + 360;
            int num2 = (int)Mathf.Floor(num / 60f);
            string amPM = "AM";
            if (num2 > 12)
            {
                amPM = "PM";
            }
            if (num2 > 12)
            {
                num2 %= 12;
            }
            int num3 = num % 60;
            string text = string.Format("{0:00}:{1:00}", num2, num3).TrimStart('0') + amPM;
            time.text = text;
        }

        public void WalkieActive()
        {
            if (canvas.activeInHierarchy) return;

            walkieUIActive = true;
            canvas.SetActive(true);
        }

        public void WalkieDeactivate()
        {
            walkieUIActive = false;
            canvas.SetActive(false);
        }

        public override string GetDisplayInfo(int price = -1)
        {
            return $"${price} - Displays your location and time when holding a walkie talkie.\nEspecially useful for fog.";
        }
        internal override bool CanInitializeOnStart()
        {
            return UpgradeBus.Instance.PluginConfiguration.WALKIE_PRICE.Value <= 0;
        }
        internal new static void RegisterUpgrade()
        {
            SetupGenericPerk<WalkieGPS>(UPGRADE_NAME);
        }
        internal new static void RegisterTerminalNode()
        {
            LategameConfiguration configuration = UpgradeBus.Instance.PluginConfiguration;

            UpgradeBus.Instance.SetupOneTimeTerminalNode(UPGRADE_NAME,
                                    shareStatus: true,
                                    configuration.WALKIE_ENABLED.Value,
                                    configuration.WALKIE_PRICE.Value,
                                    configuration.OVERRIDE_UPGRADE_NAMES ? configuration.WALKIE_GPS_OVERRIDE_NAME : "");
        }
    }
}
