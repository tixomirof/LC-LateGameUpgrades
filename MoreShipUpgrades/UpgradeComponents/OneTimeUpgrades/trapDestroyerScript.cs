﻿using MoreShipUpgrades.Managers;
using MoreShipUpgrades.Misc;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace MoreShipUpgrades.UpgradeComponents.OneTimeUpgrades
{
    public class trapDestroyerScript : BaseUpgrade, IOneTimeUpgradeDisplayInfo
    {
        public static string UPGRADE_NAME = "Malware Broadcaster";
        void Start()
        {
            upgradeName = UPGRADE_NAME;
            DontDestroyOnLoad(gameObject);
            Register();
        }

        public override void load()
        {
            base.load();

            UpgradeBus.instance.DestroyTraps = true;
            UpgradeBus.instance.trapHandler = this;
        }

        public override void Unwind()
        {
            base.Unwind();

            UpgradeBus.instance.DestroyTraps = false;
        }
        public override void Register()
        {
            base.Register();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReqDestroyObjectServerRpc(NetworkObjectReference go)
        {
            go.TryGet(out NetworkObject netObj);
            if (netObj == null)
            {
                HUDManager.Instance.AddTextToChatOnServer("Can't retrieve obj", 0);
                return;
            }
            if (netObj.gameObject.name == "Landmine(Clone)" || netObj.gameObject.name == "TurretContainer(Clone)")
            {
                if (UpgradeBus.instance.cfg.EXPLODE_TRAP) { SpawnExplosionClientRpc(netObj.gameObject.transform.position); }
                Destroy(netObj.gameObject);
            }
        }

        [ClientRpc]
        private void SpawnExplosionClientRpc(Vector3 position)
        {
            if (UpgradeBus.instance.cfg.EXPLODE_TRAP) { Landmine.SpawnExplosion(position + Vector3.up, true, 5.7f, 6.4f); }
        }

        public string GetDisplayInfo(int price = -1)
        {
            string desc;
            if (UpgradeBus.instance.cfg.DESTROY_TRAP)
            {
                if (UpgradeBus.instance.cfg.EXPLODE_TRAP)
                {
                    desc = "Broadcasted codes now explode map hazards.";
                }
                else
                {
                    desc = "Broadcasted codes now destroy map hazards.";
                }
            }
            else { desc = $"Broadcasted codes now disable map hazards for {UpgradeBus.instance.cfg.DISARM_TIME} seconds."; }
            return desc;
        }
    }
}
