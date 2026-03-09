using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    class HeavyGunTurret : TurretBase
    {
        private GameObject _projectilePrefabGameObject;
        private GameObject _projectileExplosionEffectPrefab;
        private GameObject _projectileExplosionEffectLightPrefab;

        private GameObject _projectileTrailGameObject;

        private readonly List<Projectile> _projectileList = new List<Projectile>();

        override protected void RegisterRemoteProcedureCalls()
        {
            _zNetView.Register<int, int>("ZTD_LaunchHeavyProjectile", RPC_LaunchHeavyProjectile);
            _zNetView.Register<int, Vector3, Vector3, bool>("ZTD_DetonateHeavyProjectile", RPC_DetonateHeavyProjectile);
        }

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"HeavyGunTurret.GetSpecialBodyPartsOfTurret()");

            _lightYellow = HelperLib.GetChildGameObject(gameObject, "Light (Yellow)");
            _lightRed = HelperLib.GetChildGameObject(gameObject, "Light (Red)");
            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            if (ZTurretDefense.DisableTurretLight.Value)
            {
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightYellow);
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightRed);
            }

            _projectilePrefabGameObject = HelperLib.GetChildGameObject(gameObject, "ProjectilePrefab");
            _projectileExplosionEffectPrefab = HelperLib.GetChildGameObject(gameObject, "ProjectileImpactEffect");
            _projectileExplosionEffectLightPrefab = HelperLib.GetChildGameObject(gameObject, "ProjectileImpactEffectLight");

            // roll own trails later
            _projectileTrailGameObject = HelperLib.GetChildGameObject(gameObject, "Trail");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("HeavyGunTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 90f;
            _rotationDefinitions.MaxRotationHorizontalRight = 90f;
            _rotationDefinitions.MaxRotationVerticalUp = 35.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 20.0f;
            _rotationDefinitions.RotationSpeed = 35.0f;

            _rotationDefinitions.AllowedAimDeviance = 5.0f;

            // set up projectiles based on barrels
            int precacheProjectileCount = 10;
            // AddDebugMsg($"HeavyGunTurret.SetUpTurretLimits() - Pre-cache Projectiles ({precacheProjectileCount})");
            for(int i = 0; i < precacheProjectileCount; i++)
            {
                var projectile = new Projectile()
                {
                    Id = i,
                    ProjectileGameObject = Instantiate(_projectilePrefabGameObject, this.transform.root),
                };
                projectile.ProjectileGameObject.SetActive(false);

                _projectileList.Add(projectile);
            }
        }

        override protected void SwitchOwnershipHandling()
        {
            AddDebugMsg($"HeavyGunTurret.SwitchOwnershipHandling({IsOwner})");

            foreach (var projectile in _projectileList)
            {
                // force removal of projectile from world -- easier to force such a reset
                projectile.OwnerHasActiveControl = false;

                if (projectile.AnnouncedAndVisible)
                    RemoveProjectileFromWorldAndDisposeTrail(projectile);
            }
        }

        override protected void SyncTurretNetData()
        {
            if (_netDataObjectHandler.HasDataToRead == false)
                return;

            base.SyncTurretNetData();

            // sync all projectiles, if they are active
            foreach (var projectile in _projectileList)
            {
                // as far as we know this projectile is not launched, so we don't pull out info about it
                if (projectile.AnnouncedAndVisible == false)
                    continue;

                projectile.ProjectileGameObject.transform.position = _netDataObjectHandler.Data.GetVec3($"projectile_{projectile.Id}.position", Vector3.zero);
                projectile.ProjectileGameObject.transform.rotation = _netDataObjectHandler.Data.GetQuaternion($"projectile_{projectile.Id}.rotation", Quaternion.identity);
            }
        }

        override protected void TriggerTurretFiring()
        {
            // AddDebugMsg($"HeavyGunTurret.TriggerTurretFiring()");

            // safety - if turret-setup has crapped out
            if (_barrelList == null)
            {
                AddWarningMsg($"HeavyGunTurret.TriggerTurretFiring() - _barrelList == null");
                return;
            }

            if (_nextBarrelIdToUse > _barrelList.Count)
            {
                AddWarningMsg($"HeavyGunTurret.TriggerTurretFiring() - if (_nextBarrelIdToUse > _barrelList.Count) : if ({_nextBarrelIdToUse} > {_barrelList.Count})");
                return;
            }

            _nextShootDelayTimer += FireInterval;

            // safety -- maximum bad value is 'half fire interval away'
            if (_nextShootDelayTimer < (FireInterval / 2.0f))
                _nextShootDelayTimer = (FireInterval / 2.0f);

            // AddDebugMsg($"HeavyGunTurret.TriggerTurretFiring() - Fire, Barrel: {_nextBarrelIdToUse}");

            // grab projectile from list of projectiles, look from top of list for a free one
            var projectile = GetNextAvailableProjectile();
            if (projectile == null)
            {
                AddWarningMsg($"HeavyGunTurret.TriggerTurretFiring() - No projectiles available, skipping (We have {_projectileList.Count} projectiles cached)");
                // don't quit out here, allow barrel to switch below etc -- maybe show fail-launch fx in the future... (click sound)
            }

            if (projectile != null)
            {
                // projectile should be moved by owner
                projectile.OwnerHasActiveControl = true;

                // notify other players that the effect for launch should be played
                _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_LaunchHeavyProjectile", _nextBarrelIdToUse, projectile.Id);
            }

            _nextBarrelIdToUse++;
            if (_nextBarrelIdToUse >= _barrelList.Count)
            {
                _nextBarrelIdToUse = 0;
            }
        }

        private Projectile GetNextAvailableProjectile()
        {
            foreach (var projectile in _projectileList)
            {
                if (projectile.OwnerHasActiveControl == false)
                {
                    return projectile;
                }
            }

            return null;
        }

        override protected void ControlAndMoveMissilesOrProjectilesOrDrones()
        {
            // AddDebugMsg($"HeavyGunTurret.ControlAndMoveMissilesOrProjectiles() - {_projectileList.Count} projectiles");

            foreach (var projectile in _projectileList)
            {
                // owner has not launched it, so he don't move it - pre-check here for cleaner code
                if (projectile.OwnerHasActiveControl == false)
                    continue;

                ControlAndMoveProjectile(projectile);
            }
        }

        // parameter of which projectile this is in regards to
        private void ControlAndMoveProjectile(Projectile projectile)
        {
            // AddDebugMsg($"HeavyGunTurret.ControlAndMoveProjectile: {projectile.Id} is Active and In Game");

            var oldPos = projectile.ProjectileGameObject.transform.position;

            // move projectile according to properties
            projectile.ProjectileGameObject.transform.position += projectile.ProjectileGameObject.transform.forward * ProjectileVelocity * Time.deltaTime;

            // cast ray in the track, see if we hit anything
            var maxDistanceRay = Vector3.Distance(oldPos, projectile.ProjectileGameObject.transform.position);

            Ray ray = new Ray(oldPos, projectile.ProjectileGameObject.transform.forward);
            var hits = Physics.RaycastAll(ray, maxDistanceRay, _rayMaskSolids);
            if (hits.Length > 0)
            {
                // we got a list of hits, call function to handle it
                if (HandleProjectileHits(hits, projectile))
                    return;
            }

            // set data to net - based on where it was moved (full length)
            SetDataToDataObject(projectile);

            // if projectile is more than x meters from turret, then detonate it -- safety so they don't continue forever
            var distanceFromTurret = Vector3.Distance(transform.position, projectile.ProjectileGameObject.transform.position);
            if (distanceFromTurret > (Range * 5.0f)) // ex. 50 * 5.0f = 250.0f
            {
                DetonateProjectile(projectile, projectile.ProjectileGameObject.transform.position, true);
            }
        }

        // go over hits, return true of we hit something where we cannot penetrate so we are setting position to shorter than previously moved
        private bool HandleProjectileHits(RaycastHit[] hits, Projectile projectile)
        {
            // var sortedHits = SortHits(hits);

            var sortedHits = new List<RaycastHit>(hits);
            sortedHits.Sort((x, y) => x.distance.CompareTo(y.distance));

            // AddDebugMsg($"Projectile hit List: {sortedHits.Count}");

            foreach (var hit in sortedHits)
            {
                // go over every hit, if it is a collision with non-char, quit out with position set

                // check what we hit -- if it is a character, push damage to it
                var charHit = hit.collider.transform.root.gameObject.GetComponent<Character>();

                if (charHit != null)
                {
                    // impact hitData prep
                    var hitData = _hitData.Clone();
                    hitData.m_hitCollider = hit.collider;
                    hitData.m_point = hit.point;
                    hitData.m_dir = projectile.ProjectileGameObject.transform.rotation.eulerAngles;

                    // notice: damage here is for impact only, not detonation. That is handled below.
                    ApplyDamageToCharacterHit(charHit, hitData);

                    // we hit something, so we should detonate the projectile explosion - only Destroy if it is NOT a character hit
                    DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), false);

                    // AddDebugMsg($"Char hit by projectile: {charHit.m_name}, {charHit.m_boss}, {charHit.m_level}, {charHit.m_weakSpots.Length > 0}, hitPos: {hit.point} ({hit.distance})");

                    continue;
                }

                var damage = (_hitData.m_damage.m_blunt + _hitData.m_damage.m_pierce + _hitData.m_damage.m_fire) * 0.75f;
                var customHitData = _hitData.Clone();
                customHitData.m_hitCollider = hit.collider;
                customHitData.m_point = hit.point;
                customHitData.m_damage.m_blunt = 0;
                customHitData.m_damage.m_slash = 0;
                customHitData.m_damage.m_pierce = 0;
                customHitData.m_damage.m_chop = damage;
                customHitData.m_damage.m_pickaxe = damage * 0.5f;
                customHitData.m_damage.m_fire = 0;
                customHitData.m_damage.m_frost = 0;
                customHitData.m_damage.m_lightning = 0;
                customHitData.m_damage.m_poison = 0;
                customHitData.m_damage.m_spirit = 0;

                customHitData.m_toolTier = 10; // can damage all known things

                customHitData.m_pushForce = 0.0f;

                // try to see if it perhaps is a tree that was hit
                var treeHit = hit.transform.root.gameObject.GetComponent<TreeBase>();
                if (treeHit != null)
                {
                    treeHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [Tree] {treeHit.name}, maxHealth: {treeHit.m_health}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    // detonate this projectile whenever we hit anything - but this version also destroys it
                    DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), true);

                    projectile.ProjectileGameObject.transform.position = hit.point;
                    SetDataToDataObject(projectile);
                    return true;
                }

                var treeLogHit = hit.transform.root.gameObject.GetComponent<TreeLog>();
                if (treeLogHit != null)
                {
                    treeLogHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [TreeLog] {treeLogHit.name}, maxHealth: {treeLogHit.m_health}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    // detonate this projectile whenever we hit anything - but this version also destroys it
                    DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), true);

                    projectile.ProjectileGameObject.transform.position = hit.point;
                    SetDataToDataObject(projectile);
                    return true;
                }

                var destructibleHit = hit.transform.root.gameObject.GetComponent<Destructible>();
                if (destructibleHit != null)
                {
                    destructibleHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [Destructible] {destructibleHit.name}, maxHealth: {destructibleHit.m_health}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    // detonate this projectile whenever we hit anything - but this version also destroys it
                    DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), true);

                    projectile.ProjectileGameObject.transform.position = hit.point;
                    SetDataToDataObject(projectile);
                    return true;
                }

                var largeRockHit = hit.transform.root.gameObject.GetComponent<MineRock5>();
                if (largeRockHit != null)
                {
                    largeRockHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [MineRock5] {largeRockHit.name}, maxHealth: {largeRockHit.m_health}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    // detonate this projectile whenever we hit anything - but this version also destroys it
                    DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), true);

                    projectile.ProjectileGameObject.transform.position = hit.point;
                    SetDataToDataObject(projectile);
                    return true;
                }

                // detonate this projectile whenever we hit anything - but this version also destroys it
                DetonateProjectile(projectile, new Vector3(hit.point.x, hit.point.y, hit.point.z), true);

                projectile.ProjectileGameObject.transform.position = hit.point;
                SetDataToDataObject(projectile);
                return true;
            }

            return false;
        }

        private void SetDataToDataObject(Projectile projectile)
        {
            _netDataObjectHandler.Data.Set($"projectile_{projectile.Id}.position", projectile.ProjectileGameObject.transform.position);
            _netDataObjectHandler.Data.Set($"projectile_{projectile.Id}.rotation", projectile.ProjectileGameObject.transform.rotation);
        }

        private void DetonateProjectile(Projectile projectile, Vector3 impactLocation, bool destroyProjectile)
        {
             // AddDebugMsg($"HeavyGunTurret.DetonateProjectile: {impactLocation}");

            // flag that owner has disabled control
            if (destroyProjectile)
                projectile.OwnerHasActiveControl = false;

            // tell everyone to disable the visual projectile and play detonation effect
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_DetonateHeavyProjectile", projectile.Id, impactLocation, Vector3.up, destroyProjectile);

            // damage all in area
            DamageAreaTargets(impactLocation, _rangedHitData, false);
        }

        private void RPC_LaunchHeavyProjectile(long sender, int barrelId, int projectileId)
        {
            // AddDebugMsg($"HeavyGunTurret.RPC_LaunchHeavyProjectile: {barrelId}, {projectileId}");

            // safety, if we have not set up barrels, we quit out
            if (_barrelList == null)
                return;

            // if the barrel id is higher than what we have, something is wrong - skip
            if ((barrelId + 1) > _barrelList.Count)
                return;

            // LAUNCH FIRE EFFECT

            var barrelObject = _barrelList[barrelId];

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

            // Show projectile in game - from list of possible projectiles
            var projectile = GetProjectileFromId(projectileId);
            if (projectile == null)
            {
                AddWarningMsg($"HeavyGunTurret.RPC_LaunchHeavyProjectile: projectile is null, as it cannot be found from id {projectileId} -- aborting visual");
                return;
            }

            // set up projectile location and direction
            projectile.ProjectileGameObject.transform.SetPositionAndRotation(barrelObject.ThisBarrelGameObject.transform.position, barrelObject.ThisBarrelGameObject.transform.rotation);
            projectile.ProjectileGameObject.SetActive(false);
            projectile.ProjectileGameObject.SetActive(true);
            projectile.AnnouncedAndVisible = true;

            // enable trails if exist
            if (_projectileTrailGameObject != null)
            {
                var trailGameObject = Instantiate(_projectileTrailGameObject, projectile.ProjectileGameObject.transform.position, projectile.ProjectileGameObject.transform.rotation, projectile.ProjectileGameObject.transform);

                // set up the trail part
                var trailPart = trailGameObject.AddComponent<Trail>();
                trailPart.ParentGameObject = trailGameObject;
                trailPart.InitLocationListAtStartLocation(projectile.ProjectileGameObject.transform.position);
                projectile.Trail = trailPart;
                projectile.Trail.ParentGameObject.SetActive(true);
            }
        }

        private Projectile GetProjectileFromId(int id)
        {
            foreach (var projectile in _projectileList)
            {
                if (id == projectile.Id)
                    return projectile;
            }

            return null;
        }

        private void RPC_DetonateHeavyProjectile(long sender, int projectileId, Vector3 detonateLocation, Vector3 direction, bool destroyProjectile)
        {
            // AddDebugMsg($"HeavyGunTurret.RPC_DetonateHeavyProjectile: {projectileId}, {detonateLocation} (destroyProjectile: {destroyProjectile})");

            if (destroyProjectile)
            {
                var projectile = GetProjectileFromId(projectileId);

                RemoveProjectileFromWorldAndDisposeTrail(projectile);
            }

            // play detonation effect

            var impactNormalQuat = new Quaternion();
            impactNormalQuat.SetLookRotation(direction);

            var impactSfx = Instantiate(_projectileExplosionEffectPrefab, null);
            impactSfx.transform.position = detonateLocation;
            impactSfx.transform.rotation = impactNormalQuat;

            impactSfx.SetActive(false);
            impactSfx.SetActive(true);

            Destroy(impactSfx, 5.0f);

            // detonate Light effect

            var impactLightSfx = Instantiate(_projectileExplosionEffectLightPrefab, null);
            impactLightSfx.transform.position = detonateLocation;
            impactLightSfx.transform.rotation = impactNormalQuat;

            impactLightSfx.SetActive(false);
            impactLightSfx.SetActive(true);

            Destroy(impactLightSfx, 0.6f); // aggressively cull light, before we add special script to animate out lights...
        }

        private void RemoveProjectileFromWorldAndDisposeTrail(Projectile projectile)
        {
            // set flag that visual projectile is removed, so we can use this again from our mini-pool
            projectile.ProjectileGameObject.SetActive(false);
            projectile.AnnouncedAndVisible = false;

            // disable trails and dispose of them
            if (projectile.Trail != null)
            {
                projectile.Trail.DisableTrail();
                projectile.Trail.ParentGameObject.transform.SetParent(null);
                Destroy(projectile.Trail.ParentGameObject, 5.0f);
                projectile.Trail = null;
            }
        }
    }
}
