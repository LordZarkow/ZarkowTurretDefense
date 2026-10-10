using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using ZarkowTurretDefense.Models;
using ZarkowTurretDefense.Services;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        public const string PluginVersion = "1.6.1130";

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

        public static ConfigEntry<bool> ExportBuiltinConfigFiles;
        public static ConfigEntry<string> TurretListFile;
        public static ConfigEntry<string> BuildingpartListFile;

        // end settings from config file

        private readonly Harmony _harmony = new Harmony(PluginGUID);
        
        private readonly Dictionary<string, AssetBundle> _assetBundles = new Dictionary<string, AssetBundle>();

        // the lists in use (built-in, or the user's override file) and what was registered from them
        private List<TurretConfig> _turretConfigs = new List<TurretConfig>();
        private List<BuildingpartConfig> _buildingpartConfigs = new List<BuildingpartConfig>();
        private readonly Dictionary<string, RegisteredTurret> _turrets = new Dictionary<string, RegisteredTurret>();
        private readonly Dictionary<string, RegisteredBuildingpart> _buildingparts = new Dictionary<string, RegisteredBuildingpart>();
        private string _turretOverrideText = string.Empty;        // text of the list files as read at startup; what a server sends to clients
        private string _buildingpartOverrideText = string.Empty;
        private string _lastAppliedTurretJson;
        private string _lastAppliedBuildingpartJson;
        private CustomRPC _listSyncRpc;

        private class RegisteredTurret
        {
            public GameObject Prefab;
            public TurretBase Turret;
        }

        private class RegisteredBuildingpart
        {
            public GameObject Prefab;
            public BuildingpartBase Buildingpart;
        }

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

            // custom lists: a user file in BepInEx/config replaces the built-in turret or building-part list (see README)
            ExportBuiltinConfigFiles = Config.Bind("CustomLists", "Export Built-in Config Files", false, new ConfigDescription("When true, the built-in turret and building-part lists are written to BepInEx/config as *.default.json at startup, to copy from when making an override file. See README."));
            TurretListFile = Config.Bind("CustomLists", "Turret List File", ConfigFileService.DefaultOverrideFileName(ConfigFileService.TurretsKind), new ConfigDescription("JSON file with the complete turret list; when it exists it replaces the built-in list. A file name is looked up in BepInEx/config, an absolute path is used as given. Empty, or no such file: the built-in list. On a server the server's file is sent to every client."));
            BuildingpartListFile = Config.Bind("CustomLists", "Buildingpart List File", ConfigFileService.DefaultOverrideFileName(ConfigFileService.BuildingpartsKind), new ConfigDescription("JSON file with the complete building-part list; when it exists it replaces the built-in list. A file name is looked up in BepInEx/config, an absolute path is used as given. Empty, or no such file: the built-in list. On a server the server's file is sent to every client."));

            if (ExportBuiltinConfigFiles.Value)
            {
                ConfigFileService.ExportBuiltin(ConfigFileService.TurretsKind, ConfigFileService.TurretsResource);
                ConfigFileService.ExportBuiltin(ConfigFileService.BuildingpartsKind, ConfigFileService.BuildingpartsResource);
            }

            _turretOverrideText = ConfigFileService.ReadOverrideText(ConfigFileService.TurretsKind, TurretListFile.Value);
            _buildingpartOverrideText = ConfigFileService.ReadOverrideText(ConfigFileService.BuildingpartsKind, BuildingpartListFile.Value);
            _lastAppliedTurretJson = _turretOverrideText;
            _lastAppliedBuildingpartJson = _buildingpartOverrideText;

            _turretConfigs = SelectTurretConfigs(_turretOverrideText);
            _buildingpartConfigs = SelectBuildingpartConfigs(_buildingpartOverrideText);

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

            // a server sends its list files to every connecting client before the world loads (Jotunn initial
            // synchronization); the client then re-applies stats, costs and availability to what it registered
            _listSyncRpc = NetworkManager.Instance.AddRPC("ZarkowTurretDefense_CustomLists", ListSyncServerReceive, ListSyncClientReceive);
            SynchronizationManager.Instance.AddInitialSynchronization(_listSyncRpc, BuildListSyncPackage);

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

            var turretConfigs = _turretConfigs;   // selected in Awake: built-in list or the user's override file

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

                            case TurretType.AdvancedGun:
                                turret = prefab.AddComponent<AdvancedGunTurret>();
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

                        _turrets[turretConfig.name] = new RegisteredTurret { Prefab = prefab, Turret = turret };

                        Jotunn.Logger.LogDebug($"### --- Turret Added ---");
                    } // if DO we have prefab in asset bundle
                }
            });
        } // AddTurrets

        private void AddBuildingParts()
        {
            Jotunn.Logger.LogInfo($"### --- Added BuildingParts ---");

            var buildingpartsConfigs = _buildingpartConfigs;   // selected in Awake: built-in list or the user's override file

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
                        case BuildingpartType.HoverCart:
                            // add custom hover script
                            buildingpart = prefab.AddComponent<HoverCart>();
                            break;
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

                    _buildingparts[buildingpartConfig.name] = new RegisteredBuildingpart { Prefab = prefab, Buildingpart = buildingpart };

                    Jotunn.Logger.LogDebug($"### --- BuildingPart Added ---");
                }
            });
        } // AddBuildingParts

        private static List<TurretConfig> SelectTurretConfigs(string overrideJson)
        {
            return ConfigFileService.SelectList<TurretConfig>(ConfigFileService.TurretsKind, ConfigFileService.TurretsResource, overrideJson,
                TurretConfigManager.Parse, config => config.name, ConfigValidation.ValidateTurrets);
        }

        private static List<BuildingpartConfig> SelectBuildingpartConfigs(string overrideJson)
        {
            return ConfigFileService.SelectList<BuildingpartConfig>(ConfigFileService.BuildingpartsKind, ConfigFileService.BuildingpartsResource, overrideJson,
                BuildingpartConfigManager.Parse, config => config.name, ConfigValidation.ValidateBuildingparts);
        }

        private ZPackage BuildListSyncPackage(ZNetPeer peer)
        {
            var package = new ZPackage();
            package.Write(_turretOverrideText ?? string.Empty);
            package.Write(_buildingpartOverrideText ?? string.Empty);
            return package;
        }

        private IEnumerator ListSyncServerReceive(long sender, ZPackage package)
        {
            // clients never send lists; anything arriving on the server is ignored
            yield break;
        }

        private IEnumerator ListSyncClientReceive(long sender, ZPackage package)
        {
            var turretText = package.ReadString();
            var buildingpartText = package.ReadString();
            Jotunn.Logger.LogInfo("### Config: turret and building-part lists received from the server, applying");
            ReapplyTurretConfigs(turretText);
            ReapplyBuildingpartConfigs(buildingpartText);
            yield break;
        }

        /// <summary>
        /// Applies a (synced) turret list to what was registered at startup: stats and costs for listed
        /// turrets, build-menu removal for unlisted ones. A turret in the list that was not registered at
        /// startup cannot be added without a restart; that is logged.
        /// </summary>
        private void ReapplyTurretConfigs(string overrideJson)
        {
            if (overrideJson == _lastAppliedTurretJson)
            {
                return;
            }
            _lastAppliedTurretJson = overrideJson;

            _turretConfigs = SelectTurretConfigs(overrideJson);

            var byName = new Dictionary<string, TurretConfig>();
            foreach (var config in _turretConfigs)
            {
                if (!string.IsNullOrEmpty(config.name) && !byName.ContainsKey(config.name))
                {
                    byName.Add(config.name, config);
                }
            }

            foreach (var registered in _turrets)
            {
                var piece = PieceManager.Instance.GetPiece(registered.Value.Prefab.name);
                TurretConfig config;
                if (byName.TryGetValue(registered.Key, out config))
                {
                    registered.Value.Turret.Initialize(config);   // prefab component: new placements and reloaded worlds
                    if (piece != null)
                    {
                        piece.Piece.m_resources = new PieceConfig { Requirements = config.resources.Select(TurretConfigRequirement.Convert).ToArray() }.GetRequirements();
                        piece.Piece.m_enabled = config.enabled;
                    }
                }
                else if (piece != null)
                {
                    piece.Piece.m_enabled = false;   // not in the active list: cannot be built here
                }
            }

            foreach (var name in byName.Keys.Except(_turrets.Keys))
            {
                Jotunn.Logger.LogWarning($"### Config 'turrets': '{name}' is in the active list but was not registered at startup; restart with the same list to use it");
            }
        }

        private void ReapplyBuildingpartConfigs(string overrideJson)
        {
            if (overrideJson == _lastAppliedBuildingpartJson)
            {
                return;
            }
            _lastAppliedBuildingpartJson = overrideJson;

            _buildingpartConfigs = SelectBuildingpartConfigs(overrideJson);

            var byName = new Dictionary<string, BuildingpartConfig>();
            foreach (var config in _buildingpartConfigs)
            {
                if (!string.IsNullOrEmpty(config.name) && !byName.ContainsKey(config.name))
                {
                    byName.Add(config.name, config);
                }
            }

            foreach (var registered in _buildingparts)
            {
                var piece = PieceManager.Instance.GetPiece(registered.Value.Prefab.name);
                BuildingpartConfig config;
                if (byName.TryGetValue(registered.Key, out config))
                {
                    registered.Value.Buildingpart.Initialize(config);
                    if (piece != null)
                    {
                        piece.Piece.m_resources = new PieceConfig { Requirements = config.resources.Select(BuildingpartConfigRequirement.Convert).ToArray() }.GetRequirements();
                        piece.Piece.m_enabled = config.enabled;
                    }
                }
                else if (piece != null)
                {
                    piece.Piece.m_enabled = false;
                }
            }

            foreach (var name in byName.Keys.Except(_buildingparts.Keys))
            {
                Jotunn.Logger.LogWarning($"### Config 'buildingparts': '{name}' is in the active list but was not registered at startup; restart with the same list to use it");
            }
        }

    }
}