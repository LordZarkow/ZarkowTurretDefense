using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class MissileTurret : TurretBase
    {
        private GameObject _missilePrefabGameObject;
        private GameObject _missileExplosionEffectPrefab;
        private GameObject _missileExplosionEffectLightPrefab;

        private GameObject _missileTrailGameObject;

        private readonly List<Missile> _missileList = new List<Missile>();

        override protected void RegisterRemoteProcedureCalls()
        {
            _zNetView.Register<int>("ZTD_LaunchMissile", RPC_LaunchMissile);
            _zNetView.Register<int, Vector3, Vector3>("ZTD_DetonateMissile", RPC_DetonateMissile);
        }

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"MissileTurret.GetSpecialBodyPartsOfTurret()");

            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            _missilePrefabGameObject = HelperLib.GetChildGameObject(gameObject, "MissilePrefab");
            _missileExplosionEffectPrefab = HelperLib.GetChildGameObject(gameObject, "MissileImpactEffect");
            _missileExplosionEffectLightPrefab = HelperLib.GetChildGameObject(gameObject, "MissileImpactEffectLight");

            // roll own trails later
            _missileTrailGameObject = HelperLib.GetChildGameObject(gameObject, "Trail");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("MissileTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 120f;
            _rotationDefinitions.MaxRotationHorizontalRight = 120f;
            _rotationDefinitions.MaxRotationVerticalUp = 60.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 30.0f;
            _rotationDefinitions.RotationSpeed = 100.0f;

            _rotationDefinitions.AllowedAimDeviance = 40.0f;

            // set up missiles based on barrels
            // AddDebugMsg($"MissileTurret.SetUpTurretLimits() - Pre-cache Missile ({_barrelList.Count})");
            foreach (var barrel in _barrelList)
            {
                var missile = new Missile()
                {
                    Id = barrel.Id,
                    MissileGameObject = Instantiate(_missilePrefabGameObject, this.transform.root),
                };
                missile.MissileGameObject.SetActive(false);

                _missileList.Add(missile);
            }
        }

        override protected void SyncTurretNetData()
        {
            if (_netDataObjectHandler.HasDataToRead == false)
                return;

            base.SyncTurretNetData();

            // sync all missiles, if they are active
            foreach (var missile in _missileList)
            {
                if (missile.ActiveInFlight == false || missile.ActiveInGame == false)
                    continue;

                // grab data and push to missile
                missile.MissileGameObject.transform.position = _netDataObjectHandler.Data.GetVec3($"missile_{missile.Id}.position", Vector3.zero);
                missile.MissileGameObject.transform.rotation = _netDataObjectHandler.Data.GetQuaternion($"missile_{missile.Id}.rotation", Quaternion.identity);
            }
        }

        override protected void TriggerTurretFiring()
        {
            // AddDebugMsg($"MissileTurret.TriggerTurretFiring()");

            // safety - if turret-setup has crapped out
            if (_barrelList == null)
            {
                AddWarningMsg("MissileTurret.TriggerTurretFiring() - _barrelList == null");
                return;
            }

            if (_nextBarrelIdToUse > _barrelList.Count)
            {
                AddWarningMsg($"MissileTurret.TriggerTurretFiring() - if (_nextBarrelIdToUse > _barrelList.Count) : if ({_nextBarrelIdToUse} > {_barrelList.Count})");
                return;
            }

            // grab missile from first barrel index and forward, using ammo count remain to get id to use, and after we launched last one, initiate reload action
            if (AmmoCount > 0)
            {
                // we have no more ammo in gun, safety, we should never be able to fire a gun without ammo, is actually handled in base class, but checked here for completeness
                if (_ammoInGun == 0)
                {
                    return;
                }

                _nextBarrelIdToUse = AmmoCount - _ammoInGun;
            }

            if (_missileList.Count > 1)
            {
                _nextShootDelayTimer += FireInterval;

                // safety -- maximum bad value is 'half fire interval away'
                if (_nextShootDelayTimer < (FireInterval / 2.0f))
                    _nextShootDelayTimer = (FireInterval / 2.0f);
            }
            else
            {
                _nextShootDelayTimer = Single.MaxValue; // we block until detonation, then add time
            }

            // AddDebugMsg($"TurretBase.TriggerTurretFiring() - Fire, Barrel: {_nextBarrelIdToUse}");

            // set up missile, show it, init it to fly
            var missile = _missileList[_nextBarrelIdToUse];

            // SAFETY - if missile is still in flight etc, skip firing
            if (missile.ActiveInGame && missile.ActiveInFlight)
            {
                return;
            }

            missile.ActiveInFlight = true;
            missile.TimeLivedLeft = 16.0f; // default burn-out time

            // notify other players that the effect for launch should be played
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchMissile", _nextBarrelIdToUse);

            // the picking of targets is handled via the move-function of missiles themselves
        }

        override protected void ControlAndMoveMissilesOrProjectilesOrDrones()
        {
            // AddDebugMsg($"MissileTurret.ControlAndMoveMissilesOrProjectiles() - {_missileList.Count} missiles");

            foreach (var missile in _missileList)
            {
                ControlAndMoveMissile(missile);
            }
        }

        // parameter of which missile this is in regards to
        private void ControlAndMoveMissile(Missile missile)
        {
            // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id}");

            if (missile.ActiveInFlight == false || missile.ActiveInGame == false)
            {
                return;
            }

            // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} is Active and In Game, (TimeLivedLeft: {missile.TimeLivedLeft}, Targeting Active: {missile.TargetingActive})");

            // count of targeting life time on missile
            missile.TimeLivedLeft -= Time.deltaTime;

            // if this missile has a target set, make sure it is not dead -- else we need to remove target and ask for new available from list
            if (missile.Target != null)
            {
                // we have target - do some cleaning up if target is dead etc

                // if we are NOT targeting location, let's check if char is alive, else assign temp target for last calc pos
                if (missile.Target.IsMoveOrder == false)
                {
                    // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} is targeting CHAR ({missile.Target.Character.m_name} [{missile.Target.Character.GetInstanceID()}], {missile.Target.Character.GetHealthPercentage()} % hp, Dead: {missile.Target.Character.IsDead()})");

                    if (missile.Target.Character.GetHealth() <= 0.0f || missile.Target.Character.GetInstanceID() == 0)
                    {
                        // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} - TARGET CHAR is DEAD - we set temp target");

                        // assign LOCATION of this monster as target position for 0.5-1.0 seconds
                        var newTarget = new Target()
                        {
                            //add random diff in x,y,z and random TTL, to get some dispersion into the clouds of missiles
                            Location = missile.Target.Location,
                            IsMoveOrder = true,
                            TimeToLive = 0.25f + (Random.value * 0.50f),
                        };

                        missile.DeRegisterMissileAndRemoveTarget();

                        missile.RegisterMissileAndAddTarget(newTarget);

                        // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} - NEW TARGET, based on char target that died. Loc: {newTarget.Location}, TTL: {newTarget.TimeToLive}");
                    }
                }
                else // we are temp-targeting location, so count down timer to pick new target
                {
                    // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} is targeting LOCATION ({missile.Target.Location}, {missile.Target.TimeToLive})");

                    // targeting location, if this is a temp-target see if we time out that and should look for a new target now
                    if (missile.Target.TimeToLive > 0.0f)
                    {
                        missile.Target.TimeToLive -= Time.deltaTime;

                        if (missile.Target.TimeToLive <= 0.0f)
                        {
                            missile.DeRegisterMissileAndRemoveTarget();

                            // AddDebugMsg($"MissileTurret.ControlAndMoveMissile: missile_{missile.Id} - location targeting out of time, removing Target");
                        }
                    }
                }
            }

            // try to grab new target
            if (missile.Target == null && missile.TargetingActive)
            {
                // AddDebugMsg($"MissileTurret.ControlAndMoveMissile(missile_{missile.Id}) - target is null, grab new target");
                
                GetTargetForMissile(missile);

                // if we cannot find a target, set a dummy target? That may be valid for a second at least...
                if (missile.Target == null)
                {
                    // AddDebugMsg($"MissileTurret.ControlAndMoveMissile(missile_{missile.Id}) - NO VALID TARGETS FOUND, set patrol target");

                    var randomXSum = missile.MissileGameObject.transform.position.x + (Random.value * 10.0f) - 5.0f;
                    var randomYSum = missile.MissileGameObject.transform.position.y + (Random.value * 4.0f) + 6.0f;
                    var randomZSum = missile.MissileGameObject.transform.position.z + (Random.value * 10.0f) - 5.0f;

                    var newTarget = new Target()
                    {
                        //add random diff in x,y,z and random TTL, to get some dispersion into the clouds of missiles
                        Location = new Vector3(randomXSum, randomYSum, randomZSum),
                        IsMoveOrder = true,
                        TimeToLive = 0.5f + (Random.value + 2.0f),
                    };

                    missile.RegisterMissileAndAddTarget(newTarget);
                }
            }

            // AddDebugMsg($"MissileTurret.ControlAndMoveMissile(missile_{missile.Id}) - Is Targeting: {((missile.Target != null && missile.Target.Character != null && missile.Target.Character.IsDead() == false) ? "Yes" : "No")}");

            // if this projectile is GUIDED, then adjust direction in small increments towards the possibly moving targets position
            if (missile.Target != null)
            {
                Vector3 aimPoint = missile.Target.Location;
                bool targetFlying = true;

                // if we are actually targeting a character, it will have deviated from the stored location, so update the aimpoint to point towards the predicted position for intercept
                if (missile.Target.Character != null && missile.Target.Character.GetHealth() > 0.0f)
                {
                    var targetPosition = missile.Target.Character.GetCenterPoint();

                    aimPoint = MathHelper.FirstOrderIntercept(missile.MissileGameObject.transform.position, Vector3.zero, 0.0f,
                        targetPosition, missile.Target.Character.m_currentVel);

                    // hold for ref too
                    missile.Target.Location = aimPoint;

                    targetFlying = missile.Target.Character.IsFlying();
                }

                // if more than 20m from target, set x of target aimpoint 12 higher up
                // NOTE: If target is FLYING, we are happy to attack straight at, no need to go above
                var distanceToTarget = Vector3.Distance(missile.MissileGameObject.transform.position, aimPoint);
                if (distanceToTarget >= 20.0f && targetFlying == false)
                {
                    aimPoint.y += 12.0f;
                }

                Vector3 targetDirection = aimPoint - missile.MissileGameObject.transform.position;

                Vector3 newDirection = Vector3.RotateTowards(missile.MissileGameObject.transform.forward, targetDirection, MissileTurnRate * Time.deltaTime, 0.0f);
                // Debug.DrawRay(transform.position, newDirection, Color.red, 10.0f);

                missile.MissileGameObject.transform.rotation = Quaternion.LookRotation(newDirection);

                // AddDebugMsg($"MissileTurret.ControlAndMoveMissile(missile_{missile.Id}) - Target: {missile.Target.Character} [{missile.Target.Character.GetInstanceID()}], Loc: {targetPosition}. Direction: {targetDirection}");
            } // if guided end

            var oldPos = missile.MissileGameObject.transform.position;

            // move projectile according to properties
            missile.MissileGameObject.transform.position += missile.MissileGameObject.transform.forward * MissileVelocity * Time.deltaTime;

            // cast ray in the track, see if we hit anything
            var maxDistanceRay = Vector3.Distance(oldPos, missile.MissileGameObject.transform.position);

            Ray ray = new Ray(oldPos, missile.MissileGameObject.transform.forward);
            if (Physics.Raycast(ray, out var impactInfo, maxDistanceRay, _rayMaskSolids))
            {
                // adjust missile to position of impact
                missile.MissileGameObject.transform.position = impactInfo.point;
                SetDataToDataObject(missile);

                // impact hitData prep
                var hitData = _hitData.Clone();
                hitData.m_hitCollider = impactInfo.collider;
                hitData.m_point = impactInfo.point;
                hitData.m_dir = missile.MissileGameObject.transform.rotation.eulerAngles; // direction of missile, could be face of col mesh hit or whatever

                // check what we hit -- if it is a character, push damage to it
                var charHit = impactInfo.collider.transform.root.gameObject.GetComponent<Character>();

                // notice: damage here is for impact only, not detonation. That is handled below.
                if (charHit != null)
                    ApplyDamageToCharacterHit(charHit, hitData);

                // we hit something, so we should detonate the missile
                DetonateMissile(missile, new Vector3(impactInfo.point.x, impactInfo.point.y + 1.0f, impactInfo.point.z));
                return;
            }

            // set info of missile info to net
            SetDataToDataObject(missile);

            // if missile is more than x meters from turret, then detonate it, we are losing cable-control of it
            var distanceFromTurret = Vector3.Distance(transform.position, missile.MissileGameObject.transform.position);
            if (distanceFromTurret > (Range * 1.5f))
            {
                DetonateMissile(missile, missile.MissileGameObject.transform.position);
            }
        }

        private void SetDataToDataObject(Missile missile)
        {
            _netDataObjectHandler.Data.Set($"missile_{missile.Id}.position", missile.MissileGameObject.transform.position);
            _netDataObjectHandler.Data.Set($"missile_{missile.Id}.rotation", missile.MissileGameObject.transform.rotation);
        }

        private void GetTargetForMissile(Missile missile)
        {
            // AddDebugMsg($"MissileTurret.GetTargetForMissile(missile_{missile.Id}) - known targets: {_targetList.Count}");

            // safety, if we know of no targets, don't even try this
            if (_targetList.Count == 0)
                return;

            // walk targets and look at the target-info -- is anyone NOT targeted (0 linked in target back to missile)? Next turn: Is anyone only having 1 missiles targeting it? and so on.
            // register this missile in target as targeting it. Set target as Target in missile.

            int numberOfTrackingMissiles = 0;
            Target targetFound;
            do
            {
                targetFound = GetTargetWithDefinedTrackingMissiles(numberOfTrackingMissiles);

                // next turn, try one higher count of already existing trackers, to find first free target to over-target on
                numberOfTrackingMissiles++;

                // safety, if deeper than missiles level, we are probably over-tracking Plyer only, skip out, we found nothing
                if (numberOfTrackingMissiles > AmmoCount + 2)
                    return;

            } while (targetFound == null);

            // register missile in target, register target in missile
            missile.RegisterMissileAndAddTarget(targetFound);

            // AddDebugMsg($"MissileTurret.GetTargetWithDefinedTrackingMissiles(missile_{missile.Id}) - targetFound: {targetFound.Character} ({numberOfTrackingMissiles})");
        }

        private Target GetTargetWithDefinedTrackingMissiles(int numberOfTrackingMissiles)
        {
            // AddDebugMsg($"MissileTurret.GetTargetWithDefinedTrackingMissiles({numberOfTrackingMissiles})");

            foreach (var target in _targetList)
            {
                // never target player char
                if (target.Character.IsPlayer() == true)
                    continue;

                if (target.MissilesSharingThisTarget.Count <= numberOfTrackingMissiles)
                {
                    return target;
                }
            }

            return null;
        }

        private void DetonateMissile(Missile missile, Vector3 impactLocation)
        {
            // AddDebugMsg($"MissileTurret.DetonateMissile: id: missile_{missile.Id}, {impactLocation}");

            // flag that owner has disabled control
            missile.ActiveInFlight = false;

            // deregister all
            missile.DeRegisterMissileAndRemoveTarget();

            // Special: if we are using a single wire-guided missile, block firing until missile is destroyed
            if (_missileList.Count == 1)
            {
                _nextShootDelayTimer = FireInterval;
            }

            // tell everyone to disable the visual missile and play detonation effect
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_DetonateMissile", missile.Id, impactLocation, Vector3.up);

            // damage all in area
            DamageAreaTargets(impactLocation, _rangedHitData, false);
        }

        // --- RPC ---

        private void RPC_LaunchMissile(long sender, int missileId)
        {
            // AddDebugMsg($"MissileTurret.RPC_LaunchMissile: {missileId}");

            // safety, if we have not set up barrels, we quit out
            if (_barrelList == null)
                return;

            // if the barrel id is higher than what we have, something is wrong - skip
            if ((missileId + 1) > _barrelList.Count || (missileId + 1) >_missileList.Count)
                return;

            var barrelObject = _barrelList[missileId];

            // LAUNCH FIRE EFFECT

            // clone effect, move out into the world, easier to retain due to oversight in API for transforms
            var newEffect = Instantiate(barrelObject.LaunchEffectGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newEffect.transform.SetParent(null);
            Destroy(newEffect, 5.0f); // destroy to force cleanup and remove odd loop/replay issue
            newEffect.SetActive(true);

            // TRIGGER AUDIO from barrel

            var newSfxEffect = Instantiate(barrelObject.LaunchAudioGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newSfxEffect.transform.SetParent(null);
            Destroy(newSfxEffect, 5.0f); // destroy to force cleanup and remove odd loop/replay issue
            newSfxEffect.SetActive(true);

            // Show missile in game
            var missile = _missileList[missileId];
            missile.MissileGameObject.transform.SetPositionAndRotation(barrelObject.ThisBarrelGameObject.transform.position, barrelObject.ThisBarrelGameObject.transform.rotation);
            missile.MissileGameObject.SetActive(false);
            missile.MissileGameObject.SetActive(true);
            missile.ActiveInGame = true;

            // enable trails if exist
            if (_missileTrailGameObject != null)
            {
                var trailGameObject = Instantiate(_missileTrailGameObject, missile.MissileGameObject.transform.position, missile.MissileGameObject.transform.rotation, missile.MissileGameObject.transform);

                // set up the trail part
                var trailPart = trailGameObject.AddComponent<Trail>();
                trailPart.ParentGameObject = trailGameObject;
                trailPart.InitLocationListAtStartLocation(missile.MissileGameObject.transform.position);
                missile.Trail = trailPart;
                missile.Trail.ParentGameObject.SetActive(true);
            }
        }

        private void RPC_DetonateMissile(long sender, int missileId, Vector3 detonateLocation, Vector3 direction)
        {
            // AddDebugMsg($"MissileTurret.RPC_DetonateMissile: missile_{missile.Id}, {detonateLocation}, {direction}");

            var missile = _missileList[missileId];

            // set flag that visual missile is removed, so we can do this again
            missile.MissileGameObject.SetActive(false);
            missile.ActiveInGame = false;

            // disable trails and dispose of them
            if (missile.Trail != null)
            {
                missile.Trail.DisableTrail();
                missile.Trail.ParentGameObject.transform.SetParent(null);
                Destroy(missile.Trail.ParentGameObject, 5.0f);
                missile.Trail = null;
            }

            // play detonation effect

            var impactNormalQuat = new Quaternion();
            impactNormalQuat.SetLookRotation(direction);

            var impactSfx = Instantiate(_missileExplosionEffectPrefab, null);
            impactSfx.transform.position = detonateLocation;
            impactSfx.transform.rotation = impactNormalQuat;

            impactSfx.SetActive(false);
            impactSfx.SetActive(true);

            Destroy(impactSfx, 10.0f);

            // detonate Light effect

            var impactLightSfx = Instantiate(_missileExplosionEffectLightPrefab, null);
            impactLightSfx.transform.position = detonateLocation;
            impactLightSfx.transform.rotation = impactNormalQuat;

            impactLightSfx.SetActive(false);
            impactLightSfx.SetActive(true);

            Destroy(impactLightSfx, 0.6f); // aggressively cull light, before we add special script to animate out lights...
        }
    }
}