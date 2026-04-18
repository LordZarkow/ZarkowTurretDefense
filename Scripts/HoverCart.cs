using PlayFab.DataModels;
using PlayFab.EconomyModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using ZarkowTurretDefense;
using ZarkowTurretDefense.Models;
using ZarkowTurretDefense.Scripts;

namespace ZarkowTurretDefense.Scripts
{
    using static MeleeWeaponTrail;
    using Random = UnityEngine.Random;

    public class HoverCart : BuildingpartBase
    {
        protected Rigidbody _rigidBodyHoverCart;

        protected GameObject _partEngineRightFront;
        protected GameObject _partEngineLeftFront;
        protected GameObject _partEngineRightMid;
        protected GameObject _partEngineLeftMid;
        protected GameObject _partEngineRightRear;
        protected GameObject _partEngineLeftRear;

        protected LineRenderer _partEngineRightFrontLineRenderer;
        protected LineRenderer _partEngineLeftFrontLineRenderer;
        protected LineRenderer _partEngineRightMidLineRenderer;
        protected LineRenderer _partEngineLeftMidLineRenderer;
        protected LineRenderer _partEngineRightRearLineRenderer;
        protected LineRenderer _partEngineLeftRearLineRenderer;

        protected float hoverHeight = 3f;
        protected float springStrength = 80f;
        protected float damping = 12f;
        protected float maxLiftPerPoint = 100f;

        protected float uprightTorque = 20f;
        protected float uprightDamping = 5f;

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
                var msg = $"{BuildingpartTypeOfThisBuildingpart}.Awake() ({gameObject.name}) -- Heightmap loaded: {(ourHeightmap != null)} ({Heightmap.s_heightmaps.Count} heightmaps loaded), position: {this.transform.position}{(_zNetView.m_distant ? "-- Flagged as distant" : "")}";
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

        override protected void RegisterRemoteProcedureCalls()
        {
            // _zNetView.Register<int, Vector3, Vector3>("ZTD_FireTurretGun", RPC_FireTurretGun);
        }

        override protected void GetSpecialBodyPartsOfBuildingpart()
        {
            // AddDebugMsg($"HoverCart.GetSpecialBodyPartsOfBuildingpart()");

            _rigidBodyHoverCart = gameObject.GetComponent<Rigidbody>();
            _rigidBodyHoverCart.centerOfMass = new Vector3(0.0f, -0.5f, 0.0f);
            _rigidBodyHoverCart.linearDamping = 1.0f;
            _rigidBodyHoverCart.angularDamping = 2.0f;

            // get all named engine-entry points
            _partEngineRightFront = HelperLib.GetChildGameObject(gameObject, "EngineRightFront");
            // Add a LineRenderer component to each engine location
            _partEngineRightFrontLineRenderer = _partEngineRightFront.GetComponent<LineRenderer>();

            _partEngineLeftFront = HelperLib.GetChildGameObject(gameObject, "EngineLeftFront");
            _partEngineLeftFrontLineRenderer = _partEngineLeftFront.GetComponent<LineRenderer>();

            _partEngineRightMid = HelperLib.GetChildGameObject(gameObject, "EngineRightMid");
            _partEngineRightMidLineRenderer = _partEngineRightMid.GetComponent<LineRenderer>();

            _partEngineLeftMid = HelperLib.GetChildGameObject(gameObject, "EngineLeftMid");
            _partEngineLeftMidLineRenderer = _partEngineLeftMid.GetComponent<LineRenderer>();

            _partEngineRightRear = HelperLib.GetChildGameObject(gameObject, "EngineRightRear");
            _partEngineRightRearLineRenderer = _partEngineRightRear.GetComponent<LineRenderer>();

            _partEngineLeftRear = HelperLib.GetChildGameObject(gameObject, "EngineLeftRear");
            _partEngineLeftRearLineRenderer = _partEngineLeftRear.GetComponent<LineRenderer>();
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
                        AddWarningMsg($"{BuildingpartTypeOfThisBuildingpart}.Update() -- Finally got it >> Heightmap.FindHeightmap{this.transform.position} return  map {map.GetInstanceID()}, distantLod: {map.IsDistantLod}, bounds: {map.m_bounds}, width: {map.m_width}");
                    }

                    var wearntear = gameObject.GetComponent<WearNTear>();
                    wearntear.Start();

                    _everSeenHeightmap = true;
                }
            }

            if (_zNetView == null || _zNetView.IsOwner() == false)
            {
                return;
            }

            var timeDelta = Time.deltaTime;

            if (timeDelta == 0.0f)
                return;
        }

        private void FixedUpdate()
        {
            if (_everSeenHeightmap == false)
                return;

            if (_zNetView == null || _zNetView.IsOwner() == false)
            {
                return;
            }

            if (Time.deltaTime == 0.0f)
                return;

            AddLogInfo($"---- {_rigidBodyHoverCart.linearVelocity}");

            GenerateForceDependingOnDistanceToGround(_partEngineRightFront, _partEngineRightFrontLineRenderer);
            GenerateForceDependingOnDistanceToGround(_partEngineLeftFront, _partEngineLeftFrontLineRenderer);

            // GenerateForceDependingOnDistanceToGround(_partEngineRightMid, _partEngineRightMidLineRenderer);
            // GenerateForceDependingOnDistanceToGround(_partEngineLeftMid, _partEngineLeftMidLineRenderer);

            GenerateForceDependingOnDistanceToGround(_partEngineRightRear, _partEngineRightRearLineRenderer);
            GenerateForceDependingOnDistanceToGround(_partEngineLeftRear, _partEngineLeftRearLineRenderer);

            ApplyUprightStabilization();
        }

        private void GenerateForceDependingOnDistanceToGround(GameObject gameObject, LineRenderer engineLineRender)
        {
            // we will take the engine game object, and cast a ray in its down angle, to measure distance to ground.
            // We will round up to 0.1 if below, and apply max force when close to the ground, and no force if beyond 2 meters.
            var amount = 0.0f;
            float heightAboveGround = 1.1f;

            var ray = new Ray(gameObject.transform.position, Vector3.down);
            if (Physics.Raycast(ray, out var impactInfo, heightAboveGround, _rayMaskSolids))
            {
                // Ignore steep surfaces / walls
                float groundAlignment = Vector3.Dot(impactInfo.normal, Vector3.up);
                if (groundAlignment < 0.6f)
                    return;

                float hoverError = hoverHeight - impactInfo.distance;

                // Velocity at this hover point
                Vector3 pointVelocity = _rigidBodyHoverCart.GetPointVelocity(gameObject.transform.position);

                // Vertical speed relative to world up
                float verticalSpeed = Vector3.Dot(pointVelocity, Vector3.up);

                float springForce = hoverError * springStrength;
                float dampingForce = -verticalSpeed * damping;

                float lift = springForce + dampingForce;
                lift = Mathf.Clamp(lift, 0f, maxLiftPerPoint);

                // calc out impact
                var hitPoint = impactInfo.point;

                var distance = impactInfo.distance;

                if (distance < 0.1f)
                {
                    distance = 0.1f;
                }

                // apply upwards force linear to distance, as a start
                var distanceRatio = (1.0f - UnityEngine.Mathf.InverseLerp(0.1f, heightAboveGround, distance));
                amount = distanceRatio * 20.0f; // mult with our max-power value

                if (engineLineRender != null)
                {
                    engineLineRender.SetPosition(0, gameObject.transform.position);
                    engineLineRender.SetPosition(1, impactInfo.point);
                }

                var force = Vector3.up * amount; // only push straight up

                // AddLogInfo($"Hover: {gameObject.gameObject.name}, distance: {distance}, ratio: {distanceRatio}, amount: {amount}, Force {force}");

                _rigidBodyHoverCart.AddForceAtPosition(force, gameObject.transform.position, ForceMode.Acceleration);
            }
            else
            {
                if (engineLineRender != null)
                {
                    engineLineRender.SetPosition(0, gameObject.transform.position);
                    engineLineRender.SetPosition(1, gameObject.transform.position);
                }
            }

        }

        void ApplyUprightStabilization()
        {
            // Rotate craft so its up matches world up
            Vector3 currentUp = transform.up;
            Vector3 torqueAxis = Vector3.Cross(currentUp, Vector3.up);

            // Angular velocity damping around tilt axes
            Vector3 localAngularVel = transform.InverseTransformDirection(_rigidBodyHoverCart.angularVelocity);

            Vector3 correctiveTorque =
                torqueAxis * uprightTorque
                - _rigidBodyHoverCart.angularVelocity * uprightDamping;

            _rigidBodyHoverCart.AddTorque(correctiveTorque, ForceMode.Acceleration);
        }

    }
}