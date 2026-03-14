using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using Jotunn.Managers;
    using UnityEngine;

    public class GatherDroneTurret : DroneTurret
    {
        protected readonly int _rayMaskGatherObjects = LayerMask.GetMask("item");

        private GameObject _droneCarryPoint;

        private Container _container;
        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"GatherDroneTurret.GetSpecialBodyPartsOfTurret()");

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

            _droneCarryPoint = HelperLib.GetChildGameObject(_droneGameObject, "DroneCarryPoint");

            // holder of drone light
            _droneLight = HelperLib.GetChildGameObject(_droneGameObject, "DroneSensorLight");
            // AddLogInfo($"Drone {gameObject.name}, _droneLight ({_droneLight})");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("GatherDroneTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 180f;
            _rotationDefinitions.MaxRotationHorizontalRight = 180f;
            _rotationDefinitions.MaxRotationVerticalUp = 0.1f;
            _rotationDefinitions.MaxRotationVerticalDown = 0.1f;
            _rotationDefinitions.RotationSpeed = 360.0f;

            _rotationDefinitions.AllowedAimDeviance = 180.0f;

            // for cannon on the drone itself
            _droneRotationDefinitions.MaxRotationHorizontalLeft = 120f;
            _droneRotationDefinitions.MaxRotationHorizontalRight = 120f;
            _droneRotationDefinitions.MaxRotationVerticalUp = 1.0f;
            _droneRotationDefinitions.MaxRotationVerticalDown = 89.9f;
            _droneRotationDefinitions.RotationSpeed = 35.0f;

            _droneRotationDefinitions.AllowedAimDeviance = 5.0f;

            // holder of aim data
            _droneAimResult = new DegreesSpecifier();

            // grab container ref, reduce lookup time
            _container = this.transform.GetComponent<Container>();
        }

        override protected float SetDroneMovementHeight(float heightIn)
        {
            return heightIn + 3.0f;
        }

        override protected bool MoveAndHandleGather(Target target)
        {
            // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndHandleGather({target.GetHashCode()}, {target.GatherItemDrop.name})");

            // we have dropped item or something else happened, go to next target
            if (target.GatherItemDrop == null)
            {
                // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndHandleGather({target.GetHashCode()}, target.GatherItemDrop == null --  piece might be lost, skip target");
                return true;
            }

            // adjust position we are traveling to, based on targets location vs drop location
            var centerLoc = (target.OnGatheritemDropReturnTrip == false) ? target.GatherItemDrop.transform.position : this.gameObject.transform.position;
            target.Location = new Vector3(centerLoc.x, SetDroneMovementHeight(centerLoc.y), centerLoc.z);

            // if we are some bit away from target location, increase speed towards max, or start decreasing towards 0.
            var distance = Vector3.Distance(_droneGameObject.transform.position, target.Location);
            if ((distance - DroneAttackRange) > _droneSpeed - 1.0f)
            {
                // AddDebugMsg($"{TurretTypeOfThisTurret}.GatherItemDrop() -- Speeding up -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
                _droneSpeed += Time.deltaTime * 3.0f;
                if (_droneSpeed > _droneMaxSpeed)
                    _droneSpeed = _droneMaxSpeed;
            }
            else
            {
                // AddDebugMsg($"{TurretTypeOfThisTurret}.GatherItemDrop() -- Slowing down -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
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

            // if we are within distance to target, enable doing the action needed
            var newDistance = Vector3.Distance(_droneGameObject.transform.position, target.Location);

            if (newDistance < (target.OnGatheritemDropReturnTrip == false ? DroneAttackRange : DroneAttackRange * 1.2f))
            {
                UpdateDroneMode(TurretPatrolType.AttackTarget);
            }
            else
            {
                UpdateDroneMode(TurretPatrolType.ScanTarget);
            }

            // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndHandleGather({target.GetHashCode()}, {target.GatherItemDrop.gameObject.name}, returnTrip: {target.OnGatheritemDropReturnTrip}) -- distance: {newDistance} ({(newDistance / Range)}, {_droneMode}), speed: {_droneSpeed} ({(_droneSpeed * Time.deltaTime)}), new location: {_droneGameObject.transform.position}, target location: {target.Location} -- TTL: {target.TimeToLive}");

            if (_droneTarget.OnGatheritemDropReturnTrip)
            {
                SyncItemToDrone(false, false);

                // we are on return-leg, keep it up until done
                return false;
            }

            // item is not pickable, maybe because the item is already picked up?
            if (_droneTarget.GatherItemDrop.CanPickup(false) == false)
            {
                // the item may have been picked up already or swooped up by some mod in put in container etc - skip logging it out now
                // AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndHandleGather - Target {_droneTarget.GatherItemDrop.name} is NOT Pickupable");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            // if we are out of time, return that we are finished
            if (_droneTarget.TimeToLive <= 0.0f)
            {
                AddDebugMsg($"{TurretTypeOfThisTurret}.MoveAndHandleGather - Pickup aborted - we ran out of time on Target {_droneTarget.GatherItemDrop.name}");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            return false;
        }

        override protected void FindNewTarget()
        {
            // AddDebugMsg("GatherDroneTurret.FindNewTarget()");

            float rangeCompareValue = float.MaxValue;

            var newTargetList = new List<Target>();

            List<ItemDrop> droppeditemList = ItemDrop.s_instances;

            // AddDebugMsg($"Target search: ItemDrops Found: {droppeditemList.Count}");

            // set up exclusion-list
            var exclusionList = _container.GetInventory().m_inventory;

            //AddLogInfo($"exclusionList: {exclusionList.Count} >> {string.Join("][", exclusionList)}"); // m_shared.m_name

            //AddLogInfo($"exclusionList: {exclusionList.Count} >>");
            //foreach (var exclitem in exclusionList)
            //{
            //    AddLogInfo($"name> {exclitem.m_shared.m_name}");
            //    AddLogInfo($"Pos> {exclitem.m_gridPos}");
            //    AddLogInfo("---");
            //}

            foreach (ItemDrop item in droppeditemList)
            {
                // evaluate if this target is ok, else continue to next

                if (item == null)
                    continue;

                // not enabled, so not really there in the world
                if (item.enabled == false)
                {
                    AddDebugMsg($"itemDrop, Target search: Item {item.name} is not enabled -- skipping");
                    continue;
                }

                if (item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Fish)
                {
                    // AddDebugMsg($"itemDrop, Target search: Item {item.name} is detected as a fish -- skipping");
                    continue;
                }

                if (IsItemInExclusionList(item, exclusionList))
                {
                    // AddDebugMsg($"itemDrop, Target search: Item {item.m_itemData.m_shared.m_name} is in exclusionlist -- skipping");
                    continue;
                }

                // get range to char, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToItemDrop(item);

                // AddDebugMsg($"ItemDrop, Target search: Validating: {item.name}, range: {rangeToPiece}");

                // if more than range, don't care about it at all, skip
                if (rangeToPiece >= Range)
                    continue;

                // if item is close than minimum range, then ignore fully
                if (rangeToPiece <= MinimumRange)
                    continue;

                // make sure this target is NOT in the one being actively targeted by the gather drone already
                if (_droneTarget != null)
                {
                    if (_droneTarget.GatherItemDrop == item)
                        continue;
                }

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    rangeCompareValue = AddItemToTargetList(newTargetList, item, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // adjust for target-mode set back to BEST target
            if (newTargetList.Count > 0)
            {
                rangeCompareValue = newTargetList[0].DistanceRating;
            }

            // set new list of targets and new (BEST) range compare, to set MODE
            SetTargets(newTargetList, rangeCompareValue);

            // inject higher time-spread of finding new items
            _updateTargetTimer += 5.0f;
        }

        private bool IsItemInExclusionList(ItemDrop item, List<ItemDrop.ItemData> exclusionList)
        {
            foreach (var exclItem in exclusionList)
            {
                if (item.m_itemData.m_shared.m_name == exclItem.m_shared.m_name)
                {
                    return true;
                }
            }

            return false;
        }

        protected float GetRangeToItemDrop(ItemDrop itemDrop)
        {
            return Vector3.Distance(itemDrop.transform.position, _turretAimPoint.transform.position);
        }

        protected float AddItemToTargetList(List<Target> newTargetList, ItemDrop item, float distanceRating, int maxTargets)
        {
            // AddDebugMsg($"AddItemToTargetList({newTargetList.Count}, {item.name}, {distanceRating}, {maxTargets})");
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
                    newTargetList.Add(CreateGatherTarget(item, distanceRating));

                    lowestDistance = distanceRating;
                }

                // AddDebugMsg($"return {lowestDistance}");
                return lowestDistance;
            }

            // AddDebugMsg($"We should insert the target at set pos, remove any target that goes beyond max, ({indexToInsertTargetAt}, {wearAndTear.name}, {distanceRating})");

            // we should insert the target at set pos, remove any target that goes beyond max
            newTargetList.Insert(indexToInsertTargetAt, CreateGatherTarget(item, distanceRating));

            // AddDebugMsg($"if (newTargetList.Count > maxTargets) : if ({newTargetList.Count} > {maxTargets})");
            if (newTargetList.Count > maxTargets)
            {
                newTargetList.RemoveAt(maxTargets);

                lowestDistance = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            // AddDebugMsg($"return {lowestDistance}");
            return lowestDistance;
        }

        protected Target CreateGatherTarget(ItemDrop item, float distanceRating)
        {
            return new Target()
            {
                Character = null,
                GatherItemDrop = item,
                IsGatherItemOrder = true,
                Location = item.gameObject.transform.position,
                DistanceRating = distanceRating,
            };
        }

        override protected bool UpdateAimInfoForCurrentTarget()
        {
            if (_droneTarget == null)
                return false;

            if (_droneTarget.GatherItemDrop == null)
            {
                return false;
            }

            if (_droneTarget.GatherItemDrop.gameObject == null)
            {
                return false;
            }

            var result = HelperLib.UpdateAimInfoForCurrentItemDropTarget(_droneTarget, _droneAimPoint, _droneAimResult, _aimResultTempCalcHolder);

            // AddDebugMsg($"GatherDroneTurret.UpdateAimInfoForCurrentItemDropTarget({_droneTarget.GatherItemDrop.gameObject.name}) -- {result}");

            return result;
        }

        // gather OR drop attempt
        override protected void LaunchProjectileAndRegisterHitPosition(Quaternion directionQuat, Vector3 barrelEdgePosition)
        {
            // AddDebugMsg($"GatherDroneTurret.LaunchProjectileAndRegisterHitPosition: Attempt to grab/drop item (ReturnLeg: {_droneTarget.OnGatheritemDropReturnTrip})");

            // if item is within range and available, grab it, put it at position under drone, send effect-pos of where item Was.
            if (_droneTarget.OnGatheritemDropReturnTrip == false)
            {
                var itemPosition = _droneTarget.GatherItemDrop.transform.position;

                _droneTarget.OnGatheritemDropReturnTrip = true;
                SyncItemToDrone(true, false);

                // tell other players the launch occur, what barrel to use and impact info, to draw effects...
                _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchDroneCannonProjectile", itemPosition, Vector3.forward);

                return;
            }

            // return trip

            // drop item by forcing de-sync against it
            SyncItemToDrone(false, true);
            
            _droneTarget.GatherItemDrop = null;
        }

        protected void SyncItemToDrone(bool turnOffUsageOfGravity, bool turnOnUsageOfGravity)
        {
            // _droneTarget.GatherItemDrop.transform.position = _droneCarryPoint.transform.position;
            // _droneTarget.GatherItemDrop.transform.rotation = _droneCarryPoint.transform.rotation;

            var rigidBodyDrop = _droneTarget.GatherItemDrop.gameObject.GetComponent<Rigidbody>();
            if (rigidBodyDrop != null)
            {
                rigidBodyDrop.MovePosition(_droneCarryPoint.transform.position);
                rigidBodyDrop.MoveRotation(_droneCarryPoint.transform.rotation);
                rigidBodyDrop.velocity = Vector3.zero;

                if (turnOffUsageOfGravity)
                    rigidBodyDrop.useGravity = false;

                if (turnOnUsageOfGravity)
                    rigidBodyDrop.useGravity = true;
            }
        }

    }
}
