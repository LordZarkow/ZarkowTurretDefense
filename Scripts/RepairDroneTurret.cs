using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class RepairDroneTurret : DroneTurret
    {
        protected readonly int _rayMaskRepairDroneSolids = LayerMask.GetMask("piece", "piece_nonsolid", "vehicle");

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"DroneTurret.GetSpecialBodyPartsOfTurret()");

            _lightYellow = HelperLib.GetChildGameObject(gameObject, "Light (Yellow)");
            _lightRed = HelperLib.GetChildGameObject(gameObject, "Light (Red)");
            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            // get Sfx parts from turret
            _dirtImpactPrefab = HelperLib.GetChildGameObject(gameObject, "RepairDetonation");

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
            // AddDebugMsg("RepairDroneTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 180f;
            _rotationDefinitions.MaxRotationHorizontalRight = 180f;
            _rotationDefinitions.MaxRotationVerticalUp = 0.1f;
            _rotationDefinitions.MaxRotationVerticalDown = 0.1f;
            _rotationDefinitions.RotationSpeed = 360.0f;

            _rotationDefinitions.AllowedAimDeviance = 180.0f;

            // for cannon on the drone itself
            _droneRotationDefinitions.MaxRotationHorizontalLeft = 90f;
            _droneRotationDefinitions.MaxRotationHorizontalRight = 90f;
            _droneRotationDefinitions.MaxRotationVerticalUp = 1.0f;
            _droneRotationDefinitions.MaxRotationVerticalDown = 89.9f;
            _droneRotationDefinitions.RotationSpeed = 35.0f;

            _droneRotationDefinitions.AllowedAimDeviance = 5.0f;

            // holder of aim data
            _droneAimResult = new DegreesSpecifier();
        }

        override protected float SetDroneMovementHeight(float heightIn)
        {
            return heightIn + 3.0f;
        }

        override protected bool MoveAndHandleWearAndTear(Target target)
        {
            // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear({target.GetHashCode()}, {target.WearAndTearPiece.m_piece.m_name})");

            if (target.WearAndTearPiece == null)
            {
                AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear({target.GetHashCode()}, target.WearAndTearPiece == null --  piece might already be destroyed, skip target");
                return true;
            }

            var centerLoc = target.WearAndTearPiece.gameObject.transform.position;

            target.Location = new Vector3(centerLoc.x, SetDroneMovementHeight(centerLoc.y), centerLoc.z);

            // if we are some bit away from target location, increase speed towards max, or start decreasing towards 0.
            var distance = Vector3.Distance(_droneGameObject.transform.position, target.Location);
            if ((distance - DroneAttackRange) > _droneSpeed - 1.0f)
            {
                // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear() -- Speeding up -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
                _droneSpeed += Time.deltaTime * 3.0f;
                if (_droneSpeed > _droneMaxSpeed)
                    _droneSpeed = _droneMaxSpeed;
            }
            else
            {
                // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear() -- Slowing down -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
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

            // if we are within distance to target, get ready to repair
            var newDistance = Vector3.Distance(_droneGameObject.transform.position, target.Location);

            if (newDistance < DroneAttackRange)
            {
                UpdateDroneMode(TurretPatrolType.AttackTarget);
            }
            else
            {
                UpdateDroneMode(TurretPatrolType.ScanTarget);
            }

            // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear({target.GetHashCode()}, {target.WearAndTearPiece.gameObject.name}) -- distance: {newDistance} ({(newDistance / Range)}, {_droneMode}), speed: {_droneSpeed} ({(_droneSpeed * Time.deltaTime)}), health: {_droneTarget.WearAndTearPiece.GetHealthPercentage()}, new location: {_droneGameObject.transform.position}, target location: {target.Location} -- TTL: {target.TimeToLive}");

            if (_droneTarget.WearAndTearPiece.GetHealthPercentage() <= 0.0f)
            {
                AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear - Target {_droneTarget.WearAndTearPiece.m_piece.name} is Destroyed");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            // safety, making sure it is a repair drone flag set, then verify if we are done repairing
            if (RepairTarget)
            {
                if (_droneTarget.WearAndTearPiece.GetHealthPercentage() >= 1.0f)
                {
                    // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear - Target {_droneTarget.WearAndTearPiece.m_piece.name} is Fully Repaired");
                    UpdateDroneMode(TurretPatrolType.NoTarget);
                    return true;
                }
            }

            // if we are out of time, return that we are finished
            if (_droneTarget.TimeToLive <= 0.0f)
            {
                // AddDebugMsg($"RepairDroneTurret.MoveAndHandleWearAndTear - Target {_droneTarget.WearAndTearPiece.m_piece.name} aborted - we ran out of time on Target");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            return false;
        }

        override protected void FindNewTarget()
        {
            // AddDebugMsg("TurretBase.FindNewTarget()");

            float rangeCompareValue = float.MaxValue;

            var newTargetList = new List<Target>();

            // special, if we ONLY want to target Players (healing)
            if (OnlyPlayerForTargeting)
            {
                SetTargets(newTargetList, rangeCompareValue);
                return;
            }

            List<WearNTear> allWearAndTearPieces = WearNTear.GetAllInstances();

            // AddDebugMsg($"Target search: allWearAndTearPieces Found: {allWearAndTearPieces.Count}");

            foreach (WearNTear wearAndTearPiece in allWearAndTearPieces)
            {
                // evaluate if this target is ok, else continue to next

                if (wearAndTearPiece == null)
                    continue;

                // no need to be shooting dead targets
                if (wearAndTearPiece.GetHealthPercentage() <= 0.0f)
                {
                    AddDebugMsg($"WearAndTearPiece, Target search: Character {wearAndTearPiece.name}, is DESTROYED -- skipping");
                    continue;
                }

                // AddDebugMsg($"WearAndTearPiece, Target search: Validating: WearAndtearPiece {wearAndTearPiece.name}");

                // get range to char, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToWearAndTearPiece(wearAndTearPiece);

                // if more than *1.5f range, don't care about it at all, skip
                if (rangeToPiece > Range * 1.5f)
                    continue;

                // special, this is a Repair attack, so lets verify that the unit is below 100%
                if (RepairTarget)
                {
                    if (wearAndTearPiece.GetHealthPercentage() >= 0.9f) // only care when targets are below 90%, avoid constant repair-jobs being done at 99% mark in rain
                        continue;
                }

                // if piece is closer than min-range, put it in low-prio queue (not really used by repair-drones...)
                if (rangeToPiece < MinimumRange)
                {
                    rangeToPiece = (Range * 2.0f) + rangeToPiece;
                }
                else
                {
                    // adjust value based on health-percentage, to prio those that are damage more than others, even if further away from tower
                    rangeToPiece *= wearAndTearPiece.GetHealthPercentage();
                }

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    rangeCompareValue = AddWearAndTearPieceToTargetList(newTargetList, wearAndTearPiece, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // AddDebugMsg($"Target search: Target picked: {(newTarget != null ? $"{newTarget.m_name} ({newTarget.GetInstanceID()})" : "----")}, range-points: {rangeCompareValue} from {allCharacters.Count} chars");

            // adjust for target-mode set back to BEST target
            if (newTargetList.Count > 0)
            {
                rangeCompareValue = newTargetList[0].DistanceRating;
            }

            // set new list of targets and new (BEST) range compare, to set MODE
            SetTargets(newTargetList, rangeCompareValue);

            // inject higher time-spread of finding new pieces
            _updateTargetTimer += 10.0f;
        }

        private float GetRangeToWearAndTearPiece(WearNTear wearAndTearPiece)
        {
            return Vector3.Distance(wearAndTearPiece.gameObject.transform.position, _turretAimPoint.transform.position);
        }

        private float AddWearAndTearPieceToTargetList(List<Target> newTargetList, WearNTear wearAndTear, float distanceRating, int maxTargets)
        {
            // AddDebugMsg($"AddWearAndTearPieceToTargetList({newTargetList.Count}, {wearAndTear.name}, {distanceRating}, {maxTargets}) -- Health: {wearAndTear.GetHealthPercentage()}");
            var lowestDistance = float.MaxValue;

            // if we already have target stored, look for last one to get value to quit out early against
            // AddDebugMsg($"if (newTargetList.Count > 0) : if ({newTargetList.Count} > 0)");
            if (newTargetList.Count > 0)
            {
                lowestDistance = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            // if our value is worse than last one AND we are FULL already, skip out
            // AddDebugMsg($"if (distanceRating >= lowestDistance) : if ({distanceRating} >= {lowestDistance})");
            if (distanceRating >= lowestDistance && newTargetList.Count >= MaximumNumberOfTrackedTargets)
            {
                // AddDebugMsg($"Worse distanceRating AND we are having a FULL list, so skip");

                // AddDebugMsg($"return {lowestDistance}");
                return lowestDistance;
            }

            var indexToInsertTargetAt = GetTargetListIndexBasedOnDistance(newTargetList, distanceRating);

            // AddDebugMsg($"indexToInsertTargetAt = {indexToInsertTargetAt}");

            // index is outside elements we have
            // AddDebugMsg($"if (indexToInsertTargetAt > newTargetList.Count - 1) : if ({indexToInsertTargetAt} > {newTargetList.Count - 1}");
            if (indexToInsertTargetAt > newTargetList.Count - 1)
            {
                // if we have less than maximum targets, then we add at end -- else, return without having added anything
                // AddDebugMsg($"if (newTargetList.Count < maxTargets) : if ({newTargetList.Count} < {maxTargets})");
                if (newTargetList.Count < maxTargets)
                {
                    newTargetList.Add(CreateWearAndTearTarget(wearAndTear, distanceRating));

                    lowestDistance = distanceRating;
                }

                // AddDebugMsg($"return {lowestDistance}");
                return lowestDistance;
            }

            // AddDebugMsg($"We should insert the target at set pos, remove any target that goes beyond max, ({indexToInsertTargetAt}, {wearAndTear.name}, {distanceRating})");

            // we should insert the target at set pos, remove any target that goes beyond max
            newTargetList.Insert(indexToInsertTargetAt, CreateWearAndTearTarget(wearAndTear, distanceRating));

            // AddDebugMsg($"if (newTargetList.Count > maxTargets) : if ({newTargetList.Count} > {maxTargets})");
            if (newTargetList.Count > maxTargets)
            {
                newTargetList.RemoveAt(maxTargets);

                lowestDistance = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            // AddDebugMsg($"return {lowestDistance}");
            return lowestDistance;
        }

        private Target CreateWearAndTearTarget(WearNTear wearAndTear, float distanceRating)
        {
            return new Target()
            {
                Character = null,
                WearAndTearPiece = wearAndTear,
                IsTargetWearAndTearOrder = true,
                Location = wearAndTear.gameObject.transform.position,
                DistanceRating = distanceRating,
            };
        }

        override protected bool UpdateAimInfoForCurrentTarget()
        {
            if (_droneTarget == null)
                return false;

            if (_droneTarget.WearAndTearPiece == null)
            {
                return false;
            }

            if (_droneTarget.WearAndTearPiece.gameObject == null)
            {
                return false;
            }

            var result = HelperLib.UpdateAimInfoForCurrentPieceTarget(_droneTarget, _droneAimPoint, _droneAimResult, _aimResultTempCalcHolder);

            // AddDebugMsg($"RepairDroneTurret.UpdateAimInfoForCurrentPieceTarget({_droneTarget.WearAndTearPiece.gameObject.name}) -- {result}");

            return result;
        }

        // repair attempt
        override protected void LaunchProjectileAndRegisterHitPosition(Quaternion directionQuat, Vector3 barrelEdgePosition)
        {
            // AddDebugMsg($"RepairDroneTurret.LaunchProjectileAndRegisterHitPosition: Cast Ray: {directionQuat} from {barrelEdgePosition}");

            // var ray = new Ray(barrelEdgePosition, directionQuat * Vector3.forward);

            //if (Physics.Raycast(ray, out var impactInfo, Range * 2.0f, _rayMaskRepairDroneSolids) == false)
            //    return;

            // var hitArray = Physics.RaycastAll(ray, 4.1f, _rayMaskRepairDroneSolids);

            var detonationLocation = barrelEdgePosition;
            detonationLocation += (directionQuat * Vector3.forward) * (DroneAttackRange * 1.0f);

            var colliderArray = Physics.OverlapSphere(detonationLocation, 2.5f, _rayMaskRepairDroneSolids);

            // AddDebugMsg($"RepairDroneTurret.LaunchProjectileAndRegisterHitPosition: Items Hit Count: {colliderArray.Length}");

            if (colliderArray.Length == 0)
                return;

            foreach (var colliderHit in colliderArray)
            {
                var wearAndTearHit = colliderHit.transform.root.gameObject.GetComponent<WearNTear>();

                if (wearAndTearHit == null)
                {
                    continue;
                }

                if (wearAndTearHit.GetHealthPercentage() >= 1.0f)
                {
                    continue;
                }

                // AddDebugMsg($"RepairDroneTurret.LaunchProjectileAndRegisterHitPosition: {wearAndTearHit.gameObject.name}, {wearAndTearHit.gameObject.GetInstanceID()} - Location: {wearAndTearHit.gameObject.transform.position} - {wearAndTearHit.GetHealthPercentage()} - REPAIR");
                wearAndTearHit.Repair();
            }

            // tell other players the launch occur, what barrel to use and impact info, to draw effects...
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchDroneCannonProjectile", detonationLocation, Vector3.forward);
        }

    }
}
