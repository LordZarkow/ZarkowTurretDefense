using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class DroneTurret : TurretBase
    {
        protected GameObject _droneGameObject;
        protected GameObject _droneSensorGameObject;

        protected GameObject _droneTurn;
        protected GameObject _droneTilt;

        protected GameObject _droneAimPoint;

        protected GameObject _droneLight;

        protected RotationDefinitions _droneRotationDefinitions;
        protected DegreesSpecifier _droneAimResult;
        private readonly RotationData _droneRotationData = new RotationData();

        protected Target _droneTarget; // run this separate, so we can cherry-pick it at will

        protected readonly float _droneMaxSpeed = 10.0f;
        protected float _droneSpeed;

        protected TurretPatrolType _droneMode = TurretPatrolType.ScanTarget;

        protected readonly int _rayMaskDroneDownCheck = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");

        protected int _inactivityToRestCounter = 3;

        override protected void RegisterRemoteProcedureCalls()
        {
            _zNetView.Register<Vector3, Vector3>("ZTD_LaunchDroneCannonProjectile", RPC_LaunchDroneCannonProjectile);
        }

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"DroneTurret.GetSpecialBodyPartsOfTurret()");

            _lightYellow = HelperLib.GetChildGameObject(gameObject, "Light (Yellow)");
            _lightRed = HelperLib.GetChildGameObject(gameObject, "Light (Red)");
            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            // get Sfx parts from turret
            _dirtImpactPrefab = HelperLib.GetChildGameObject(gameObject, "DirtImpact");

            _droneGameObject = HelperLib.GetChildGameObject(gameObject, "Drone");

            _droneSensorGameObject = HelperLib.GetChildGameObject(_droneGameObject, "DroneSensor");

            if (ZTurretDefense.DisableDroneLight.Value)
            {
                HelperLib.DisableLightsOnGameObjectAndChildren(_droneSensorGameObject);
            }

            _droneTurn = HelperLib.GetChildGameObject(_droneGameObject, "DroneTurn");
            _droneTilt = HelperLib.GetChildGameObject(_droneGameObject, "DroneTilt");

            _droneAimPoint = HelperLib.GetChildGameObject(_droneGameObject, "DroneAimPoint");

            // holder of drone light
            _droneLight = HelperLib.GetChildGameObject(_droneGameObject, "DroneSensorLight");
            // AddLogInfo($"Drone {gameObject.name}, _droneLight ({_droneLight})");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("DroneTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 180f;
            _rotationDefinitions.MaxRotationHorizontalRight = 180f;
            _rotationDefinitions.MaxRotationVerticalUp = 0.1f;
            _rotationDefinitions.MaxRotationVerticalDown = 0.1f;
            _rotationDefinitions.RotationSpeed = 360.0f;

            _rotationDefinitions.AllowedAimDeviance = 180.0f;

            // for cannon on the drone itself
            _droneRotationDefinitions.MaxRotationHorizontalLeft = 45f;
            _droneRotationDefinitions.MaxRotationHorizontalRight = 45f;
            _droneRotationDefinitions.MaxRotationVerticalUp = 5.0f;
            _droneRotationDefinitions.MaxRotationVerticalDown = 65.0f;
            _droneRotationDefinitions.RotationSpeed = 35.0f;

            _droneRotationDefinitions.AllowedAimDeviance = 25.0f;

            // holder of aim data
            _droneAimResult = new DegreesSpecifier();
        }

        override protected void SyncTurretNetData()
        {
            if (_netDataObjectHandler.HasDataToRead == false)
                return;

            base.SyncTurretNetData();

            // turret recharging parts -- possible future todo

            // sync drone
            _droneGameObject.transform.position = _netDataObjectHandler.Data.GetVec3("_droneGameObject.position", Vector3.zero);
            _droneGameObject.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_droneGameObject.rotation", Quaternion.identity);

            // cannon and sensor
            _droneTurn.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_droneTurn.rotation", Quaternion.identity);
            _droneTilt.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_droneTilt.rotation", Quaternion.identity);
            _droneSensorGameObject.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_droneSensorGameObject.rotation", Quaternion.identity);
        }

        override protected void ControlAndMoveMissilesOrProjectilesOrDrones()
        {
            // AddDebugMsg($"{TurretTypeOfThisTurret}.ControlAndMoveMissilesOrProjectilesOrDrones({_targetList.Count}) -- _droneTarget: {_droneTarget?.GetHashCode()}");

            // move the drone around -- if we have no target, random point within circle around drone, but 10m up
            // line down to hit ground, mark location and plus 4m up, travel there

            if (_droneTarget == null)
            {
                // we have nothing, check if there is any target from list to grab
                _droneTarget = _targetList.FirstOrDefault();

                if (_droneTarget != null)
                {
                    _inactivityToRestCounter = 3;

                    _droneTarget.TimeToLive = 10.0f; // minimum attack time before we can ask tower for a new target -- to make it less jittery

                    UpdateDroneMode(TurretPatrolType.ScanTarget);

                    // AddDebugMsg($"DroneTurret.ControlAndMoveMissilesOrProjectilesOrDrones({_targetList.Count}) -- grabbed Target from list: C:{(_droneTarget.Character != null)}/R:{_droneTarget.IsTargetWearAndTearOrder}/G:{_droneTarget.IsGatherItemOrder} , {_droneTarget.Location}");
                }
            }

            // if we are still at null - create a temp patrol target order
            if (_droneTarget == null && _inactivityToRestCounter > 0)
            {
                if (PatrolCloseToTurret && _inactivityToRestCounter > 0)
                {
                    _inactivityToRestCounter--;
                }

                // create a target, location based
                var randomLocationPosXPosZ = Random.insideUnitCircle * (PatrolCloseToTurret ? Range * 0.1f : Range);
                var randomLocationStart = new Vector3(transform.position.x + randomLocationPosXPosZ.x, transform.position.y + 250.0f, transform.position.z + randomLocationPosXPosZ.y);

                Ray ray = new Ray(randomLocationStart, Vector3.down);
                if (Physics.Raycast(ray, out var impactInfo, 500.0f, _rayMaskDroneDownCheck))
                {
                    // grab location, create new target, add it to list...
                    var heightToMoveTo = impactInfo.point.y + 5.0f;
                    if (heightToMoveTo < 35.0f)
                        heightToMoveTo = 35.0f;

                    var newTargetLocation = new Vector3(impactInfo.point.x, heightToMoveTo, impactInfo.point.z);

                    // verify there is a free path to this location, or else bounce before the impact point?

                    var newTarget = new Target()
                    {
                        Location = newTargetLocation,
                        IsMoveOrder = true,
                        TimeToLive = 15.0f, // TTL here is ONLY for making sure we are not quickly switching locations
                    };
                    _droneTarget = newTarget;

                    UpdateDroneMode(TurretPatrolType.NoTarget);

                    // AddDebugMsg($"DroneTurret.ControlAndMoveMissilesOrProjectilesOrDrones({_targetList.Count}) -- added new MOVEMENT Target: {newTarget.Location}");
                }

                // do something special if we fail to hit a point that is valid against terrain etc
            }
            else if (_droneTarget == null && _inactivityToRestCounter == 0)
            {
                // special, we want to rest home on the drone turret, so travel back to it, and next, park on it.
                var newTarget = new Target()
                {
                    Location = new Vector3(_turretAimPoint.transform.position.x, _turretAimPoint.transform.position.y + 2.0f, _turretAimPoint.transform.position.z),
                    IsMoveOrder = true,
                    TimeToLive = 10.0f,
                };
                _droneTarget = newTarget;

                UpdateDroneMode(TurretPatrolType.NoTarget);

                _inactivityToRestCounter--;
            } 
            else if (_droneTarget == null && _inactivityToRestCounter == -1)
            {
                // special, park on turret
                var newTarget = new Target()
                {
                    Location = new Vector3(_turretAimPoint.transform.position.x, _turretAimPoint.transform.position.y, _turretAimPoint.transform.position.z),
                    IsMoveOrder = true,
                    TimeToLive = 10.0f,
                };
                _droneTarget = newTarget;

                UpdateDroneMode(TurretPatrolType.NoTarget);

                _inactivityToRestCounter--;
            }


            if (_droneTarget == null)
                return;

            // travel towards target location or try to get within range of target, and hopefully get LOS on it.
            if (_droneTarget.TimeToLive > 0.0f)
                _droneTarget.TimeToLive -= Time.deltaTime;

            if (_droneTarget.IsMoveOrder)
            {
                var result = MoveTowardsTargetDestination(_droneTarget);

                if (result && _droneTarget.TimeToLive <= 0.0f)
                {
                    _droneTarget = null;
                }
                else
                {
                    // if we actually have a target waiting in queue, then switch to it
                    if (_targetList.Count > 0)
                        _droneTarget.TimeToLive = -999.0f;
                }

                // set data to net data object
                SetDataToDataObject();

                return;
            }

            if (_droneTarget.IsTargetWearAndTearOrder)
            {
                var repairResult = MoveAndHandleWearAndTear(_droneTarget);

                if (repairResult)
                {
                    // also remove this from target list
                    _targetList.Remove(_droneTarget);

                    _droneTarget = null;
                }

                // set data to net data object
                SetDataToDataObject();

                return;
            }

            if (_droneTarget.IsTargetLoggerOrder)
            {
                var loggerResult = MoveAndHandleTreesAndLogsAndStubs(_droneTarget);

                if (loggerResult)
                {
                    // also remove this from target list
                    _targetList.Remove(_droneTarget);

                    _droneTarget = null;
                }

                // set data to net data object
                SetDataToDataObject();

                return;
            }

            if (_droneTarget.IsGatherItemOrder)
            {
                var gatherResult = MoveAndHandleGather(_droneTarget);

                if (gatherResult || (_droneTarget.OnGatheritemDropReturnTrip == false && _droneTarget.TimeToLive < 0.0f))
                {
                    _targetList.Remove(_droneTarget);

                    _droneTarget = null;
                }

                // set data to net data object
                SetDataToDataObject();

                return;
            }

            // moving towards character target to destroy -- but safety check here if someone else destroyed it
            if (_droneTarget.Character != null && _droneTarget.Character.GetHealth() <= 0.0f)
            {
                _droneTarget = null;
                return;
            }

            var attackResult = MoveAndAttackTarget(_droneTarget);

            // set data to net data object
            SetDataToDataObject();

            // if we destroyed the target
            if (attackResult)
            {
                // also remove this from target list
                _targetList.Remove(_droneTarget);

                _droneTarget = null;
            }
        }

        private void SetDataToDataObject()
        {
            // drone body
            _netDataObjectHandler.Data.Set("_droneGameObject.position", _droneGameObject.transform.position);
            _netDataObjectHandler.Data.Set("_droneGameObject.rotation", _droneGameObject.transform.rotation);

            // drone turret and sensor
            _netDataObjectHandler.Data.Set("_droneTurn.rotation", _droneTurn.transform.rotation);
            _netDataObjectHandler.Data.Set("_droneTilt.rotation", _droneTilt.transform.rotation);
            _netDataObjectHandler.Data.Set("_droneSensorGameObject.rotation", _droneSensorGameObject.transform.rotation);
        }

        private bool MoveTowardsTargetDestination(Target target)
        {
            // AddDebugMsg($"DroneTurret.MoveTowardsTargetDestination({target.GetHashCode()})");

            if (target == null)
            {
                AddWarningMsg($"DroneTurret.MoveTowardsTargetDestination() -- target is null");
                return true;
            }

            if (_droneGameObject == null)
            {
                AddWarningMsg($"DroneTurret.MoveTowardsTargetDestination() -- _droneGameObject is null");
                return true;
            }

            // if we are some bit away from target location, increase speed towards max, or start decreasing towards 0.
            var distance = Vector3.Distance(_droneGameObject.transform.position, target.Location);
            if (distance > _droneSpeed + 0.1f)
            {
                _droneSpeed += Time.deltaTime * 3.0f;
                if (_droneSpeed > _droneMaxSpeed)
                    _droneSpeed = _droneMaxSpeed;
            }
            else
            {
                _droneSpeed -= Time.deltaTime * 5.0f;

                if (_droneSpeed < 0.0f)
                    _droneSpeed = (distance > 0.45f) ? 0.25f : 0.0f;
            }

            // calc direction between pos and targetPos
            var direction = target.Location - _droneGameObject.transform.position;
            direction.Normalize();

            if (distance > 5.0f)
            {
                // rotate also...
                var diffLocation = target.Location - _droneGameObject.transform.position;
                diffLocation.y = 0.0f;
                _droneGameObject.transform.rotation = Quaternion.Slerp(_droneGameObject.transform.rotation, Quaternion.LookRotation(diffLocation), 2.5f * Time.deltaTime);
            }

            // move drone according to properties
            _droneGameObject.transform.position += direction * _droneSpeed * Time.deltaTime;

            // if we reach target area, set TTL to 0.0f, so it will be removed
            var newDistance = Vector3.Distance(_droneGameObject.transform.position, target.Location);

            // AddDebugMsg($"DroneTurret.MoveTowardsTargetDestination({target.GetHashCode()}) -- distance: {newDistance}, speed: {_droneSpeed} ({(_droneSpeed * Time.deltaTime)}), new location: {_droneGameObject.transform.position}, target location: {target.Location}");

            if (newDistance < 0.5f)
            {

                // extra count-down of time if we are close to it's ending target location
                target.TimeToLive -= Time.deltaTime;

                return true;
            }

            return false;
        }

        virtual protected bool MoveAndHandleWearAndTear(Target target)
        {
            // implement in decending class that care about this

            AddWarningMsg($"DroneTurret.MoveAndHandleWearAndTear({target.GetHashCode()}, {target.WearAndTearPiece.m_piece.m_name}) -- Virtual, not implemented");

            return true; // always done
        }

        virtual protected bool MoveAndHandleGather(Target target)
        {
            // implement in decending class that care about this

            AddWarningMsg($"DroneTurret.MoveAndHandleGather({target.GetHashCode()}) -- Virtual, not implemented");

            return true; // always done
        }

        virtual protected bool MoveAndHandleTreesAndLogsAndStubs(Target target)
        {
            // implement in decending class that care about this

            AddWarningMsg($"DroneTurret.MoveAndHandleTreesAndLogs({target.GetHashCode()}) -- Virtual, not implemented");

            return true; // always done
        }

        virtual protected float SetDroneMovementHeight(float heightIn)
        {
            return heightIn + (HealingTarget ? 2.0f : 4.0f);
        }

        private bool MoveAndAttackTarget(Target target)
        {
            // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndAttackTarget({target.GetHashCode()}, {target.Character.m_name})");

            if (target.Character == null)
            {
                // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndAttackTarget({target.GetHashCode()}, target.Character == null --  most likely just recently died, so we skip out here and mark target as destroyed.");
                return true;
            }

            // something going on here, not sure...we try again later
            if (target.Character.m_collider == null)
            {
                AddWarningMsg($"{TurretTypeOfThisTurret}.MoveAndAttackTarget({target.GetHashCode()}, {target.Character.m_name} [{target.Character.GetInstanceID()}], {target.Character.GetHealth()} hp), lacks capsule collider...");
                // HelperLib.PrintAllReflectionOfObject(target.Character);
                return false;
            }

            var centerLoc = target.Character.GetCenterPoint();

            target.Location = new Vector3(centerLoc.x, SetDroneMovementHeight(centerLoc.y), centerLoc.z);

            // if we are some bit away from target location, increase speed towards max, or start decreasing towards 0.
            var distance = Vector3.Distance(_droneGameObject.transform.position, target.Location);
            if ((distance - DroneAttackRange ) > _droneSpeed - 1.0f)
            {
                _droneSpeed += Time.deltaTime * 3.0f;
                if (_droneSpeed > _droneMaxSpeed)
                    _droneSpeed = _droneMaxSpeed;
            }
            else
            {
                _droneSpeed -= Time.deltaTime * 5.0f;

                if (_droneSpeed < 0.0f)
                    _droneSpeed = (distance > DroneAttackRange * 0.3f) ? 0.25f : 0.0f;
            }

            // calc direction between pos and targetPos
            var direction = target.Location - _droneGameObject.transform.position;
            direction.Normalize();

            if (distance > 0.5f)
            {
                // rotate also...
                var diffLocation = target.Location - _droneGameObject.transform.position;
                diffLocation.y = 0.0f;
                _droneGameObject.transform.rotation = Quaternion.Slerp(_droneGameObject.transform.rotation, Quaternion.LookRotation(diffLocation), 2.5f * Time.deltaTime);
            }

            // move drone according to properties
            _droneGameObject.transform.position += direction * _droneSpeed * Time.deltaTime;

            // check if we can SEE the target
            var canSeeCharacter = CanSeeCharacter(target.Character, _droneAimPoint);

            // if we are within distance to target, enable firing guns
            var newDistance = Vector3.Distance(_droneGameObject.transform.position, target.Location);

            if (newDistance < DroneAttackRange && canSeeCharacter)
            {
                UpdateDroneMode(TurretPatrolType.AttackTarget);
            }
            else
            {
                UpdateDroneMode(TurretPatrolType.ScanTarget);
            }

            // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndAttackTarget({target.GetHashCode()}) -- distance: {newDistance} ({(newDistance / Range)}, {_droneMode}), speed: {_droneSpeed} ({(_droneSpeed * Time.deltaTime)}), health: {_droneTarget.Character.GetHealthPercentage()}, new location: {_droneGameObject.transform.position}, target location: {target.Location}");

            if (_droneTarget.Character.GetHealth() <= 0.0f)
            {
                // AddDebugMsg($"Target {_droneTarget.Character.m_name} is Dead");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            // if this is a healing drone, check if target is healed, then go to another target
            if (HealingTarget)
            {
                if (_droneTarget.Character.GetHealthPercentage() >= 1.0f)
                {
                    // AddDebugMsg($"Target {_droneTarget.Character.m_name} is Fully Healed");
                    UpdateDroneMode(TurretPatrolType.NoTarget);
                    return true;
                }
            }

            // if we are out of time, return that we are finished
            if (_droneTarget.TimeToLive <= 0.0f)
            {
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            return false;
        }

        override protected void UpdateShootingIntervalAndDetermineIfWeShouldShoot()
        {
            // count down delay until next shot based on ROF
            if (_nextShootDelayTimer > 0.0f)
                _nextShootDelayTimer -= Time.deltaTime;

            // only do this if we are in attack mode
            if (_droneMode != TurretPatrolType.AttackTarget)
                return;

            // AddDebugMsg($"{TurretTypeOfThisTurret}.UpdateShootingIntervalAndDetermineIfWeShouldShoot() - _nextShootDelayTimer: {_nextShootDelayTimer}, _droneMode: {_droneMode}");

            // special, gather drone, second leg, we don't care about aim
            if (_droneTarget.IsGatherItemOrder && _droneTarget.OnGatheritemDropReturnTrip)
            {
                if (_nextShootDelayTimer <= 0.0f)
                {
                    TriggerTurretFiring();
                }

                return;
            }

            // both axis must align -- check so we are within allowed deviance
            // update the distance in each rotation
            _droneRotationData.DistanceRotY = _droneRotationData.CurrentRotationY - _droneAimResult.DegreesY;
            _droneRotationData.DistanceRotX = _droneRotationData.CurrentRotationX - _droneAimResult.DegreesX;

            // still not time, so quit out
            if (_nextShootDelayTimer > 0.0f)
                return;

            // if either rotation is outside of the allowed deviance, skip shooting for now
            if (Math.Abs(_droneRotationData.DistanceRotY) > _droneRotationDefinitions.AllowedAimDeviance ||
                Math.Abs(_droneRotationData.DistanceRotX) > _droneRotationDefinitions.AllowedAimDeviance)
            {
                // AddDebugMsg($"DroneTurret.UpdateShootingIntervalAndDetermineIfWeShouldShoot() - Outside of AllowedAimDeviance: {_droneRotationDefinitions.AllowedAimDeviance}");
                return;
            }

            if (_nextShootDelayTimer <= 0.0f)
            {
                if (AmmoCount > 0)
                {
                    // we have no more ammo in gun, safety, we should never be able to fire a gun without ammo
                    if (_ammoInGun == 0)
                    {
                        return;
                    }
                }

                TriggerTurretFiring();

                // if gun has a magazine, reduce ammo in mag with 1
                if (AmmoCount > 0)
                {
                    _ammoInGun--;

                    if (_ammoInGun <= 0)
                    {
                        _reloadGunTimer = ReloadTime;
                    }
                }
            }
        }

        override protected void TriggerTurretFiring()
        {
            // we do not fire the turret, we only fire from the drone

            _nextShootDelayTimer += FireInterval;

            // safety -- maximum bad value is 'half fire interval away'
            if (_nextShootDelayTimer < (FireInterval / 2.0f))
                _nextShootDelayTimer = (FireInterval / 2.0f);

            // AddDebugMsg($"{TurretTypeOfThisTurret}.TriggerTurretFiring() - Fire, Barrel: {_nextBarrelIdToUse}");

            // basic turret always shoots straight as the turret is aimed currently
            LaunchProjectileAndRegisterHitPosition(_droneTilt.transform.rotation, _barrelList[0].ThisBarrelGameObject.transform.position);
        }

        // Launch projectile here (and apply damage on hit) - announce launch and impact effect for network
        override protected void LaunchProjectileAndRegisterHitPosition(Quaternion directionQuat, Vector3 barrelEdgePosition)
        {
            // AddDebugMsg($"DroneTurret.LaunchProjectileAndRegisterHitPosition: Cast Ray: {directionQuat} from {barrelEdgePosition}");

            var ray = new Ray(barrelEdgePosition, directionQuat * Vector3.forward);
            if (Physics.Raycast(ray, out var impactInfo, Range * 2.0f, _rayMaskSolids) == false)
                return;

            // safety, shouldn't happen
            if (_hitData == null)
            {
                AddWarningMsg("LaunchProjectileAndShowHitPosition() - _hitData is null");
                return;
            }

            // check what we hit -- if it is a character, push damage to it (except Players)
            var charHit = impactInfo.collider.transform.root.gameObject.GetComponent<Character>();

            if (charHit != null)
            {
                // special, this is a healing act, towards perhaps player
                if (HealingTarget)
                {
                    charHit.Heal(HealingAmount, true);
                }
                else
                {
                    // normal attack
                    var hitData = _hitData.Clone();
                    hitData.m_hitCollider = impactInfo.collider;
                    hitData.m_point = impactInfo.point;
                    hitData.m_dir = directionQuat.eulerAngles;

                    ApplyDamageToCharacterHit(charHit, hitData);
                }
            }

            // tell other players the launch occur, what barrel to use and impact info, to draw effects...
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchDroneCannonProjectile", impactInfo.point, impactInfo.normal);
        }


        override protected void CleanTargetListAndUpdateAimInfo()
        {
            // clean our char-targets when needed
            if (_targetList.Count > 0)
            {
                // clean out dead targets from list
                var deleteList = new List<Target>();
                foreach (var target in _targetList)
                {
                    if (target.Character != null && target.Character.GetHealth() <= 0.0f)
                        deleteList.Add(target);
                }

                foreach (var deleteTarget in deleteList)
                {
                    _targetList.Remove(deleteTarget);
                }
            }

            // update aim if drone has target still
            if (_droneTarget == null)
            {
                UpdateDroneMode(TurretPatrolType.NoTarget);
            }
            else
            {
                if (UpdateAimInfoForCurrentTarget() == false)
                {
                    // fail, something wrong with target
                    UpdateDroneMode(TurretPatrolType.NoTarget);
                }
            }
        }

        protected void UpdateDroneMode(TurretPatrolType newMode)
        {
            // AddDebugMsg($"{TurretTypeOfThisTurret}.UpdateDroneMode({newMode}), is: {_droneMode}");

            if (newMode == _droneMode)
                return;

            // a change, so impact drone
            _droneMode = newMode;

            // set Attack or Scan bits
            if (_droneMode == TurretPatrolType.AttackTarget || _droneMode == TurretPatrolType.ScanTarget)
            {
                if (_droneLight != null)
                {
                    // AddLogInfo($"Drone {gameObject.name}, set _droneLight:  ON ({_droneMode})");
                    _droneLight.SetActive(true);
                }
            }
            else // turn off, we are doing nothing
            {
                if (_droneLight != null)
                {
                    // AddLogInfo($"Drone {gameObject.name}, set _droneLight:  OFF ({_droneMode})");
                    _droneLight.SetActive(false);
                }
            }
        }


        override protected bool UpdateAimInfoForCurrentTarget()
        {
            if (_droneTarget == null)
                return false;

            // AddDebugMsg($"DroneTurret.UpdateAimInfoForCurrentTarget({_droneTarget.Character?.m_name})");
            return HelperLib.UpdateAimInfoForCurrentTarget(_droneTarget, _droneAimPoint, _droneAimResult, _aimResultTempCalcHolder);
        }

        override protected void RotateTurretTowardsTarget()
        {
            // AddDebugMsg($"DroneTurret.RotateTurretTowardsTarget()");

            // rotate drone cannon instead of turret
            HelperLib.RotateTurretTowardsTarget(_droneTurn, _droneTilt, _droneAimResult, _droneRotationDefinitions, _droneRotationData);

            // copy rotation and tilt to sensor
            _droneSensorGameObject.transform.rotation = _droneTilt.transform.rotation;

            // AddDebugMsg($"DroneTurret.RotateTurretTowardsTarget() -- rotations: {_droneTilt.transform.rotation.x} / {_droneTurn.transform.rotation.y}, {_droneSensorGameObject.transform.rotation.x} / {_droneSensorGameObject.transform.rotation.y} ");

            // store cannon and sensor to net
            _netDataObjectHandler.Data.Set("_droneTurn.rotation", _droneTurn.transform.rotation);
            _netDataObjectHandler.Data.Set("_droneTilt.rotation", _droneTilt.transform.rotation);
            _netDataObjectHandler.Data.Set("_droneSensorGameObject.rotation", _droneSensorGameObject.transform.rotation);
        }

        // --- RPC ---

        private void RPC_LaunchDroneCannonProjectile(long sender, Vector3 impactLocation, Vector3 impactNormal)
        {
            // AddDebugMsg($"DroneTurret.RPC_LaunchDroneCannonProjectile: {impactLocation}, {impactNormal}");

            // safety, if we have not set up barrels, we quit out
            if (_barrelList == null)
                return;

            if (_barrelList.Count == 0)
                return;

            var barrelObject = _barrelList.FirstOrDefault();
            if (barrelObject == null)
            {
                AddWarningMsg($"DroneTurret.RPC_LaunchDroneCannonProjectile: FAIL - no barrelObject found");
                return;
            }

            // LAUNCH FIRE EFFECT

            // clone effect, move out into the world, easier to retain due to oversight in API for transforms
            var newEffect = Instantiate(barrelObject.LaunchEffectGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newEffect.transform.SetParent(null);
            newEffect.SetActive(false);

            // AddLogMsg($"DroneTurret.RPC_LaunchDroneCannonProjectile: newEffect - {newEffect.name}");

            newEffect.SetActive(true);
            Destroy(newEffect, 5.0f); // destroy to force cleanup and remove odd loop/replay issue

            // TRIGGER AUDIO from barrel

            var newSfxEffect = Instantiate(barrelObject.LaunchAudioGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newSfxEffect.transform.SetParent(null);
            newSfxEffect.SetActive(false);

            // AddLogMsg($"DroneTurret.RPC_LaunchDroneCannonProjectile: newSfxEffect - {newSfxEffect.name}");

            newSfxEffect.SetActive(true);
            Destroy(newSfxEffect, 1.0f); // destroy to force cleanup and remove odd loop/replay issue

            // IMPACT EFFECT

            // hit something solid - pop hit-marker at the hit position
            var impactNormalQuat = new Quaternion();
            impactNormalQuat.SetLookRotation(impactNormal);

            var impactSfx = Instantiate(_dirtImpactPrefab, null);
            impactSfx.transform.position = impactLocation;
            impactSfx.transform.rotation = impactNormalQuat;

            impactSfx.SetActive(false);
            impactSfx.SetActive(true);

            // AddLogMsg($"DroneTurret.RPC_LaunchDroneCannonProjectile: impactFX - {impactSfx.name}");

            Destroy(impactSfx, 10.0f);

            // End Impact SFX
        }

    }
}
