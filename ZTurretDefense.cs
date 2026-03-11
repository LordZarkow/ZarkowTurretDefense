using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using ZarkowTurretDefense.Models;
using ZarkowTurretDefense.Services;
using UnityEngine;
using System.Collections.Generic;

namespace ZarkowTurretDefense
{
    using System;
    using Scripts;
    using System.IO;
    using System.Reflection;

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class ZTurretDefense : BaseUnityPlugin
    {
        public const string PluginGUID = "com.digitalsoftware.zarkowturretdefense";
        public const string PluginName = "Zarkow's Turret Defense";
        public const string PluginVersion = "1.0.1000";

        // settings from config file

        public static ConfigEntry<int> TurretVolume;
        public static ConfigEntry<int> TurretStyle;

        public static ConfigEntry<float> DamageModifier;
        public static ConfigEntry<float> CostModifier;

        public static ConfigEntry<bool> DisableTurretLight;
        public static ConfigEntry<bool> DisableDroneLight;
        public static ConfigEntry<bool> DisableBuildingpartsLight;

        public static ConfigEntry<bool> TurretsShouldFullyIgnorePlayers;

        public static ConfigEntry<bool> ShowHeightMapDebugLogEntries;
        public static ConfigEntry<bool> ShowObjectDestroyDebugLogEntries;

        // end settings from config file

        private readonly Harmony _harmony = new Harmony(PluginGUID);
        
        private readonly Dictionary<string, AssetBundle> _assetBundles = new Dictionary<string, AssetBundle>();

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            TurretVolume = Config.Bind("General", "Turret Volume", 100, new ConfigDescription("Custom Turret Volume", new AcceptableValueRange<int>(0, 100)));
            TurretStyle = Config.Bind("General", "Turret Style", 0, new ConfigDescription("Turret Style (Not active yet)", new AcceptableValueRange<int>(0, 4)));

            DamageModifier = Config.Bind("Difficulty", "Damage Modifier", 1.0f, new ConfigDescription("Difficulty: Optionally Modify Damage of Turrets", new AcceptableValueRange<float>(0.01f, 100.0f)));
            CostModifier = Config.Bind("Difficulty", "Cost Modifier", 1.0f, new ConfigDescription("Difficulty: Optionally Modify the Material Cost of Turrets", new AcceptableValueRange<float>(0.01f, 100.0f)));

            // behavior tweaks
            TurretsShouldFullyIgnorePlayers = Config.Bind("BehaviorTweaks", "Turrets Should Fully Ignore Players", false, new ConfigDescription("Tweak: Turrets should no longer even look at players for targeting purpose", new AcceptableValueRange<bool>(false, true)));

            // performance weak settings - disable the spotlights in the Piece's
            DisableTurretLight = Config.Bind("PerformanceTweaks", "Disable Lights - Turrets", false, new ConfigDescription("Tweak: Disable Lights - Turrets", new AcceptableValueRange<bool>(false, true)));
            DisableDroneLight = Config.Bind("PerformanceTweaks", "Disable Lights - Drones", false, new ConfigDescription("Tweak: Disable Lights - Drones", new AcceptableValueRange<bool>(false, true)));
            DisableBuildingpartsLight = Config.Bind("PerformanceTweaks", "Disable Lights - Buildingparts", false, new ConfigDescription("Tweak: Disable Lights - Buildingparts", new AcceptableValueRange<bool>(false, true)));

            // debug
            ShowHeightMapDebugLogEntries = Config.Bind("Debug", "Show HeightMap Debug Log Entries", false, new ConfigDescription("Debug: Show HeightMap Warning and Info log lines", new AcceptableValueRange<bool>(false, true)));
            ShowObjectDestroyDebugLogEntries = Config.Bind("Debug", "Show Object Destroy Debug Log Entries", false, new ConfigDescription("Debug: Show log lines when an object from the mod pack is de-loaded as player move out of range", new AcceptableValueRange<bool>(false, true)));

            LoadAssetBundles();

            // add all known localizations
            LoadLocalization("English");
            LoadLocalization("German");

            // possible addition - look for .json file in dll lib folder, as replacement translations

            // add all turrets from config file
            AddTurrets();

            // add lights / spotlights and building-pieces
            AddBuildingParts();

            UnloadAssetBundles();

            _harmony.PatchAll();

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo($"### RELEASE {PluginVersion} ### {PluginName} Loaded.");
            
            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }


        private void LoadAssetBundles()
        {
            _assetBundles.Add("turrets", AssetUtils.LoadAssetBundleFromResources("turrets"));
        }

        private void UnloadAssetBundles()
        {
            foreach (var assetBundle in _assetBundles)
            {
                assetBundle.Value.Unload(false);
            }
        }

        private void LoadLocalization(string languageName)
        {
            Jotunn.Logger.LogInfo($"### Load Localization language {languageName}");
            var langFile = LoadLocalizationJsonFromResource($"ZarkowTurretDefense.Assets.Localizations.{languageName}.json");
            Localization.AddJsonFile(languageName, langFile);
        }

        public static string LoadLocalizationJsonFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string jsonResourceFile;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonResourceFile = reader.ReadToEnd(); //Make string equal to full file
            }

            return jsonResourceFile;
        }

        private void AddTurrets()
        {
            Jotunn.Logger.LogInfo($"### --- Added Turrets ---");

            var turretConfigs = new List<TurretConfig>();
            turretConfigs.AddRange(TurretConfigManager.LoadTurretConfigJsonFromResource("ZarkowTurretDefense.Assets.Configs.turretsconfigs.json"));

            turretConfigs.ForEach(turretConfig =>
            {
                if (turretConfig.enabled)
                {
                    // Load prefab from asset bundle and apply config

                    Jotunn.Logger.LogDebug($"### Get assetBundle for '{turretConfig.bundleName}'");
                    var assetBundle = _assetBundles[turretConfig.bundleName];

                    if (assetBundle == null)
                    {
                        Jotunn.Logger.LogWarning($"### assetBundle is null");
                        return;
                    }

                    Jotunn.Logger.LogDebug($"### Read asset from {turretConfig.prefabPath}");
                    var prefab = assetBundle.LoadAsset<GameObject>(turretConfig.prefabPath);

                    if (prefab == null)
                    {
                        Jotunn.Logger.LogFatal($"### Missing prefab {turretConfig.prefabPath} in bundle");
                    }
                    else
                    {

                        Jotunn.Logger.LogDebug($"### Add component script to prefab based on type");
                        TurretBase turret;

                        var turretType = (TurretType)Enum.Parse(typeof(TurretType), turretConfig.type, true);
                        switch (turretType)
                        {
                            case TurretType.SignalTurret:
                                turret = prefab.AddComponent<SignalTurret>();
                                break;

                            case TurretType.MineTurret:
                                turret = prefab.AddComponent<MineTurret>();
                                break;

                            case TurretType.LightGun:
                                turret = prefab.AddComponent<LightGunTurret>();
                                break;

                            case TurretType.Gun:
                                turret = prefab.AddComponent<GunTurret>();
                                break;

                            case TurretType.HeavyGun:
                                turret = prefab.AddComponent<HeavyGunTurret>();
                                break;

                            case TurretType.MissileGun:
                                turret = prefab.AddComponent<MissileTurret>();
                                break;

                            case TurretType.Drone:
                                turret = prefab.AddComponent<DroneTurret>();
                                break;

                            case TurretType.RepairDrone:
                                turret = prefab.AddComponent<RepairDroneTurret>();
                                break;

                            case TurretType.GatherDrone:
                                turret = prefab.AddComponent<GatherDroneTurret>();
                                break;

                            case TurretType.FishingDrone:
                                turret = prefab.AddComponent<FishingDroneTurret>();
                                break;

                            case TurretType.LoggerDrone:
                                turret = prefab.AddComponent<LoggerDroneTurret>();
                                break;

                            case TurretType.InertTurret:
                                turret = prefab.AddComponent<InertTurret>();
                                break;

                            default:
                                turret = prefab.AddComponent<TurretBase>();
                                break;
                        }
                        
                        Jotunn.Logger.LogDebug($"### Init Turret Config '{turretConfig.name}'");
                        turret.Initialize(turretConfig);

                        Jotunn.Logger.LogDebug($"### Apply Config and Create CustomPiece '{turret}'");
                        var turretPiece = TurretConfig.Convert(prefab, turretConfig);

                        Jotunn.Logger.LogDebug($"### Add piece to PieceManager");
                        PieceManager.Instance.AddPiece(turretPiece);

                        Jotunn.Logger.LogDebug($"### --- Turret Added ---");
                    } // if DO we have prefab in asset bundle
                }
            });
        } // AddTurrets

        private void AddBuildingParts()
        {
            Jotunn.Logger.LogInfo($"### --- Added BuildingParts ---");

            var buildingpartsConfigs = new List<BuildingpartConfig>();
            buildingpartsConfigs.AddRange(BuildingpartConfigManager.LoadBuildingpartConfigJsonFromResource("ZarkowTurretDefense.Assets.Configs.buildingpartsconfigs.json"));

            buildingpartsConfigs.ForEach(buildingpartConfig =>
            {
                if (buildingpartConfig.enabled)
                {
                    // Load prefab from asset bundle and apply config

                    Jotunn.Logger.LogDebug($"### Get assetBundle for '{buildingpartConfig.bundleName}'");
                    var assetBundle = _assetBundles[buildingpartConfig.bundleName];

                    if (assetBundle == null)
                    {
                        Jotunn.Logger.LogWarning($"### assetBundle is null");
                        return;
                    }

                    Jotunn.Logger.LogDebug($"### Read asset from {buildingpartConfig.prefabPath}");
                    var prefab = assetBundle.LoadAsset<GameObject>(buildingpartConfig.prefabPath);

                    Jotunn.Logger.LogDebug($"### Add component script to prefab based on type");
                    BuildingpartBase buildingpart;

                    var buildingpartType = (BuildingpartType)Enum.Parse(typeof(BuildingpartType), buildingpartConfig.type, true);
                    switch (buildingpartType)
                    {
                        default:
                            buildingpart = prefab.AddComponent<BuildingpartBase>();
                            break;
                    }

                    Jotunn.Logger.LogDebug($"### Init Buildingpart config '{buildingpartConfig.name}'");
                    buildingpart.Initialize(buildingpartConfig);

                    Jotunn.Logger.LogDebug($"### Apply Config and Create CustomPiece '{buildingpart}'");
                    var buildPiece = BuildingpartConfig.Convert(prefab, buildingpartConfig);

                    Jotunn.Logger.LogDebug($"### Add piece to PieceManager");
                    PieceManager.Instance.AddPiece(buildPiece);

                    Jotunn.Logger.LogDebug($"### --- BuildingPart Added ---");
                }
            });
        } // AddBuildingParts

    }
}