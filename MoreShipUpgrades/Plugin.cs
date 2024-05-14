﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using MoreShipUpgrades.Managers;
using System.IO;
using System.Reflection;
using MoreShipUpgrades.Misc;
using Newtonsoft.Json;
using LethalLib.Extras;
using MoreShipUpgrades.UpgradeComponents.Items;
using MoreShipUpgrades.UpgradeComponents.Items.PortableTeleporter;
using MoreShipUpgrades.UpgradeComponents.Items.Wheelbarrow;
using LethalLib.Modules;
using MoreShipUpgrades.UpgradeComponents.Contracts;
using MoreShipUpgrades.UpgradeComponents.Items.Contracts.Extraction;
using MoreShipUpgrades.UpgradeComponents.Items.Contracts.Exorcism;
using MoreShipUpgrades.UpgradeComponents.Items.Contracts.Exterminator;
using MoreShipUpgrades.UpgradeComponents.Items.Contracts.DataRetrieval;
using MoreShipUpgrades.UpgradeComponents.Items.Contracts.BombDefusal;
using MoreShipUpgrades.UpgradeComponents.Interfaces;
using System.Linq;
using MoreShipUpgrades.Compat;
using MoreShipUpgrades.Patches.Enemies;
using MoreShipUpgrades.Patches.HUD;
using MoreShipUpgrades.Patches.Interactables;
using MoreShipUpgrades.Patches.Items;
using MoreShipUpgrades.Patches.NetworkManager;
using MoreShipUpgrades.Patches.PlayerController;
using MoreShipUpgrades.Patches.RoundComponents;
using MoreShipUpgrades.Patches.TerminalComponents;
using MoreShipUpgrades.Input;
using MoreShipUpgrades.Misc.Upgrades;
using MoreShipUpgrades.Misc.Commands;
using MoreShipUpgrades.Misc.UI.Application;
using InteractiveTerminalAPI.UI;

namespace MoreShipUpgrades
{
    [BepInEx.BepInPlugin(Metadata.GUID,Metadata.NAME,Metadata.VERSION)]
    [BepInDependency("evaisa.lethallib","0.13.0")]
    [BepInDependency("com.sigurd.csync")]
    [BepInDependency("com.rune580.LethalCompanyInputUtils")]
    [BepInDependency("WhiteSpike.InteractiveTerminalAPI")]
    public class Plugin : BaseUnityPlugin
    {
        internal static readonly Harmony harmony = new(Metadata.GUID);
        internal static readonly ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(Metadata.NAME);
        AudioClip itemBreak, buttonPressed, error;
        AudioClip[] wheelbarrowSound, shoppingCartSound;

        public new static LategameConfiguration Config;

        void Awake()
        {
            Config = new LategameConfiguration(base.Config);

            // netcode patching stuff
            IEnumerable<Type> types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null);
            }

            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shipupgrades");
            AssetBundle UpgradeAssets = AssetBundle.LoadFromFile(assetDir);

            GameObject gameObject = new GameObject("UpgradeBus");
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.AddComponent<UpgradeBus>();
            gameObject = new GameObject("SpawnItemManager");
            gameObject.AddComponent<SpawnItemManager>();

            UpgradeBus.Instance.UpgradeAssets = UpgradeAssets;
            SetupModStore(ref UpgradeAssets);

            SetupItems();
            SetupCommands(ref types);
            SetupPerks(ref types);

            SetupContractMapObjects(ref UpgradeAssets);

            InputUtils_Compat.Init();
            PatchMainVersion();
            InteractiveTerminalManager.RegisterApplication<UpgradeStoreApplication>(["lgu", "lategame store"]);
            InteractiveTerminalManager.RegisterApplication<WeatherProbeApplication>("probe");
            InteractiveTerminalManager.RegisterApplication<ExtendDeadlineApplication>("extend deadline");

            mls.LogInfo($"{Metadata.NAME} {Metadata.VERSION} has been loaded successfully.");
        }

        internal static void TryPatchBetaVersion() 
        {
            UpgradeBus.Instance.IsBeta = false;
        }
        internal static void PatchMainVersion()
        {
            TryPatchBetaVersion();
            PatchEnemies();
            PatchHUD();
            PatchInteractables();
            PatchItems();
            PatchVitalComponents();
            PatchWeather();
        }
        static void PatchEnemies()
        {
            try
            {
                harmony.PatchAll(typeof(BaboonBirdAIPatcher));
                harmony.PatchAll(typeof(ButlerBeesPatcher));
                harmony.PatchAll(typeof(EnemyAIPatcher));
                harmony.PatchAll(typeof(EnemyAICollisionDetectPatcher));
                harmony.PatchAll(typeof(HoarderBugAIPatcher));
                harmony.PatchAll(typeof(RedLocustBeesPatch));
                harmony.PatchAll(typeof(SpringManAIPatcher));
                mls.LogInfo("Enemies have been patched");
            } catch (Exception exception)
            {
                mls.LogError("An error has occurred patching enemies...");
                mls.LogError(exception);
            }
        }

        static void PatchHUD()
        {
            try
            {
                harmony.PatchAll(typeof(HudManagerPatcher));
                mls.LogInfo("HUD has been patched");
            }
            catch (Exception exception)
            {
                mls.LogError("An error has occurred patching HUD...");
                mls.LogError(exception);
            }
        }

        static void PatchInteractables()
        {
            try
            {
                harmony.PatchAll(typeof(DoorLockPatcher));
                harmony.PatchAll(typeof(InteractTriggerPatcher));
                harmony.PatchAll(typeof(StartMatchLevelPatcher));
                harmony.PatchAll(typeof(SteamValveHazardPatch));
                mls.LogInfo("Interactables have been patched");
            }
            catch (Exception exception)
            {
                mls.LogError("An error has occurred patching interactables...");
                mls.LogError(exception);
            }
        }

        static void PatchItems()
        {
            try
            {
                harmony.PatchAll(typeof(BoomBoxPatcher));
                harmony.PatchAll(typeof(DropPodPatcher));
                harmony.PatchAll(typeof(GrabbableObjectPatcher));
                harmony.PatchAll(typeof(KnifePatcher));
                harmony.PatchAll(typeof(PatchToolPatcher));
                harmony.PatchAll(typeof(RadarBoosterPatcher));
                harmony.PatchAll(typeof(ShovelPatcher));
                harmony.PatchAll(typeof(WalkiePatcher));
                mls.LogInfo("Items have been patched");
            }
            catch (Exception exception)
            {
                mls.LogError("An error has occurred patching items...");
                mls.LogError(exception);
            }
        }
        static void PatchVitalComponents()
        {
            try
            {
            harmony.PatchAll(typeof(GameNetworkManagerPatcher));
            harmony.PatchAll(typeof(PlayerControllerBPatcher));
            harmony.PatchAll(typeof(RoundManagerPatcher));
            harmony.PatchAll(typeof(RoundManagerTranspilerPatcher));
            harmony.PatchAll(typeof(StartOfRoundPatcher));
            harmony.PatchAll(typeof(TimeOfDayPatcher));
            harmony.PatchAll(typeof(TimeOfDayTranspilerPatcher));
            harmony.PatchAll(typeof(TerminalAccessibleObjectPatcher));
            harmony.PatchAll(typeof(TerminalPatcher));
            harmony.PatchAll(typeof(Keybinds));
            mls.LogInfo("Game managers have been patched");
            }
            catch (Exception exception)
            {
                mls.LogError("An error has occurred patching the game managers...");
                mls.LogError(exception);
            }
        }
        static void PatchWeather()
        {
            try
            {
                harmony.PatchAll(typeof(StormyWeather));
                mls.LogInfo("Weathers have been patched");
            }
            catch (Exception exception)
            {
                mls.LogError("An error has occurred patching weathers...");
                mls.LogError(exception);
            }
        }

        private void SetupContractMapObjects(ref AssetBundle bundle)
        {
            AnimationCurve curve = new AnimationCurve(new Keyframe(0,1), new Keyframe(1,1)); // always spawn 1

            SetupScavContract(ref bundle, curve);
            SetupExterminatorContract(curve);
            SetupDataContract(curve);
            SetupExorcismContract(curve);
            SetupBombContract(curve);
        }

        void SetupBombContract(AnimationCurve curve)
        {
            Item bomb = AssetBundleHandler.GetItemObject("Bomb");
            bomb.spawnPrefab.AddComponent<ScrapValueSyncer>();
            bomb.isConductiveMetal = false;
            DefusalContract coNest = bomb.spawnPrefab.AddComponent<DefusalContract>();
            coNest.SetPosition = true;

            BombDefusalScript bombScript = bomb.spawnPrefab.AddComponent<BombDefusalScript>();
            bombScript.snip = AssetBundleHandler.GetAudioClip("Bomb Cut");
            bombScript.tick = AssetBundleHandler.GetAudioClip("Bomb Tick");

            RegisterSpawnableContractObject(bomb, curve);
        }

        const int MAXIMUM_RITUAL_ITEMS = 5;
        void SetupExorcismContract(AnimationCurve curve)
        {
            Item contractLoot = AssetBundleHandler.GetItemObject("Demon Tome");
            contractLoot.spawnPrefab.AddComponent<ScrapValueSyncer>();
            Items.RegisterItem(contractLoot);
            Utilities.FixMixerGroups(contractLoot.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(contractLoot.spawnPrefab);

            Item mainItem = AssetBundleHandler.GetItemObject("Pentagram");

            for (int i = 0; i < MAXIMUM_RITUAL_ITEMS; i++)
            {
                Item exorItem = AssetBundleHandler.GetItemObject("RitualItem" + i);
                exorItem.spawnPrefab.AddComponent<ExorcismContract>();
                RegisterSpawnableContractObject(exorItem, new AnimationCurve(new Keyframe(0, 3), new Keyframe(1, 3)));
            }

            ExorcismContract co = mainItem.spawnPrefab.AddComponent<ExorcismContract>();
            co.SetPosition = true;

            PentagramScript pentScript = mainItem.spawnPrefab.AddComponent<PentagramScript>();
            pentScript.loot = contractLoot.spawnPrefab;
            pentScript.chant = AssetBundleHandler.GetAudioClip("Ritual Fail");
            pentScript.portal = AssetBundleHandler.GetAudioClip("Ritual Success");

            RegisterSpawnableContractObject(mainItem, curve);
        }

        void RegisterSpawnableContractObject(Item item, AnimationCurve curve)
        {
            Utilities.FixMixerGroups(item.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Items.RegisterItem(item);

            SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
            mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
            mapObjDefBug.spawnableMapObject.prefabToSpawn = item.spawnPrefab;
            MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => curve);
        }


        void SetupExterminatorContract(AnimationCurve curve)
        {
            Item bugLoot = AssetBundleHandler.GetItemObject("HoardingBugEggsLoot");
            bugLoot.spawnPrefab.AddComponent<ScrapValueSyncer>();
            Items.RegisterItem(bugLoot);
            Utilities.FixMixerGroups(bugLoot.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(bugLoot.spawnPrefab);

            Item nest = AssetBundleHandler.GetItemObject("HoardingBugEggs");

            ExterminatorContract coNest = nest.spawnPrefab.AddComponent<ExterminatorContract>();
            coNest.SetPosition = true;

            BugNestScript nestScript = nest.spawnPrefab.AddComponent<BugNestScript>();
            nestScript.loot = bugLoot.spawnPrefab;

            RegisterSpawnableContractObject(nest, curve);
        }

        void SetupScavContract(ref AssetBundle bundle, AnimationCurve curve)
        {
            Item scav = AssetBundleHandler.GetItemObject("Scavenger");
            if (scav == null) return;

            scav.weight = UpgradeBus.Instance.PluginConfiguration.CONTRACT_EXTRACT_WEIGHT.Value;
            ExtractionContract co = scav.spawnPrefab.AddComponent<ExtractionContract>();
            co.SetPosition = true;

            ExtractPlayerScript extractScript = scav.spawnPrefab.AddComponent<ExtractPlayerScript>();
            scav.spawnPrefab.AddComponent<ScrapValueSyncer>();
            TextAsset scavAudioPaths = AssetBundleHandler.GetGenericAsset<TextAsset>("Scavenger Sounds");
            Dictionary<string, string[]> scavAudioDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(scavAudioPaths.text);
            ExtractPlayerScript.clipDict.Add("lost", CreateAudioClipArray(scavAudioDict["lost"], ref bundle));
            ExtractPlayerScript.clipDict.Add("heal", CreateAudioClipArray(scavAudioDict["heal"], ref bundle));
            ExtractPlayerScript.clipDict.Add("safe", CreateAudioClipArray(scavAudioDict["safe"], ref bundle));
            ExtractPlayerScript.clipDict.Add("held", CreateAudioClipArray(scavAudioDict["held"], ref bundle));

            RegisterSpawnableContractObject(scav, curve);
        }

        void SetupDataContract(AnimationCurve curve)
        {
            Item dataLoot = AssetBundleHandler.GetItemObject("Floppy Disk");
            dataLoot.spawnPrefab.AddComponent<ScrapValueSyncer>();
            Items.RegisterItem(dataLoot);
            Utilities.FixMixerGroups(dataLoot.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(dataLoot.spawnPrefab);

            Item pc = AssetBundleHandler.GetItemObject("Laptop");

            DataRetrievalContract coPC = pc.spawnPrefab.AddComponent<DataRetrievalContract>();
            coPC.SetPosition = true;

            DataPCScript dataScript = pc.spawnPrefab.AddComponent<DataPCScript>();
            dataScript.error = AssetBundleHandler.GetAudioClip("Laptop Error");
            dataScript.startup = AssetBundleHandler.GetAudioClip("Laptop Start");
            dataScript.loot = dataLoot.spawnPrefab;

            RegisterSpawnableContractObject(pc, curve);
        }

        private AudioClip[] CreateAudioClipArray(string[] paths, ref AssetBundle bundle)
        {
            AudioClip[] clips = new AudioClip[paths.Length];
            for(int i = 0; i < paths.Length; i++)
            {
                clips[i] = AssetBundleHandler.TryLoadAudioClipAsset(ref bundle, paths[i]);
            }
            return clips;
        }

        private void SetupModStore(ref AssetBundle bundle)
        {
            GameObject modStore = AssetBundleHandler.TryLoadGameObjectAsset(ref bundle, "Assets/ShipUpgrades/LguStore.prefab");
            if (modStore == null) return;

            modStore.AddComponent<ContractManager>();
            modStore.AddComponent<LguStore>();
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(modStore);
            UpgradeBus.Instance.modStorePrefab = modStore;
        }
        private void SetupItems()
        {
            SetupTeleporterButtons();
            SetupNightVision();
            SetupMedkit();
            SetupPeeper();
            SetupSamples();
            SetupHelmet();
            SetupDivingKit();
            SetupWheelbarrows();
            mls.LogInfo("Items have been setup");
        }
        private void SetupSamples()
        {
            Dictionary<string, int> MINIMUM_VALUES = new Dictionary<string, int>()
            {
                { "centipede", UpgradeBus.Instance.PluginConfiguration.SNARE_FLEA_SAMPLE_MINIMUM_VALUE.Value },
                { "bunker spider", UpgradeBus.Instance.PluginConfiguration.BUNKER_SPIDER_SAMPLE_MINIMUM_VALUE.Value },
                { "hoarding bug", UpgradeBus.Instance.PluginConfiguration.HOARDING_BUG_SAMPLE_MINIMUM_VALUE.Value },
                { "flowerman", UpgradeBus.Instance.PluginConfiguration.BRACKEN_SAMPLE_MINIMUM_VALUE.Value },
                { "mouthdog", UpgradeBus.Instance.PluginConfiguration.EYELESS_DOG_SAMPLE_MINIMUM_VALUE.Value },
                { "baboon hawk", UpgradeBus.Instance.PluginConfiguration.BABOON_HAWK_SAMPLE_MINIMUM_VALUE.Value },
                { "crawler", UpgradeBus.Instance.PluginConfiguration.THUMPER_SAMPLE_MINIMUM_VALUE.Value },
                { "forestgiant", UpgradeBus.Instance.PluginConfiguration.FOREST_KEEPER_SAMPLE_MINIMUM_VALUE.Value },
                { "manticoil", UpgradeBus.Instance.PluginConfiguration.MANTICOIL_SAMPLE_MINIMUM_VALUE.Value },
                { "tulip snake", UpgradeBus.Instance.PluginConfiguration.TULIP_SNAKE_SAMPLE_MINIMUM_VALUE.Value },
            };
            Dictionary<string, int> MAXIMUM_VALUES = new Dictionary<string, int>()
            {
                { "centipede", UpgradeBus.Instance.PluginConfiguration.SNARE_FLEA_SAMPLE_MAXIMUM_VALUE.Value },
                { "bunker spider", UpgradeBus.Instance.PluginConfiguration.BUNKER_SPIDER_SAMPLE_MAXIMUM_VALUE.Value },
                { "hoarding bug", UpgradeBus.Instance.PluginConfiguration.HOARDING_BUG_SAMPLE_MAXIMUM_VALUE.Value },
                { "flowerman", UpgradeBus.Instance.PluginConfiguration.BRACKEN_SAMPLE_MAXIMUM_VALUE.Value },
                { "mouthdog", UpgradeBus.Instance.PluginConfiguration.EYELESS_DOG_SAMPLE_MAXIMUM_VALUE.Value },
                { "baboon hawk", UpgradeBus.Instance.PluginConfiguration.BABOON_HAWK_SAMPLE_MAXIMUM_VALUE.Value },
                { "crawler", UpgradeBus.Instance.PluginConfiguration.THUMPER_SAMPLE_MAXIMUM_VALUE.Value },
                { "forestgiant", UpgradeBus.Instance.PluginConfiguration.FOREST_KEEPER_SAMPLE_MAXIMUM_VALUE.Value },
                { "manticoil", UpgradeBus.Instance.PluginConfiguration.MANTICOIL_SAMPLE_MAXIMUM_VALUE.Value },
                { "tulip snake", UpgradeBus.Instance.PluginConfiguration.TULIP_SNAKE_SAMPLE_MAXIMUM_VALUE.Value },
            };
            foreach (string creatureName in AssetBundleHandler.samplePaths.Keys)
            {
                Item sample = AssetBundleHandler.GetItemObject(creatureName);
                sample.minValue = MINIMUM_VALUES[creatureName];
                sample.maxValue = MAXIMUM_VALUES[creatureName];
                API.HunterSamples.RegisterSampleItem(sample, creatureName, registerNetworkPrefab: true, grabbableToEnemies: true);
            }

        }
        private void SetupTeleporterButtons()
        {
            // Teleporter Button SFX
            itemBreak = AssetBundleHandler.GetAudioClip("Break");
            error = AssetBundleHandler.GetAudioClip("Error");
            buttonPressed = AssetBundleHandler.GetAudioClip("Button Press");

            if (itemBreak == null || error == null || buttonPressed == null) return;

            SetupRegularTeleporterButton();
            SetupAdvancedTeleporterButton();
        }

        private void SetupHelmet()
        {
            Item helmet = AssetBundleHandler.GetItemObject("HelmetItem");
            UpgradeBus.Instance.helmetModel = AssetBundleHandler.GetPerkGameObject("HelmetModel");
            if (helmet == null) return;

            UpgradeBus.Instance.SFX.Add("helmet",AssetBundleHandler.GetAudioClip("HelmetHit"));
            UpgradeBus.Instance.SFX.Add("breakWood",AssetBundleHandler.GetAudioClip("breakWood"));

            Helmet helmScript = helmet.spawnPrefab.AddComponent<Helmet>();
            helmScript.itemProperties = helmet;
            helmScript.grabbable = true;
            helmScript.grabbableToEnemies = true;
            helmet.creditsWorth = UpgradeBus.Instance.PluginConfiguration.HELMET_PRICE.Value;
            helmet.positionOffset = new Vector3(-0.25f, 0f, 0f);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(helmet.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Helmet", helmet);
            SetupStoreItem(helmet);
        }
        private TerminalNode SetupInfoNode(Item storeItem)
        {
            TerminalNode infoNode = ScriptableObject.CreateInstance<TerminalNode>();
            GrabbableObject grabbableObject = storeItem.spawnPrefab.GetComponent<GrabbableObject>();
            if (grabbableObject is IDisplayInfo displayInfo) infoNode.displayText += displayInfo.GetDisplayInfo() + "\n";
            if (grabbableObject is IItemWorldBuilding worldBuilding) infoNode.displayText += worldBuilding.GetWorldBuildingText() + "\n";
            infoNode.clearPreviousText = true;
            return infoNode;
        }
        private void SetupStoreItem(Item storeItem)
        {
            TerminalNode infoNode = SetupInfoNode(storeItem);
            Items.RegisterShopItem(shopItem: storeItem, itemInfo: infoNode, price: storeItem.creditsWorth);
        }
        private void SetupRegularTeleporterButton()
        {
            Item regularPortableTeleporter = AssetBundleHandler.GetItemObject("Portable Tele");
            if (regularPortableTeleporter == null) return;

            regularPortableTeleporter.itemName = "Portable Tele";
            regularPortableTeleporter.itemId = 492012;
            RegularPortableTeleporter regularTeleportScript = regularPortableTeleporter.spawnPrefab.AddComponent<RegularPortableTeleporter>();
            regularTeleportScript.itemProperties = regularPortableTeleporter;
            regularTeleportScript.grabbable = true;
            regularTeleportScript.grabbableToEnemies = true;
            regularTeleportScript.ItemBreak = itemBreak;
            regularTeleportScript.useCooldown = 2f;
            regularTeleportScript.error = error;
            regularTeleportScript.buttonPress = buttonPressed;
            regularPortableTeleporter.creditsWorth = UpgradeBus.Instance.PluginConfiguration.WEAK_TELE_PRICE.Value;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(regularPortableTeleporter.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Tele", regularPortableTeleporter);

            SetupStoreItem(regularPortableTeleporter);
        }
        private void SetupAdvancedTeleporterButton()
        {
            Item advancedPortableTeleporter = AssetBundleHandler.GetItemObject("Advanced Portable Tele");
            if (advancedPortableTeleporter == null) return;

            advancedPortableTeleporter.creditsWorth = UpgradeBus.Instance.PluginConfiguration.ADVANCED_TELE_PRICE.Value;
            advancedPortableTeleporter.itemName = "Advanced Portable Tele";
            advancedPortableTeleporter.itemId = 492013;
            AdvancedPortableTeleporter advancedTeleportScript = advancedPortableTeleporter.spawnPrefab.AddComponent<AdvancedPortableTeleporter>();
            advancedTeleportScript.itemProperties = advancedPortableTeleporter;
            advancedTeleportScript.grabbable = true;
            advancedTeleportScript.useCooldown = 2f;
            advancedTeleportScript.grabbableToEnemies = true;
            advancedTeleportScript.ItemBreak = itemBreak;
            advancedTeleportScript.error = error;
            advancedTeleportScript.buttonPress = buttonPressed;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(advancedPortableTeleporter.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("AdvTele", advancedPortableTeleporter);

            SetupStoreItem(advancedPortableTeleporter);
        }

        private void SetupNightVision()
        {
            Item nightVisionItem = AssetBundleHandler.GetItemObject("Night Vision");
            if (nightVisionItem == null) return;

            nightVisionItem.creditsWorth = UpgradeBus.Instance.PluginConfiguration.NIGHT_VISION_PRICE.Value;
            nightVisionItem.spawnPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            nightVisionItem.itemId = 492014;
            NightVisionGoggles visScript = nightVisionItem.spawnPrefab.AddComponent<NightVisionGoggles>();
            visScript.itemProperties = nightVisionItem;
            visScript.grabbable = true;
            visScript.useCooldown = 2f;
            visScript.grabbableToEnemies = true;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(nightVisionItem.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Night", nightVisionItem);

            SetupStoreItem(nightVisionItem);
        }
        private void SetupDivingKit()
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

            UpgradeBus.Instance.ItemsToSync.Add("Dive",DiveItem);

            SetupStoreItem(DiveItem);
        }
        private void SetupMedkit()
        {
            Item MedKitItem = AssetBundleHandler.GetItemObject("Medkit");
            if (MedKitItem == null) return;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, UpgradeBus.Instance.PluginConfiguration.EXTRACTION_CONTRACT_AMOUNT_MEDKITS.Value), new Keyframe(1f, UpgradeBus.Instance.PluginConfiguration.EXTRACTION_CONTRACT_AMOUNT_MEDKITS.Value));

            MedKitItem.creditsWorth = UpgradeBus.Instance.PluginConfiguration.MEDKIT_PRICE.Value;
            MedKitItem.itemId = 492016;
            Medkit medScript = MedKitItem.spawnPrefab.AddComponent<Medkit>();
            medScript.itemProperties = MedKitItem;
            medScript.grabbable = true;
            medScript.useCooldown = 2f;
            medScript.grabbableToEnemies = true;
            medScript.error = error;
            medScript.use = buttonPressed;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(MedKitItem.spawnPrefab);

            SetupStoreItem(MedKitItem);

            Item MedKitMapItem = AssetBundleHandler.GetItemObject("MedkitMapItem");
            if (MedKitMapItem == null) return;
            Medkit medMapScript = MedKitMapItem.spawnPrefab.AddComponent<Medkit>();
            MedKitMapItem.spawnPrefab.AddComponent<ExtractionContract>();
            medMapScript.itemProperties = MedKitMapItem;
            medMapScript.grabbable = true;
            medMapScript.useCooldown = 2f;
            medMapScript.grabbableToEnemies = true;
            medMapScript.error = error;
            medMapScript.use = buttonPressed;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(MedKitMapItem.spawnPrefab);

            SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
            mapObjDef.spawnableMapObject = new SpawnableMapObject();
            mapObjDef.spawnableMapObject.prefabToSpawn = MedKitMapItem.spawnPrefab;
            MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.All, (level) => curve);
            UpgradeBus.Instance.spawnableMapObjects["MedkitMapItem"] = mapObjDef;
            UpgradeBus.Instance.spawnableMapObjectsAmount["MedkitMapItem"] = UpgradeBus.Instance.PluginConfiguration.EXTRACTION_CONTRACT_AMOUNT_MEDKITS.Value;

            UpgradeBus.Instance.ItemsToSync.Add("Medkit",MedKitItem);
        }
        private void SetupPeeper()
        {
            Item Peeper = AssetBundleHandler.GetItemObject("Peeper");
            if (Peeper == null) return;

            Peeper.creditsWorth = UpgradeBus.Instance.PluginConfiguration.PEEPER_PRICE.Value;
            Peeper.twoHanded = false;
            Peeper.itemId = 492017;
            Peeper.twoHandedAnimation = false;
            Peeper.spawnPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            Peeper peepScript = Peeper.spawnPrefab.AddComponent<Peeper>();
            peepScript.itemProperties = Peeper;
            peepScript.grabbable = true;
            peepScript.grabbableToEnemies = true;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Peeper.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Peeper", Peeper);

            SetupStoreItem(Peeper);
        }
        private void SetupWheelbarrows()
        {
            wheelbarrowSound = GetAudioClipList("Wheelbarrow Sound", 4);
            shoppingCartSound = GetAudioClipList("Scrap Wheelbarrow Sound", 4);
            SetupStoreWheelbarrow();
            SetupScrapWheelbarrow();
        }
        private AudioClip[] GetAudioClipList(string name, int length)
        {
            AudioClip[] array = new AudioClip[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = AssetBundleHandler.GetAudioClip($"{name} {i}");
            }
            return array;
        }
        private void SetupScrapWheelbarrow() 
        {
            Item wheelbarrow = AssetBundleHandler.GetItemObject("Scrap Wheelbarrow");
            if (wheelbarrow == null) return;
            wheelbarrow.itemId = 492018;
            wheelbarrow.minValue = UpgradeBus.Instance.PluginConfiguration.SCRAP_WHEELBARROW_MINIMUM_VALUE.Value;
            wheelbarrow.maxValue = UpgradeBus.Instance.PluginConfiguration.SCRAP_WHEELBARROW_MAXIMUM_VALUE.Value;
            wheelbarrow.twoHanded = true;
            wheelbarrow.twoHandedAnimation = true;
            wheelbarrow.grabAnim = "HoldJetpack";
            wheelbarrow.floorYOffset = -90;
            wheelbarrow.positionOffset = new Vector3(0f, -1.7f, 0.35f);
            wheelbarrow.allowDroppingAheadOfPlayer = true;
            wheelbarrow.isConductiveMetal = true;
            wheelbarrow.isScrap = true;
            wheelbarrow.weight = 0.99f + (UpgradeBus.Instance.PluginConfiguration.SCRAP_WHEELBARROW_WEIGHT.Value /100f);
            wheelbarrow.canBeGrabbedBeforeGameStart = true;
            ScrapWheelbarrow barrowScript = wheelbarrow.spawnPrefab.AddComponent<ScrapWheelbarrow>();
            barrowScript.itemProperties = wheelbarrow;
            barrowScript.wheelsClip = shoppingCartSound;
            wheelbarrow.spawnPrefab.AddComponent<ScrapValueSyncer>();
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(wheelbarrow.spawnPrefab);
            LethalLib.Modules.Items.RegisterItem(wheelbarrow);
            Utilities.FixMixerGroups(wheelbarrow.spawnPrefab);
            int amountToSpawn = UpgradeBus.Instance.PluginConfiguration.SCRAP_WHEELBARROW_ENABLED.Value ? 1 : 0;

            AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe((1f - UpgradeBus.Instance.PluginConfiguration.SCRAP_WHEELBARROW_RARITY.Value), amountToSpawn), new Keyframe(1, amountToSpawn));
            SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
            mapObjDef.spawnableMapObject = new SpawnableMapObject();
            mapObjDef.spawnableMapObject.prefabToSpawn = wheelbarrow.spawnPrefab;
            MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.All, (level) => curve);
        }
        private void SetupStoreWheelbarrow()
        {
            Item wheelbarrow = AssetBundleHandler.GetItemObject("Store Wheelbarrow");
            if (wheelbarrow == null) return;

            wheelbarrow.itemId = 492019;
            wheelbarrow.creditsWorth = UpgradeBus.Instance.PluginConfiguration.WHEELBARROW_PRICE.Value;
            wheelbarrow.twoHanded = true;
            wheelbarrow.twoHandedAnimation = true;
            wheelbarrow.grabAnim = "HoldJetpack";
            wheelbarrow.floorYOffset = -90;
            wheelbarrow.verticalOffset =0.3f;
            wheelbarrow.positionOffset = new Vector3(0f, -0.7f, 1.4f);
            wheelbarrow.allowDroppingAheadOfPlayer = true;
            wheelbarrow.isConductiveMetal = true;
            wheelbarrow.weight = 0.99f + (UpgradeBus.Instance.PluginConfiguration.WHEELBARROW_WEIGHT.Value/100f);
            wheelbarrow.canBeGrabbedBeforeGameStart = true;
            StoreWheelbarrow barrowScript = wheelbarrow.spawnPrefab.AddComponent<StoreWheelbarrow>();
            barrowScript.itemProperties = wheelbarrow;
            barrowScript.wheelsClip = wheelbarrowSound;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(wheelbarrow.spawnPrefab);
            Utilities.FixMixerGroups(wheelbarrow.spawnPrefab);

            UpgradeBus.Instance.ItemsToSync.Add("Wheel", wheelbarrow);

            SetupStoreItem(wheelbarrow);
        }
        private void SetupPerks(ref IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                if (!type.IsSubclassOf(typeof(BaseUpgrade))) continue;
                if (type == typeof(OneTimeUpgrade) || type == typeof(TierUpgrade) || type == typeof(GameAttributeTierUpgrade)) continue;
                UpgradeBus.Instance.upgradeTypes.Add(type);

                MethodInfo method = type.GetMethod(nameof(BaseUpgrade.RegisterUpgrade), BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, null);
            }
            mls.LogInfo("Upgrades have been setup");
        }

        private void SetupCommands(ref IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                if (!type.IsSubclassOf(typeof(BaseCommand))) continue;
                UpgradeBus.Instance.commandTypes.Add(type);

                MethodInfo method = type.GetMethod(nameof(BaseCommand.RegisterCommand), BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, null);
            }
            mls.LogInfo("Commands have been setup");
        }
    }   
}
