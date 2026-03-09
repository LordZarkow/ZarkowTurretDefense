using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;
    using UnityEngine.PlayerLoop;

    public class LoggerDroneTurret : DroneTurret
    {
        private GameObject _droneWarningLight;

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"LoggerDroneTurret.GetSpecialBodyPartsOfTurret()");

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

            _droneWarningLight = HelperLib.GetChildGameObject(_droneGameObject, "DroneWarningLight");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("LoggerDroneTurret.SetUpTurretLimits()");

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
            // should possibly depend on target type
            return heightIn + 5.0f;
        }

        override protected bool MoveAndHandleTreesAndLogsAndStubs(Target target)
        {
            // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs({target.GetHashCode()}, {( target.LoggerTreeBaseTarget != null ? target.LoggerTreeBaseTarget.name : target.LoggerTreeLogTarget != null ? target.LoggerTreeLogTarget.name : target.LoggerDestructibleTarget != null ? target.LoggerDestructibleTarget.name : "BUG: NO Logger Target SET")})");

            if (target.LoggerTreeBaseTarget == null && target.LoggerTreeLogTarget == null && target.LoggerDestructibleTarget == null)
            {
                // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs({target.GetHashCode()}, (target.LoggerTreeBaseTarget &&  target.LoggerTreeLogTarget && target.LoggerDestructibleTarget) == null --  tree/treelog/stub might already be destroyed, skip target");
                return true;
            }

            var centerLoc = target.LoggerTreeBaseTarget != null ? target.LoggerTreeBaseTarget.gameObject.transform.position : target.LoggerTreeLogTarget != null ? target.LoggerTreeLogTarget.gameObject.transform.position : target.LoggerDestructibleTarget.gameObject.transform.position;

            target.Location = new Vector3(centerLoc.x, SetDroneMovementHeight(centerLoc.y), centerLoc.z);

            // if we are some bit away from target location, increase speed towards max, or start decreasing towards 0.
            var distance = Vector3.Distance(_droneGameObject.transform.position, target.Location);
            if ((distance - DroneAttackRange) > _droneSpeed - 1.0f)
            {
                // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs() -- Speeding up -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
                _droneSpeed += Time.deltaTime * 3.0f;
                if (_droneSpeed > _droneMaxSpeed)
                    _droneSpeed = _droneMaxSpeed;
            }
            else
            {
                // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs() -- Slowing down -- ({(distance - DroneAttackRange)}, {_droneSpeed - 1.0f})");
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

            // if we are within distance to target, enable firing guns
            var newDistance = Vector3.Distance(_droneGameObject.transform.position, target.Location);

            if (newDistance < DroneAttackRange)
            {
                UpdateDroneMode(TurretPatrolType.AttackTarget);
            }
            else
            {
                UpdateDroneMode(TurretPatrolType.ScanTarget);
            }

            // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs({target.GetHashCode()}, {(target.LoggerTreeBaseTarget != null ? target.LoggerTreeBaseTarget.gameObject.name : target.LoggerTreeLogTarget.gameObject.name)}) -- distance: {newDistance} ({(newDistance / Range)}, {_droneMode}), speed: {_droneSpeed} ({(_droneSpeed * Time.deltaTime)}), health: {(target.LoggerTreeBaseTarget != null ? target.LoggerTreeBaseTarget.m_health : target.LoggerTreeLogTarget.m_health)}, new location: {_droneGameObject.transform.position}, target location: {target.Location} -- TTL: {target.TimeToLive}");

            var health = target.LoggerTreeBaseTarget != null
                ? target.LoggerTreeBaseTarget.m_health
                : target.LoggerTreeLogTarget != null
                    ? target.LoggerTreeLogTarget.m_health
                    : target.LoggerDestructibleTarget.m_health;

            if (health <= 0.0f)
            {
                // AddDebugMsg($"LoggerDroneTurret.MoveAndHandleTreesAndLogs - Target {(target.LoggerTreeBaseTarget != null ? target.LoggerTreeBaseTarget.gameObject.name : target.LoggerTreeLogTarget != null ? target.LoggerTreeLogTarget.gameObject.name : target.LoggerDestructibleTarget.gameObject.name)} is Destroyed");
                UpdateDroneMode(TurretPatrolType.NoTarget);
                return true;
            }

            // if we are out of time, return that we are finished -- set this to minus 10 to force 20 sec time per target, if a thick tree...
            if (target.TimeToLive <= -10.0f)
            {
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

            // TreeBase
            var allTreeBaseObjects = new List<TreeBase>(FindObjectsOfType<TreeBase>() );
            // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: allTreeBaseObjects Found: {allTreeBaseObjects.Count}");
            foreach (var tree in allTreeBaseObjects)
            {
                if (tree == null)
                    continue;

                if (tree.m_health <= 0.0f)
                {
                    continue;
                }

                // get range to tree, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToTreeOrLogOrDestructible(tree.gameObject, _turretAimPoint);

                // if more than *1.1f range, don't care about it at all, skip
                if (rangeToPiece > Range * 1.05f)
                    continue;

                // add distance to drone as a rating, to make it travel back and forth less
                rangeToPiece += GetRangeToTreeOrLogOrDestructible(tree.gameObject, _droneGameObject) * 0.5f;

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    rangeCompareValue = AddTreeOrLogOrDestructibleToTargetList(newTargetList, tree, null, null, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // TreeLog

            var allTreeLogObjects = new List<TreeLog>(FindObjectsOfType<TreeLog>());
            // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: allTreeLogObjects Found: {allTreeLogObjects.Count}");
            foreach (var treeLog in allTreeLogObjects)
            {
                if (treeLog == null)
                    continue;

                if (treeLog.m_health <= 0.0f)
                {
                    continue;
                }

                // get range to treeLog, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToTreeOrLogOrDestructible(treeLog.gameObject, _turretAimPoint);

                // if more than *1.1f range, don't care about it at all, skip
                if (rangeToPiece > Range * 1.1f)
                    continue;

                // add distance to drone as a rating, to make it travel back and forth less
                rangeToPiece += GetRangeToTreeOrLogOrDestructible(treeLog.gameObject, _droneGameObject) * 0.5f;

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    rangeCompareValue = AddTreeOrLogOrDestructibleToTargetList(newTargetList, null, treeLog, null, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // Destructible, but only Stubs!
            var allDestructibleObjects = new List<Destructible>(FindObjectsOfType<Destructible>());
            // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: allDestructibleObjects Found: {allDestructibleObjects.Count}");
            foreach (var dest in allDestructibleObjects)
            {
                if (dest == null)
                    continue;

                if (dest.m_health <= 0.0f)
                {
                    continue;
                }

                // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: Evaluate Destructible {dest.gameObject.name}");

                // since we ONLY care about TREE STUBS, grab items in tree-type and hope this is good enough?
                if (dest.m_destructibleType != DestructibleType.Tree)
                    continue;

                // if setting is that we want to ignore saplings, then look for Plant script on it -- if it has one, skip.
                var plantObject = dest.transform.root.GetComponent<Plant>();
                if (plantObject != null)
                {
                    // AddWarningMsg($"LoggerDroneTurret.FindNewTarget: Target search: Skipping Destructible {dest.gameObject.name}, as it is a Plant");
                    continue;
                }

                // get range to item, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToTreeOrLogOrDestructible(dest.gameObject, _turretAimPoint);

                // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: Evaluate Destructible {dest.gameObject.name}, Range: {rangeToPiece}");

                // if more than some range, don't care about it at all, skip
                if (rangeToPiece > Range * 1.05f)
                    continue;

                // add distance to drone as a rating, to make it travel back and forth less
                rangeToPiece += GetRangeToTreeOrLogOrDestructible(dest.gameObject, _droneGameObject) * 0.5f;

                // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: Evaluate Destructible {dest.gameObject.name}, Range: {rangeToPiece} (with Drone)");

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    // AddDebugMsg($"LoggerDroneTurret.FindNewTarget: Target search: Add {dest.gameObject.name} to target list (RTP: {rangeToPiece})");
                    rangeCompareValue = AddTreeOrLogOrDestructibleToTargetList(newTargetList, null, null, dest, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // adjust for target-mode set back to BEST target
            if (newTargetList.Count > 0)
            {
                rangeCompareValue = newTargetList[0].DistanceRating;

                var targetName = (newTargetList[0].LoggerTreeBaseTarget != null)
                    ? newTargetList[0].LoggerTreeBaseTarget.name
                    : newTargetList[0].LoggerTreeLogTarget != null
                        ? newTargetList[0].LoggerTreeLogTarget.name
                        : newTargetList[0].LoggerDestructibleTarget.name;

                // AddDebugMsg($"Target search: Target picked: {targetName}, (RV: {rangeCompareValue}) from a total of {(allTreeBaseObjects.Count + allTreeLogObjects.Count + allDestructibleObjects.Count)} - total targets held: {newTargetList.Count}");
            }

            // set new list of targets and new (BEST) range compare, to set MODE
            SetTargets(newTargetList, rangeCompareValue);

            // inject higher time-spread of finding new pieces
            _updateTargetTimer += 20.0f;
        }

        private float GetRangeToTreeOrLogOrDestructible(GameObject treeOrLogOrDestructible, GameObject compareGameObject)
        {
            return Vector3.Distance(treeOrLogOrDestructible.transform.position, compareGameObject.transform.position);
        }

        private float AddTreeOrLogOrDestructibleToTargetList(List<Target> newTargetList, TreeBase treeBase, TreeLog treeLog, Destructible destructible, float distanceRating, int maxTargets)
        {
            var worstRangeCompareValue = float.MaxValue;

            // if we already have target stored, look for last one to get value to quit out early against
            if (newTargetList.Count > 0)
            {
                worstRangeCompareValue = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            // if our value is worse than last one AND we are FULL already, skip out
            if (distanceRating >= worstRangeCompareValue && newTargetList.Count >= MaximumNumberOfTrackedTargets)
            {
                return worstRangeCompareValue;
            }

            var indexToInsertTargetAt = GetTargetListIndexBasedOnDistance(newTargetList, distanceRating);

            // index is outside elements we have
            if (indexToInsertTargetAt > newTargetList.Count - 1)
            {
                // if we have less than maximum targets, then we add at end -- else, return without having added anything
                if (newTargetList.Count < maxTargets)
                {
                    newTargetList.Add(CreateTreeAndLogOrDestructibleTarget(treeBase, treeLog, destructible, distanceRating));

                    worstRangeCompareValue = distanceRating;
                }

                return worstRangeCompareValue;
            }

            // we should insert the target at set pos, remove any target that goes beyond max count
            newTargetList.Insert(indexToInsertTargetAt, CreateTreeAndLogOrDestructibleTarget(treeBase, treeLog, destructible, distanceRating));

            if (newTargetList.Count > maxTargets)
            {
                newTargetList.RemoveAt(maxTargets);

                worstRangeCompareValue = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            return worstRangeCompareValue;
        }

        private Target CreateTreeAndLogOrDestructibleTarget(TreeBase tree, TreeLog log, Destructible destructible, float distanceRating)
        {
            GameObject vGameObject = (tree != null) ? tree.gameObject : (log != null) ? log.gameObject : destructible.gameObject;

            return new Target()
            {
                Character = null,
                IsTargetLoggerOrder = true,
                LoggerTreeBaseTarget = tree,
                LoggerTreeLogTarget = log,
                LoggerDestructibleTarget = destructible,

                // location does not seem usable, but meh
                Location = vGameObject.transform.position,
                DistanceRating = distanceRating,
            };
        }

        override protected bool UpdateAimInfoForCurrentTarget()
        {
            if (_droneTarget == null)
                return false;

            if (_droneTarget.LoggerTreeBaseTarget == null && _droneTarget.LoggerTreeLogTarget == null && _droneTarget.LoggerDestructibleTarget == null)
            {
                return false;
            }

            if (_droneTarget.LoggerTreeBaseTarget != null && _droneTarget.LoggerTreeBaseTarget.gameObject == null)
            {
                return false;
            }

            if (_droneTarget.LoggerTreeLogTarget != null && _droneTarget.LoggerTreeLogTarget.gameObject == null)
            {
                return false;
            }

            if (_droneTarget.LoggerDestructibleTarget != null && _droneTarget.LoggerDestructibleTarget.gameObject == null)
            {
                return false;
            }

            var result = HelperLib.UpdateAimInfoForCurrentTreeTarget(_droneTarget, _droneAimPoint, _droneAimResult, _aimResultTempCalcHolder);

            return result;
        }

        // cut tree
        override protected void LaunchProjectileAndRegisterHitPosition(Quaternion directionQuat, Vector3 barrelEdgePosition)
        {
            // AddDebugMsg($"LoggerDroneTurret.LaunchProjectileAndRegisterHitPosition: Cast Ray: {directionQuat} from {barrelEdgePosition}");

            // var ray = new Ray(barrelEdgePosition, directionQuat * Vector3.forward);

            //if (Physics.Raycast(ray, out var impactInfo, Range * 2.0f, _rayMaskRepairDroneSolids) == false)
            //    return;

            // var hitArray = Physics.RaycastAll(ray, 4.1f, _rayMaskRepairDroneSolids);

            var ray = new Ray(barrelEdgePosition, directionQuat * Vector3.forward);
            if (Physics.Raycast(ray, out var impactInfo, Range * 2.0f, _rayMaskSolids) == false)
                return;

            // safety, shouldn't happen
            if (_hitData == null)
            {
                AddWarningMsg("LoggerDroneTurret.LaunchProjectileAndShowHitPosition() - _hitData is null");
                return;
            }

            var treeBaseHit = impactInfo.collider.transform.root.gameObject.GetComponent<TreeBase>();
            var treeLogHit = (treeBaseHit == null) ? impactInfo.collider.transform.root.gameObject.GetComponent<TreeLog>() : null;
            var destructibleHit = (treeLogHit == null) ? impactInfo.collider.transform.root.GetComponent<Destructible>() : null;

            if (treeBaseHit != null || treeLogHit != null || (destructibleHit != null && destructibleHit.m_destructibleType == DestructibleType.Tree))
            {
                var health = treeBaseHit != null ? treeBaseHit.m_health : treeLogHit != null ? treeLogHit.m_health : destructibleHit.m_health;
                if (health > 0.0f)
                {
                    // normal attack
                    var hitData = _hitData.Clone();
                    hitData.m_hitCollider = impactInfo.collider;
                    hitData.m_point = impactInfo.point;
                    hitData.m_dir = directionQuat.eulerAngles;

                    // make sure we do not get "Too Hard" message of the tool being too weak
                    hitData.m_toolTier = 10;

                    // reduce force given to trees
                    hitData.m_pushForce = 0.0f;
                    hitData.m_staggerMultiplier = 0.0f;

                    if (treeBaseHit != null)
                    {
                        treeBaseHit.Damage(hitData);
                    }
                    else if (treeLogHit != null)
                    {
                        treeLogHit.Damage(hitData);
                    }
                    else
                    {
                        destructibleHit.Damage(hitData);
                    }
                }
            }
            else
            {
                // fallback, see if the position we hit can run a detonation of sorts and to a sphere up to 1m with 0.5f in damage?
                // AddLogMsg($"LaunchProjectileAndShowHitPosition() - did not hit any target we cared about -> {impactInfo.transform.root.name}");

                var hits = Physics.OverlapSphere(impactInfo.point, 1.0f, _rayMaskTreesAndRock);

                // AddDebugMsg($"LaunchProjectileAndShowHitPosition(): DamageAreaTargets: {hits.Length}");

                // check if this is a valid target we care about
                foreach (var hit in hits)
                {
                    // normal attack
                    var minorHitData = _hitData.Clone();
                    minorHitData.m_hitCollider = hit;
                    minorHitData.m_point = (hit.providesContacts) ? hit.ClosestPoint(impactInfo.point) : hit.ClosestPointOnBounds(impactInfo.point);
                    minorHitData.m_dir = directionQuat.eulerAngles;

                    minorHitData.m_damage.m_chop *= 0.5f;
                    minorHitData.m_damage.m_pickaxe *= 0.5f;

                    // make sure we do not get "Too Hard" message of the tool being too weak
                    minorHitData.m_toolTier = 10;

                    // reduce force given to trees
                    minorHitData.m_pushForce = 0.0f;
                    minorHitData.m_staggerMultiplier = 0.0f;

                    var smallTreeOrStubHit = hit.transform.root.gameObject.GetComponent<Destructible>();
                    if (smallTreeOrStubHit != null)
                    {
                        smallTreeOrStubHit.Damage(minorHitData);

                        // AddDebugMsg($"LoggerDroneTurret.LaunchProjectileAndShowHitPosition(): DamageAreaTargets: [Destructible], Hit: {smallTreeOrStubHit.name}, chopDamage: {minorHitData.m_damage.m_chop},  pickaxeDamage: {minorHitData.m_damage.m_pickaxe}");

                        continue;
                    }

                    var largeRockHit = hit.transform.root.gameObject.GetComponent<MineRock5>();
                    if (largeRockHit != null)
                    {
                        largeRockHit.Damage(minorHitData);

                        // AddWarningMsg($"LoggerDroneTurret.LaunchProjectileAndShowHitPosition(): DamageAreaTargets: [MineRock5] {largeRockHit.name}, maxHealth: {largeRockHit.m_health}, chopDamage: {minorHitData.m_damage.m_chop},  pickaxeDamage: {minorHitData.m_damage.m_pickaxe}");

                        continue;
                    }
                }

            }

            // tell other players the launch occur, what barrel to use and impact info, to draw effects...
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchDroneCannonProjectile", impactInfo.point, impactInfo.normal);
        }

        void FixedUpdate()
        {
            _droneWarningLight.transform.Rotate(0.0f, 5.0f, 0.0f);
        }

    }
}
