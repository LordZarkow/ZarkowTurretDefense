using System;
using System.Collections;
using System.Collections.Generic;
using ZarkowTurretDefense;
using ZarkowTurretDefense.Models;
using UnityEngine;
using ZarkowTurretDefense.Scripts;

namespace ZarkowTurretDefense.Scripts
{
    using Random = UnityEngine.Random;

    public class BuildingpartBase : MonoBehaviour
    {
        // default values
        public float Range = 20f;
        public float MinimumRange = 0.0f;

        public BuildingpartType BuildingpartTypeOfThisBuildingpart = BuildingpartType.Static;

        protected HitData _hitData;

        private readonly int _viewBlockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "terrain", "viewblock", "vehicle");

        private readonly int _viewBlockMaskPlayersCharactersOnly = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");

        protected readonly int _rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");

        protected DegreesSpecifier _aimResult; // holding deg data from calc
        protected DegreesSpecifier _aimResultTempCalcHolder; // holding deg data from calc, for all temp cals

        // limitations
        protected RotationDefinitions _rotationDefinitions;

        protected RotationData _rotationData = new RotationData();

        protected ZNetView _zNetView;
        protected ZDO _zDataObject;
        protected NetDataObjectHandler _netDataObjectHandler;

        public bool IsOwner
        {
            get
            {
                if (_zNetView == null || !_zNetView.IsOwner())
                {
                    return false;
                }

                return true;
            }
        }

        void Awake()
        {
            // AddLogInfo($"{BuildingpartTypeOfThisBuildingpart}.Awake()");

            // AddDebugMsg("Register ZNetView");
            _zNetView = GetComponent<ZNetView>();
            _zDataObject = _zNetView.GetZDO();
            _netDataObjectHandler = new NetDataObjectHandler(_zDataObject);

            RegisterRemoteProcedureCalls();
            
            GetBasicBodyPartsOfBuildingpart();

            GetSpecialBodyPartsOfBuildingpart();

            _aimResult = new DegreesSpecifier();
            _aimResultTempCalcHolder = new DegreesSpecifier();

            // debug
            var ourHeightmap = Heightmap.FindHeightmap(this.transform.position);

            // only show msg in log if flag for debug is set
            if (ZTurretDefense.ShowHeightMapDebugLogEntries.Value == true)
            {
                var msg =
                    $"{BuildingpartTypeOfThisBuildingpart}.Awake() ({gameObject.name}) -- Heightmap loaded: {(ourHeightmap != null)} ({Heightmap.s_heightmaps.Count} heightmaps loaded), position: {this.transform.position}{(_zNetView.m_distant ? "-- Flagged as distant" : "")}";
                if (ourHeightmap == null)
                {
                    AddWarningMsg(msg);
                }
                else
                {
                    AddLogInfo(msg);
                }
            }

            if (ourHeightmap != null)
            {
                _everSeenHeightmap = true; // was be able to grab right away
            }
        }

        virtual protected void RegisterRemoteProcedureCalls()
        {
            // _zNetView.Register<int, Vector3, Vector3>("ZTD_FireTurretGun", RPC_FireTurretGun);
        }

        virtual protected void GetBasicBodyPartsOfBuildingpart()
        {
            // AddDebugMsg($"BuildingpartBase.GetBasicBodyPartsOfBuildingpart()");

            // get body-parts of buildingpart
            // _turretBase = HelperLib.GetChildGameObject(gameObject, "TurretBase");

            if (ZTurretDefense.DisableBuildingpartsLight.Value)
            {
                HelperLib.DisableLightsOnGameObjectAndChildren(gameObject);
            }
        }

        virtual protected void GetSpecialBodyPartsOfBuildingpart()
        {
            // AddDebugMsg($"BuildingpartBase.GetSpecialBodyPartsOfBuildingpart()");
        }
        
        protected void AddLogMsg(string message)
        {
            Jotunn.Logger.LogMessage($"{DateTime.Now:o} ### Buildingpart[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddDebugMsg(string message)
        {
            Jotunn.Logger.LogDebug($"{DateTime.Now:o} ### Buildingpart[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddLogInfo(string message)
        {
            Jotunn.Logger.LogInfo($"{DateTime.Now:o} ### Buildingpart[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddWarningMsg(string message)
        {
            Jotunn.Logger.LogWarning($"{DateTime.Now:o} ### Buildingpart[{gameObject.GetInstanceID()}]: {message}");
        }
        
        public void Initialize(BuildingpartConfig buildingpartConfig)
        {
            // AddDebugMsg($"BuildingpartBase.Initialize()");

            BuildingpartTypeOfThisBuildingpart = (BuildingpartType)Enum.Parse(typeof(BuildingpartType), buildingpartConfig.type, true);
        }

        private void OnDestroy()
        {
            if (ZTurretDefense.ShowObjectDestroyDebugLogEntries.Value == true)
            {
                AddLogInfo($"OnDestroy({gameObject.name}, {this.transform.position}) ");
            }
        }

        private bool _everSeenHeightmap = false;
        private void Update()
        {
            if (_everSeenHeightmap == false)
            {
                var map = Heightmap.FindHeightmap(this.transform.position);
                if (map != null)
                {
                    if (ZTurretDefense.ShowHeightMapDebugLogEntries.Value == true)
                    {
                        AddWarningMsg(
                            $"{BuildingpartTypeOfThisBuildingpart}.Update() -- Finally got it >> Heightmap.FindHeightmap{this.transform.position} return  map {map.GetInstanceID()}, distantLod: {map.IsDistantLod}, bounds: {map.m_bounds}, width: {map.m_width}");
                    }

                    var wearntear = gameObject.GetComponent<WearNTear>();
                    wearntear.Start();

                    _everSeenHeightmap = true;
                }
            }

            if (_zNetView == null || _zNetView.IsOwner() == false)
            {
                // update is ONLY run on the OWNERS simulation -- so the SYNC READ is run on ALL OTHERS
                SyncTurretNetData();
                _netDataObjectHandler.ReceiveDone();
                return;
            }
        }

        virtual protected void SyncTurretNetData()
        {
            if (_netDataObjectHandler.HasDataToRead == false)
                return;
        }


    }
}