using System;
using System.Collections;
using System.Collections.Generic;
using ZarkowTurretDefense;
using ZarkowTurretDefense.Models;
using UnityEngine;
using ZarkowTurretDefense.Scripts;

namespace ZarkowTurretDefense.Scripts
{
    using System.Linq;
    using Random = UnityEngine.Random;

    public class TurretBase : MonoBehaviour
    {
        // default values
        public float Range = 20f;
        public float MinimumRange = 0.0f;

        public float DroneAttackRange = 0.0f;

        public float FireInterval = 1.0f;
        public float ReloadTime = 0.0f;
        public int AmmoCount = 0;

        public int MaximumNumberOfTrackedTargets;

        public bool MustHaveLineOfSightToTrack;
        public bool IgnorePlayerForTargeting;
        public bool OnlyPlayerForTargeting;

        public bool IgnoreThatTargetIsBehindCover;

        public float MissileTurnRate;
        public float MissileVelocity;

        public float ProjectileVelocity;

        public float Damage = 0f;
        public float BluntDamage = 0f;
        public float SlashDamage = 0f;
        public float PierceDamage = 0f;
        public float ChopDamage = 0f;
        public float PickaxeDamage = 0f;
        public float FireDamage = 0f;
        public float FrostDamage = 0f;
        public float LightningDamage = 0f;
        public float PoisonDamage = 0f;
        public float SpiritDamage = 0f;

        public float DamageRadius = 0f;

        public float RangedDamage = 0f;
        public float RangedBluntDamage = 0f;
        public float RangedSlashDamage = 0f;
        public float RangedPierceDamage = 0f;
        public float RangedChopDamage = 0f;
        public float RangedPickaxeDamage = 0f;
        public float RangedFireDamage = 0f;
        public float RangedFrostDamage = 0f;
        public float RangedLightningDamage = 0f;
        public float RangedPoisonDamage = 0f;
        public float RangedSpiritDamage = 0f;

        public bool UniformDamageThroughDamageRadius;

        public bool HealingTarget;
        public float HealingAmount;

        public bool RepairTarget;

        public bool PatrolCloseToTurret;

        public TurretType TurretTypeOfThisTurret = TurretType.Gun;

        public enum TurretPatrolType
        {
            NoTarget,
            ScanTarget,
            AttackTarget,
        }

        protected HitData _hitData;
        protected HitData _rangedHitData;

        private readonly float _targetUpdateInterval = 0.40f;

        private readonly int _viewBlockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "terrain", "viewblock", "vehicle");

        private readonly int _viewBlockMaskPlayersCharactersOnly = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv");

        protected readonly int _rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece",
            "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
            "vehicle");

        protected readonly int _rayMaskTrees = LayerMask.GetMask("Default", "Default_small");

        protected readonly int _rayMaskTreesAndRock = LayerMask.GetMask("Default", "Default_small", "static_solid");

        protected readonly int _rayMaskCharacterDamageArea = LayerMask.GetMask("character", "character_noenv");

        protected readonly int _rayMaskExplosionArea = LayerMask.GetMask("character", "character_noenv", "Default", "Default_small", "static_solid");


        protected List<Target> _targetList = new List<Target>();
        protected float _updateTargetTimer;
        protected float _nextShootDelayTimer = 0.0f;

        protected float _reloadGunTimer = 0.0f;
        protected int _ammoInGun = 0;

        protected List<Barrel> _barrelList = new List<Barrel>();
        protected int _nextBarrelIdToUse;

        private AudioSource[] _audioSources;

        // private GameObject _turretBase;
        protected GameObject _turretTurn;
        protected GameObject _turretTilt;
        protected GameObject _turretAimPoint;

        protected TurretPatrolType _turretMode = TurretPatrolType.NoTarget;
        protected GameObject _lightYellow;
        protected GameObject _lightRed;
        protected GameObject _lightLaser;

        protected GameObject _dirtImpactPrefab;

        protected DegreesSpecifier _aimResult; // holding deg data from calc
        protected DegreesSpecifier _aimResultTempCalcHolder; // holding deg data from calc, for all temp cals

        // limitations
        protected RotationDefinitions _rotationDefinitions;

        protected RotationData _rotationData = new RotationData();

        protected ZNetView _zNetView;
        protected ZDO _zDataObject;
        protected NetDataObjectHandler _netDataObjectHandler;

        protected bool _wasOwnerLastFrame = false;

        public bool IsOwner
        {
            get
            {
                if (_zNetView == null)
                {
                    return false;
                }

                return _zNetView.IsOwner();
            }
        }

        void Awake()
        {
            // AddDebugMsg($"{TurretTypeOfThisTurret}.Awake()");

            _audioSources = GetComponentsInChildren<AudioSource>(true);

            // AddDebugMsg($"Number of AudioSources found: {_audioSources.Length}");

            foreach (var audioSource in _audioSources)
            {
                audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
            }

            SetVolume();
            ZTurretDefense.TurretVolume.SettingChanged += SetVolume;

            // AddDebugMsg("Register ZNetView");
            _zNetView = GetComponent<ZNetView>();
            _zDataObject = _zNetView.GetZDO();
            _netDataObjectHandler = new NetDataObjectHandler(_zDataObject);

            RegisterRemoteProcedureCalls();

            // set a re-usable HitData for damage etc
            _hitData = new HitData
            {
                m_damage = new HitData.DamageTypes
                {
                    m_damage = Damage * ZTurretDefense.DamageModifier.Value,
                    m_blunt = BluntDamage * ZTurretDefense.DamageModifier.Value,
                    m_slash = SlashDamage * ZTurretDefense.DamageModifier.Value,
                    m_pierce = PierceDamage * ZTurretDefense.DamageModifier.Value,
                    m_chop = ChopDamage * ZTurretDefense.DamageModifier.Value,
                    m_pickaxe = PickaxeDamage * ZTurretDefense.DamageModifier.Value,
                    m_fire = FireDamage * ZTurretDefense.DamageModifier.Value,
                    m_frost = FrostDamage * ZTurretDefense.DamageModifier.Value,
                    m_lightning = LightningDamage * ZTurretDefense.DamageModifier.Value,
                    m_poison = PoisonDamage * ZTurretDefense.DamageModifier.Value,
                    m_spirit = SpiritDamage * ZTurretDefense.DamageModifier.Value,
                },
                m_ranged = true,
                m_staggerMultiplier = 1.2f,
            };

            //HelperLib.PrintAllReflectionOfObject(_hitData.m_damage);

            if (DamageRadius > 0.0f)
            {
                _rangedHitData = new HitData
                {
                    m_damage = new HitData.DamageTypes
                    {
                        m_damage = RangedDamage * ZTurretDefense.DamageModifier.Value,
                        m_blunt = RangedBluntDamage * ZTurretDefense.DamageModifier.Value,
                        m_slash = RangedSlashDamage * ZTurretDefense.DamageModifier.Value,
                        m_pierce = RangedPierceDamage * ZTurretDefense.DamageModifier.Value,
                        m_chop = RangedChopDamage * ZTurretDefense.DamageModifier.Value,
                        m_pickaxe = RangedPickaxeDamage * ZTurretDefense.DamageModifier.Value,
                        m_fire = RangedFireDamage * ZTurretDefense.DamageModifier.Value,
                        m_frost = RangedFrostDamage * ZTurretDefense.DamageModifier.Value,
                        m_lightning = RangedLightningDamage * ZTurretDefense.DamageModifier.Value,
                        m_poison = RangedPoisonDamage * ZTurretDefense.DamageModifier.Value,
                        m_spirit = RangedSpiritDamage * ZTurretDefense.DamageModifier.Value,
                    },
                    m_ranged = true,
                    m_staggerMultiplier = 5.0f,
                };

            }

            //HelperLib.PrintAllReflectionOfObject(_rangedHitData.m_damage);

            GetBasicBodyPartsOfTurret();

            GetSpecialBodyPartsOfTurret();

            // try to get barrels, based on node names
            _barrelList = FindBarrels();

            _aimResult = new DegreesSpecifier();
            _aimResultTempCalcHolder = new DegreesSpecifier();

            // set up max-limits of rotations for turrets
            SetUpTurretSpecificData();

            // spread out turret re-target-interval between turrets that load at same time
            _updateTargetTimer = Random.Range(0.0f, _targetUpdateInterval);

            // load gun, if has magazine
            _ammoInGun = AmmoCount;

            // track if this turret was loaded before a heightmap - most likely due to Distant flag being saved to savegame
            var ourHeightmap = Heightmap.FindHeightmap(this.transform.position);

            // only show msg in log if flag for debug is set
            if (ZTurretDefense.ShowHeightMapDebugLogEntries.Value == true)
            {
                var msg =
                    $"{TurretTypeOfThisTurret}.Awake() ({gameObject.name}) -- Heightmap loaded: {(ourHeightmap != null)} ({Heightmap.s_heightmaps.Count} heightmaps loaded), position: {this.transform.position}{(_zNetView.m_distant ? "-- Flagged as distant" : "")}";

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
            _zNetView.Register<int, Vector3, Vector3>("ZTD_FireTurretGun", RPC_FireTurretGun);
        }

        virtual protected void GetBasicBodyPartsOfTurret()
        {
            // AddDebugMsg($"TurretBase.GetBasicBodyPartsOfTurret()");

            // get body-parts of the turret
            // _turretBase = HelperLib.GetChildGameObject(gameObject, "TurretBase");
            _turretTurn = HelperLib.GetChildGameObject(gameObject, "TurretTurn");
            _turretTilt = HelperLib.GetChildGameObject(gameObject, "TurretTilt");

            _turretAimPoint = HelperLib.GetChildGameObject(gameObject, "AimPoint");
        }

        virtual protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"TurretBase.GetSpecialBodyPartsOfTurret()");

            _lightYellow = HelperLib.GetChildGameObject(gameObject, "Light (Yellow)");
            _lightRed = HelperLib.GetChildGameObject(gameObject, "Light (Red)");
            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            if (ZTurretDefense.DisableTurretLight.Value)
            {
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightYellow);
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightRed);
            }

            // get Sfx parts from turret
            _dirtImpactPrefab = HelperLib.GetChildGameObject(gameObject, "DirtImpact");
        }

        virtual protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg($"TurretBase.SetUpTurretLimits()");

            // same as normal Gun, for now
            _rotationDefinitions.MaxRotationHorizontalLeft = 120f;
            _rotationDefinitions.MaxRotationHorizontalRight = 120f;
            _rotationDefinitions.MaxRotationVerticalUp = 65.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 50.0f;
            _rotationDefinitions.RotationSpeed = 130.0f;

            _rotationDefinitions.AllowedAimDeviance = 10.0f;
        }

        protected void AddLogMsg(string message)
        {
            Jotunn.Logger.LogMessage($"{DateTime.Now:o} ### Turret[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddDebugMsg(string message)
        {
            Jotunn.Logger.LogDebug($"{DateTime.Now:o} ### Turret[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddLogInfo(string message)
        {
            Jotunn.Logger.LogInfo($"{DateTime.Now:o} ### Turret[{gameObject.GetInstanceID()}]: {message}");
        }

        protected void AddWarningMsg(string message)
        {
            Jotunn.Logger.LogWarning($"{DateTime.Now:o} ### Turret[{gameObject.GetInstanceID()}]: {message}");
        }

        private List<Barrel> FindBarrels()
        {
            // AddDebugMsg($"TurretBase.FindBarrels()");

            int i = 0; // barrel id

            GameObject latestFoundBarrelGameObject;
            List<Barrel> barrels = new List<Barrel>();

            do
            {
                i++; // start at 1

                latestFoundBarrelGameObject = FindBarrelGameObjectById(i);

                if (latestFoundBarrelGameObject != null)
                {
                    // AddDebugMsg($"Turning BarrelGO {latestFoundBarrelGameObject.name} into Barrel object");

                    // turn GO into barrel and add to list
                    barrels.Add(TurnGameObjectIntoBarrel(i - 1, latestFoundBarrelGameObject));
                }
            } while (latestFoundBarrelGameObject != null);

            // AddDebugMsg($"FindBarrels() -- List holds {barrels.Count} Barrels");

            return barrels;
        }

        private GameObject FindBarrelGameObjectById(int id)
        {
            // AddDebugMsg($"Finding Barrel {id}");

            var barrelGameObject = HelperLib.GetChildGameObject(gameObject, $"LaunchPosition{id}");

            //if (barrelGameObject != null)
            //    AddDebugMsg($"Barrel found: LaunchPosition{id}, {barrelGameObject.name}, {barrelGameObject.tag}");

            return barrelGameObject;
        }

        private Barrel TurnGameObjectIntoBarrel(int id, GameObject gameObject)
        {
            // grab launch effect and launch sound effect
            var launchEffectGameObject = HelperLib.GetChildGameObject(gameObject, "LaunchEffect");
            var launchAudioGameObject = HelperLib.GetChildGameObject(gameObject, "LaunchAudio");
            return new Barrel(id, gameObject, launchEffectGameObject, launchAudioGameObject);
        }

        public void Initialize(TurretConfig turretConfig)
        {
            // AddDebugMsg($"TurretBase.Initialize()");

            TurretTypeOfThisTurret = (TurretType)Enum.Parse(typeof(TurretType), turretConfig.type, true);
            Range = turretConfig.range;
            MinimumRange = turretConfig.minimumRange;

            DroneAttackRange = turretConfig.droneAttackRange;

            FireInterval = turretConfig.fireInterval;
            ReloadTime = turretConfig.reloadTime;
            AmmoCount = turretConfig.ammoCount;

            MaximumNumberOfTrackedTargets = turretConfig.maximumNumberOfTrackedTargets;
            MustHaveLineOfSightToTrack = turretConfig.mustHaveLineOfSightToTrack;
            IgnorePlayerForTargeting = turretConfig.ignorePlayerForTargeting;
            OnlyPlayerForTargeting = turretConfig.onlyPlayerForTargeting;

            IgnoreThatTargetIsBehindCover = turretConfig.ignoreThatTargetIsBehindCover;

            MissileTurnRate = turretConfig.missileTurnRate;
            MissileVelocity = turretConfig.missileVelocity;

            ProjectileVelocity = turretConfig.projectileVelocity;

            Damage = turretConfig.damage;
            BluntDamage = turretConfig.bluntDamage;
            PierceDamage = turretConfig.pierceDamage;
            ChopDamage = turretConfig.chopDamage;
            PickaxeDamage = turretConfig.pickaxeDamage;
            FireDamage = turretConfig.fireDamage;
            FrostDamage = turretConfig.frostDamage;
            LightningDamage = turretConfig.lightningDamage;
            PoisonDamage = turretConfig.poisonDamage;
            SpiritDamage = turretConfig.spiritDamage;

            DamageRadius = turretConfig.damageRadius;

            RangedDamage = turretConfig.rangedDamage;
            RangedBluntDamage = turretConfig.rangedBluntDamage;
            RangedPierceDamage = turretConfig.rangedPierceDamage;
            RangedChopDamage = turretConfig.rangedChopDamage;
            RangedPickaxeDamage = turretConfig.rangedPickaxeDamage;
            RangedFireDamage = turretConfig.rangedFireDamage;
            RangedFrostDamage = turretConfig.rangedFrostDamage;
            RangedLightningDamage = turretConfig.rangedLightningDamage;
            RangedPoisonDamage = turretConfig.rangedPoisonDamage;
            RangedSpiritDamage = turretConfig.rangedSpiritDamage;

            UniformDamageThroughDamageRadius = turretConfig.uniformDamageThroughDamageRadius;

            // special
            HealingTarget = turretConfig.healingTarget;
            HealingAmount = turretConfig.healingAmount;

            RepairTarget = turretConfig.repairTarget;

            PatrolCloseToTurret = turretConfig.patrolCloseToTurret;
        }

        private void SetVolume(object sender, EventArgs e)
        {
            SetVolume();
        }

        private void SetVolume()
        {
            // AddDebugMsg($"Sound Volume: {ZTurretDefense.TurretVolume.Value} to {_audioSources.Length} AudioSources");

            foreach (var audioSource in _audioSources)
            {
                audioSource.volume = ZTurretDefense.TurretVolume.Value / 100.0f;
            }
        }

        private void OnDestroy()
        {
            if (ZTurretDefense.ShowObjectDestroyDebugLogEntries.Value == true)
            {
                AddLogInfo($"OnDestroy({gameObject.name}, {this.transform.position}) ");
            }

            if (_audioSources != null)
            {
                ZTurretDefense.TurretVolume.SettingChanged -= SetVolume;
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
                            $"{TurretTypeOfThisTurret}.Update() -- Finally got it >> Heightmap.FindHeightmap{this.transform.position} return  map {map.GetInstanceID()}, distantLod: {map.IsDistantLod}, bounds: {map.m_bounds}, width: {map.m_width}");
                    }

                    var wearntear = gameObject.GetComponent<WearNTear>();
                    wearntear.Start();

                    _everSeenHeightmap = true;
                }
            }

            // check if OWNER status is changing, as then we MAY need to perform some cleanup
            if (_wasOwnerLastFrame != IsOwner)
            {
                // trigger any cleanup or special handling that is needed due to switch of ownership
                SwitchOwnershipHandling();

                _wasOwnerLastFrame = IsOwner;
            }

            // if this object is NOT owned by THIS player, then grab sync-data and skip doing anything else
            if (IsOwner == false)
            {
                // update is ONLY run on the OWNERS simulation -- so the SYNC READ is run on ALL OTHERS
                SyncTurretNetData();
                _netDataObjectHandler.ReceiveDone();
                return;
            }

            _updateTargetTimer -= Time.deltaTime;

            // if we have magazine, see if we are in reload-step, if so count down reload timer. We can reload with projectiles in flight, for now
            if (AmmoCount > 0 && _reloadGunTimer > 0.0f)
            {
                _reloadGunTimer -= Time.deltaTime;

                if (_reloadGunTimer <= 0.0f)
                {
                    _ammoInGun = AmmoCount;
                }
            }

            // if this is a missile or projectile turret and the missile/projectile is in flight and showing, then fly it
            ControlAndMoveMissilesOrProjectilesOrDrones();

            // always attempt to re-target at interval, pick closest one
            if (_updateTargetTimer < 0)
            {
                FindNewTarget();

                _updateTargetTimer += _targetUpdateInterval;
            }

            CleanTargetListAndUpdateAimInfo();

            // no target, so we reset to center point aim
            if (_turretMode == TurretPatrolType.NoTarget)
            {
                _aimResult.DegreesY = 0.0f;
                _aimResult.DegreesX = 0.0f;
                _aimResult.Distance = 0.0f;
            }

            // we are still good, turn turret to aim at target, fire when we reach interval and are within tolerance

            RotateTurretTowardsTarget();

            UpdateShootingIntervalAndDetermineIfWeShouldShoot();
        }

        virtual protected void SwitchOwnershipHandling()
        {
            // implement in override when needed
            // AddLogInfo($"TurretBase.SwitchOwnershipHandling({IsOwner})");
        }

        virtual protected void SyncTurretNetData()
        {
            if (_netDataObjectHandler.HasDataToRead == false)
                return;

            if (_turretTurn != null)
                _turretTurn.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_turretTurn", Quaternion.identity);
            if (_turretTilt != null) 
                _turretTilt.transform.rotation = _netDataObjectHandler.Data.GetQuaternion("_turretTilt", Quaternion.identity);

            // sub-parts being visible
            if (_lightYellow != null)
                _lightYellow.SetActive(_netDataObjectHandler.Data.GetBool("_lightYellow.SetActive", false));
            if (_lightRed != null)
                _lightRed.SetActive(_netDataObjectHandler.Data.GetBool("_lightRed.SetActive", false));
            if (_lightLaser != null)
                _lightLaser.SetActive(_netDataObjectHandler.Data.GetBool("_lightLaser.SetActive", false));
        }

        virtual protected void CleanTargetListAndUpdateAimInfo()
        {
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

                if (_targetList.Count == 0)
                {
                    SetTargets(null, float.MaxValue);

                    // AddDebugMsg("Update(): All tracked Target(s) dead - deactivating tracking");
                }
                else
                {
                    if (UpdateAimInfoForCurrentTarget() == false)
                    {
                        // fail, something wrong with target
                        SetTargets(null, float.MaxValue);
                    }
                }
            }
        }

        virtual protected bool UpdateAimInfoForCurrentTarget()
        {
            return HelperLib.UpdateAimInfoForCurrentTarget(_targetList[0], _turretAimPoint, _aimResult, _aimResultTempCalcHolder);
        }

        virtual protected void RotateTurretTowardsTarget()
        {
            HelperLib.RotateTurretTowardsTarget(_turretTurn, _turretTilt, _aimResult, _rotationDefinitions, _rotationData);

            // send data over net
            _netDataObjectHandler.Data.Set("_turretTurn", _turretTurn.transform.rotation);
            _netDataObjectHandler.Data.Set("_turretTilt", _turretTilt.transform.rotation);
        }

        virtual protected void UpdateShootingIntervalAndDetermineIfWeShouldShoot()
        {

            if (_nextShootDelayTimer > 0.0f)
                _nextShootDelayTimer -= Time.deltaTime;

            // only if turret has entered AttackTarget Mode, do we shoot.
            if (_turretMode != TurretPatrolType.AttackTarget)
                return;

            // both axis must align -- check so we are within allowed deviance
            // update the distance in each rotation
            _rotationData.DistanceRotY = _rotationData.CurrentRotationY - _aimResult.DegreesY;
            _rotationData.DistanceRotX = _rotationData.CurrentRotationX - _aimResult.DegreesX;

            if (_nextShootDelayTimer > 0.0f)
                return;

            // if either rotation is outside of the allowed deviance, skip shooting for now
            if (Math.Abs(_rotationData.DistanceRotY) > _rotationDefinitions.AllowedAimDeviance || Math.Abs(_rotationData.DistanceRotX) > _rotationDefinitions.AllowedAimDeviance)
                return;

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

        virtual protected void TriggerTurretFiring()
        {
            // AddDebugMsg($"TurretBase.TriggerTurretFiring()");

            // safety - if turret-setup has crapped out
            if (_barrelList == null)
            {
                AddWarningMsg("TurretBase.TriggerTurretFiring() - _barrelList == null");
                return;
            }

            if (_nextBarrelIdToUse > _barrelList.Count)
            {
                AddWarningMsg($"TurretBase.TriggerTurretFiring() - if (_nextBarrelIdToUse > _barrelList.Count) : if ({_nextBarrelIdToUse} > {_barrelList.Count})");
                return;
            }

            _nextShootDelayTimer += FireInterval;

            // safety -- maximum bad value is 'half fire interval away'
            if (_nextShootDelayTimer < (FireInterval / 2.0f))
                _nextShootDelayTimer = (FireInterval / 2.0f);

            // AddDebugMsg($"TurretBase.TriggerTurretFiring() - Fire, Barrel: {_nextBarrelIdToUse}");

            // basic turret always shoots straight as the turret is aimed currently
            LaunchProjectileAndRegisterHitPosition(_turretTilt.transform.rotation, _barrelList[_nextBarrelIdToUse].ThisBarrelGameObject.transform.position);

            // determine barrel we shoot with next time
            _nextBarrelIdToUse++;
            if (_nextBarrelIdToUse >= _barrelList.Count)
                _nextBarrelIdToUse = 0;
        }

        // sets target and update mode based on it
        protected void SetTargets(List<Target> newTargetList, float primaryTargetDistanceValue)
        {
            // AddDebugMsg($"TurretBase.SetTargets({(newTargetList != null ? newTargetList.Count.ToString() : "null")}, {primaryTargetDistanceValue})");

            if (newTargetList == null)
            {
                _targetList.Clear();
            }
            else
            {
                _targetList.Clear();
                _targetList = newTargetList;

                // debug, print list of targets we hold
                //AddDebugMsg($"SetTargets: {_targetList.Count}");
                //foreach (var target in _targetList)
                //{
                //    AddDebugMsg($"SetTargets: target: {target.Character} [{target.Character.GetInstanceID()}], {target.Character.GetHealth()} hp, {target.Character.GetCenterPoint()}");
                //}
            }

            // depending on target, set mode
            if (_targetList.Count == 0)
            {
                 UpdateMode(TurretPatrolType.NoTarget);
                 return;
            }

            // target has a Value within range so it is an target to ATTACK
            if (primaryTargetDistanceValue <= Range)
            {
                UpdateMode(TurretPatrolType.AttackTarget);
            }
            else
            {
                // it is a point value above range, either beyond range or behind wall OR a player - so SCAN
                UpdateMode(TurretPatrolType.ScanTarget);
            }
        }

        virtual protected void UpdateMode(TurretPatrolType newMode)
        {
            // AddDebugMsg($"New Mode: {newMode} ({_turretMode})");

            if (newMode == _turretMode)
                return;

            // a change, so impact turret
            _turretMode = newMode;

            var showScanModeBits = false;
            var showAttackModeBits = false;

            // set flags
            if (_turretMode == TurretPatrolType.ScanTarget)
            {
                showScanModeBits = true;
            }
            else if (_turretMode == TurretPatrolType.AttackTarget)
            {
                showAttackModeBits = true;
            }

            // set Attack bits
            if (showAttackModeBits)
            {
                if (_lightRed != null)
                    _lightRed.SetActive(true);

                if (_lightLaser != null)
                    _lightLaser.SetActive(true);
            }
            else
            {
                // HIDE attack bits
                if (_lightRed != null)
                    _lightRed.SetActive(false);

                if (_lightLaser != null)
                    _lightLaser.SetActive(false);
            }

            // set Scan bits
            if (showScanModeBits)
            {
                if (_lightYellow != null)
                    _lightYellow.SetActive(true);
            }
            else
            {
                // HIDE scan bits
                if (_lightYellow != null)
                    _lightYellow.SetActive(false);
            }

            // set DataObject status
            _netDataObjectHandler.Data.Set("_lightYellow.SetActive", (_lightYellow != null) ? _lightYellow.activeSelf : false);
            _netDataObjectHandler.Data.Set("_lightRed.SetActive", (_lightRed != null) ? _lightRed.activeSelf : false);
            _netDataObjectHandler.Data.Set("_lightLaser.SetActive", (_lightLaser != null) ? _lightLaser.activeSelf : false);
        }

        virtual protected void FindNewTarget()
        {
            // AddDebugMsg("TurretBase.FindNewTarget()");

            float rangeCompareValue = float.MaxValue;

            var newTargetList = new List<Target>();

            List<Character> allCharacters = Character.GetAllCharacters();

            // AddDebugMsg($"Target search: Characters Found: {allCharacters.Count}");

            foreach (Character character in allCharacters)
            {
                // evaluate if this target is ok, else continue to next

                if (character == null)
                    continue;

                // no need to be shooting dead targets
                if (character.GetHealth() <= 0.0f)
                {
                    // AddDebugMsg($"Target search: Character {character.name}, is DEAD -- skipping");
                    continue;
                }

                // AddDebugMsg($"Target search: Validating: Character {character.name}");
                // AddLogInfo($"Target search: Validating: Character {character.name}, faction {character.GetFaction()}");

                // get range to char, as we also base our point-evaluation on it
                var rangeToChar = GetRangeToCharacter(character);

                // if more than *1.5f range, don't care about it at all, skip
                if (rangeToChar > Range * 1.5f)
                    continue;

                if (rangeToChar < MinimumRange)
                {
                    rangeToChar = (Range * 2.0f) + rangeToChar;
                }

                // special, if we ONLY want to target Players and Tamed animals (only used by healing now, but that could change)
                if (OnlyPlayerForTargeting && HealingTarget)
                {
                    if (character.GetFaction() != Character.Faction.Players && character.IsTamed() == false)
                        continue;
                }

                // special, this is a healing attack, so lets verify that the unit is below 100%
                if (HealingTarget)
                {
                    if (character.GetHealthPercentage() >= 1.0f)
                        continue;

                    // AddDebugMsg($"Target search: HealingTarget: {character.name}, {character.GetHealthPercentage()}");
                }

                // SPECIAL CHARACTERS - Dverger faction - we ignore them unless aggroed.

                // broom from witch hut
                if (character.GetFaction() == Character.Faction.Dverger)
                {
                    //AddLogInfo($"Target search: Dverger, {character.name}, is it aggro?");

                    if (character.GetBaseAI() != null
                        && character.GetBaseAI().IsAlerted() == false) // maybe swap this for IsEnemy
                    {
                        //AddLogInfo($"Target search: Dverger and NOT IsAlerted() -- {character.m_name} ({character.GetInstanceID()}) -- skipping");
                        continue;
                    }
                }


                // ANIMALS - VEG

                // vegetarian animals, any animal not counted as forrest monster (rabbits and chickens only?)
                if (character.GetFaction() == Character.Faction.AnimalsVeg)
                {
                    // AddDebugMsg($"Target search: AnimalsVeg found, {character.name}, skipping");
                    continue;
                }

                // ANIMALS - TAMEABLE

                // don't shoot tamed animals or scan
                if (character.IsTamed())
                {
                    // AddDebugMsg($"Target search: tamed found, skipping. ({ (character.m_tameable != null ? character.m_tameable.GetTameness().ToString() : "---")})");

                    // pre-check - we can heal tamed animals, so if that is the case for this weapon, then do NOT skip over this animal
                    if (HealingTarget == false)
                        continue;

                } // any animal that is tameable, but not tame yet, but also not aggro now, ignore. (PLAYERS do not have BaseAI, be careful)
                else if (character.GetBaseAI() != null && character.GetBaseAI().m_tamable != null)
                {
                    // AddDebugMsg($"Target search: GetBaseAI().m_tameable -- progress: { character.GetBaseAI().m_tamable.GetTameness().ToString()}");

                    if (character.GetBaseAI().IsAlerted() == false)
                        continue;

                    // even if alerted, if above 10 in Tame we skip shooting at it, but scan it
                    if (character.GetBaseAI().m_tamable.GetTameness() > 10)
                        rangeToChar = (Range * 5.0f) + rangeToChar;
                }

                // ANIMALS - TAMEABLE END



                // FORESTMONSTER - NOT AGGRAVATED

                 // TODO - DEER should be detected and skipped here, never attack it

                // if general forrest monster, be a bit forgiving
                if (character.GetFaction() == Character.Faction.ForestMonsters)
                {
                    // AddWarningMsg($"Target search: Seeing a ForestMonsters {character.m_name} ({character.GetInstanceID()})");

                    if (character.m_name.Equals("$enemy_deer"))
                    {
                        // we do not target Deer
                        continue;
                    }

                    if (character.GetBaseAI() != null 
                        && character.GetBaseAI().IsAlerted() == false)
                    {
                        // AddDebugMsg($"Target search: ForestMonsters and NOT IsAlerted() -- {character.m_name} ({character.GetInstanceID()})");
                        rangeToChar *= 2.0f; // must be closer to turret, half range, to trigger action
                    }
                }

                // FORESTMONSTER - NOT AGGRAVATED END



                // OTHER MODIFIERS

                // we can target targets we don't see, but prio anyone else in range (since over range, only scan)
                if (IgnoreThatTargetIsBehindCover == false)
                {
                    if (CanSeeCharacter(character, _turretAimPoint) == false)
                    {
                        // maybe add another check for if we can see then, if they are character.IsBoss() and then vs body-center

                        // cannot see their eyes, so reduce prio (scan mode+)
                        rangeToChar = (Range * 3.0f) + rangeToChar;

                        // AddDebugMsg($"Target search: Target not seen: {character.m_name} ({character.GetInstanceID()}), range-points: {rangeToChar}");

                        // if this TURRET has a requirement of LOS, skip target we cannot see
                        if (MustHaveLineOfSightToTrack == true)
                            continue;
                    }
                    else if (character.GetFaction() != Character.Faction.Players && CharacterBehindPlayers(character) == true)
                    {
                        // target is NOT a player, AND it is behind a player, so increase range-point to hold off on shooting for now
                        rangeToChar = (Range * 2.0f) + rangeToChar;
                    }
                }

                // for now, target friendly also, but super-prio anyone else (and never attack) -- also limit it to half range of turrets full guard range, it is only curious when players are close
                if (character.GetFaction() == Character.Faction.Players)
                {
                    // this turret should ignore Players OR we have a global modifier to ALWAYS ignore players -- skip
                    if (IgnorePlayerForTargeting || (HealingTarget == false && ZTurretDefense.TurretsShouldFullyIgnorePlayers.Value))
                    {
                        // AddDebugMsg($"Target search: Target is Player (IgnorePlayerForTargeting: {IgnorePlayerForTargeting},  ZTurretDefense.TurretsShouldFullyIgnorePlayers {ZTurretDefense.TurretsShouldFullyIgnorePlayers.Value}) -- skipping");
                        continue;
                    }

                    // this target is a player, only bother to scan if within half turret distance (skip doing this if OnlyPlayerForTargeting is true, as it is most likely a heal gun)
                    if (OnlyPlayerForTargeting == false && rangeToChar >= Range * 0.5f)
                    {
                        // behind cover and/or beyond half turret Range, so we don't even target them
                        continue;
                    }

                    // set very low prio (high range-compare value)
                    if (OnlyPlayerForTargeting == false)
                    {
                        rangeToChar = (Range * 10.0f) + rangeToChar;
                    }
                }

                // EVALUATE if we can even turn to see target if we turn and are limited by rotation arc boundaries, else we skip caring about it.

                var validTargetInArc = HelperLib.ValidateTargetAgainstRotateRestrictions(_turretAimPoint, character.m_currentVel, character, _aimResultTempCalcHolder, _rotationDefinitions);
                if (validTargetInArc == false)
                {
                    // AddDebugMsg($"Target search: Target is out of possible Targeting-FOV, so skip");
                    continue;
                }

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToChar < rangeCompareValue)
                {
                    rangeCompareValue = AddCharacterToTargetList(newTargetList, character, rangeToChar, MaximumNumberOfTrackedTargets);
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
        }

        private float AddCharacterToTargetList(List<Target> newTargetList, Character character, float distanceRating, int maxTargets, bool targetGround = false)
        {
            // AddDebugMsg($"AddCharacterToTargetList({newTargetList.Count}, {character}, {distanceRating}, {maxTargets}, {targetGround})");
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
                    newTargetList.Add(CreateTarget(character, distanceRating, targetGround));

                    lowestDistance = distanceRating;
                }

                // AddDebugMsg($"return {lowestDistance}");
                return lowestDistance;
            }

            // AddDebugMsg($"We should insert the target at set pos, remove any target that goes beyond max, ({indexToInsertTargetAt}, {character}, {distanceRating})");

            // we should insert the target at set pos, remove any target that goes beyond max
            newTargetList.Insert(indexToInsertTargetAt, CreateTarget(character, distanceRating, targetGround));

            // AddDebugMsg($"if (newTargetList.Count > maxTargets) : if ({newTargetList.Count} > {maxTargets})");
            if (newTargetList.Count > maxTargets)
            {
                newTargetList.RemoveAt(maxTargets);

                lowestDistance = newTargetList[newTargetList.Count - 1].DistanceRating;
            }

            // AddDebugMsg($"return {lowestDistance}");
            return lowestDistance;
        }

        protected int GetTargetListIndexBasedOnDistance(List<Target> newTargetList, float distance)
        {
            // AddDebugMsg($"GetTargetListIndexBasedOnDistance({newTargetList.Count}, {distance})");

            int index = 0;
            foreach (var target in newTargetList)
            {
                if (distance < target.DistanceRating)
                {
                    return index;
                }

                index++;
            }

            return Int32.MaxValue;
        }

        private Target CreateTarget(Character character, float distanceRating, bool targetGround)
        {
            return new Target()
            {
                Character = character,
                Location = character.GetCenterPoint(),
                DistanceRating = distanceRating,
                IsMoveOrder = targetGround,
            };
        }

        private float GetRangeToCharacter(Character character)
        {
            return Vector3.Distance(character.GetCenterPoint(), _turretAimPoint.transform.position); // character.transform.position / m_eye.position
        }

        protected bool CanSeeCharacter(Character character, GameObject aimPoint)
        {
            var vector = character.m_eye.position - aimPoint.transform.position; // GetCenterPoint() - aim towards eyes and if we can see eyes, we can see it
            return !Physics.Raycast(aimPoint.transform.position, vector.normalized, vector.magnitude, _viewBlockMask);
        }

        private bool CharacterBehindPlayers(Character character)
        {
            var vector = character.GetCenterPoint() - _turretAimPoint.transform.position; // m_bounds.center;

            var ray = new Ray(_turretAimPoint.transform.position, vector.normalized);
            if (Physics.Raycast(ray, out var hitInfo, vector.magnitude, _viewBlockMaskPlayersCharactersOnly) == false)
                return false;

            // check if this is a character and if it is a player
            var charHit = hitInfo.collider.transform.root.gameObject.GetComponent<Character>();

            // odd case, we hit something but it isn't counted as a character...check and update mask
            if (charHit == null)
            {
                AddWarningMsg($"Target search: Unexpected scanline hit, NOT A CHARACTER: {hitInfo.collider.transform.root.gameObject.name}");
                HelperLib.PrintOutGameObjectInfo(hitInfo.collider.transform.root.gameObject);
                return false;
            }

            // only hit the sought after target, so nothing in the way here
            if (charHit == character)
                return false;

            // char may be behind other monster char, and that is fine
            if (charHit.GetFaction() != Character.Faction.Players)
                return false;

            // is behind a Player char, with-hold fire for now (can be extended for tame animals too in the future)
            return true;
        }

        // Launch projectile here (and apply damage on hit) - announce launch and impact effect for network
        virtual protected void LaunchProjectileAndRegisterHitPosition(Quaternion directionQuat, Vector3 barrelEdgePosition)
        {
            // AddDebugMsg("LaunchProjectileAndShowHitPosition()");

            var ray = new Ray(barrelEdgePosition, directionQuat * Vector3.forward);
            if (Physics.Raycast(ray, out var impactInfo, Range * 2.0f, _rayMaskSolids) == false)
                return;

            // safety, shouldn't happen
            if (_hitData == null)
            {
                AddWarningMsg($"LaunchProjectileAndShowHitPosition() - _hitData is null");
                return;
            }

            // check what we hit -- if it is a character, push damage to it (except Players)
            var charHit = impactInfo.collider.transform.root.gameObject.GetComponent<Character>();

            if (charHit != null)
            {
                var hitData = _hitData.Clone();
                hitData.m_hitCollider = impactInfo.collider;
                hitData.m_point = impactInfo.point;
                hitData.m_dir = directionQuat.eulerAngles;

                ApplyDamageToCharacterHit(charHit, hitData);
            }

            // if projectile has a range set, check impacts and allocate ranged damage
            // Base-projectiles do NOT have this now, added to Heavy Projectiles (and Missiles, in separate approach)

            // tell other players the launch occur, what barrel to use and impact info, to draw effects...
            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_FireTurretGun", _nextBarrelIdToUse, impactInfo.point, impactInfo.normal);
        }

        protected void ApplyDamageToCharacterHit(Character characterHit, HitData hitData)
        {
            // safety
            if (characterHit == null)
            {
                return;
            }

            // HelperLib.PrintOutObjectHierarchy(characterHit.gameObject);

            // safety
            if (characterHit.GetHealth() <= 0.0f)
                    return;

            // Players are receiving less damage
            if (characterHit.GetFaction() == Character.Faction.Players)
            {
                var damageRatio = 0.25f;

                // adjust damage if against Player
                hitData.m_damage.m_blunt *= damageRatio;
                hitData.m_damage.m_slash *= damageRatio;
                hitData.m_damage.m_pierce *= damageRatio;
                hitData.m_damage.m_chop *= damageRatio;
                hitData.m_damage.m_pickaxe *= damageRatio;
                hitData.m_damage.m_fire *= damageRatio;
                hitData.m_damage.m_frost *= damageRatio;
                hitData.m_damage.m_lightning *= damageRatio;
                hitData.m_damage.m_poison *= damageRatio;
                hitData.m_damage.m_spirit *= damageRatio;

                hitData.m_staggerMultiplier *= 4.0f; // as if full damage
            }

            characterHit.Damage(hitData);
        }

        // explosive impact - allocate damage to characters within range
        protected void DamageAreaTargets(Vector3 hitPosition, HitData hitData, bool onlyDamageCharacters)
        {
            // AddDebugMsg($"DamageAreaTargets: {hitPosition}");

            if (DamageRadius <= 0.0f)
                return;

            if (hitData == null)
                return;

            var hits = Physics.OverlapSphere(hitPosition, DamageRadius, _rayMaskExplosionArea); // _rayMaskCharacterDamageArea

            // AddDebugMsg($"DamageAreaTargets: impacts: {hits.Length} ----------------");

            // for each collider hit - can mean MULTIPLE hits on LARGE monsters
            // POSSIBLE TODO: Sort hits by distance, list Characters hit, if dupe, reduce damage to 25% for all hits after first?

            foreach (var hit in hits)
            {
                var charHit = hit.transform.root.gameObject.GetComponent<Character>();

                // early exit if it was not a char, and we only care about chars
                if (onlyDamageCharacters && charHit == null)
                {
                    // AddDebugMsg("DamageAreaTargets: hit is not a char, skipping");
                    continue;
                }

                // AddDebugMsg($"DamageAreaTargets: >> {hit.transform.root.gameObject.name}");

                // calc damage falloff based on damage - re-used value for all types below
                var hitClosestPosition = (hit.providesContacts) ?  hit.ClosestPoint(hitPosition) : hit.ClosestPointOnBounds(hitPosition);
                var distanceToHitPosition = Vector3.Distance(hitPosition, hitClosestPosition);

                var distanceToHitPositionCalc =
                    distanceToHitPosition -
                    1.0f; // bias towards 1m closer to center, also reduce micro-float at end of range
                if (distanceToHitPositionCalc < 0.0)
                    distanceToHitPositionCalc = 0.0f;
                var damageRatio = (DamageRadius - distanceToHitPositionCalc) / DamageRadius;

                // simple version of aggressive damage falloff, more than linear, 0.5 is 0.25, 0.9 os 0.81, 0.1 is 0.01 etc
                damageRatio *= damageRatio;

                // so low ratio that there is no damage to apply...theoretical skip out, sen minimum level we care about
                if (damageRatio <= 0.05f)
                {
                    // AddDebugMsg($"DamageAreaTargets: damageRatio too low at {damageRatio} (dist: {distanceToHitPosition}) -- skipping");
                    continue;
                }

                if (damageRatio > 1.0f)
                {
                    AddDebugMsg($"DamageAreaTargets: >> damageRatio above 100%, set 10% of damage (most likely non-convex object)");
                    damageRatio = 0.1f;
                }

                // if it was a char that was hit
                if (charHit != null)
                {
                    if (charHit.GetHealth() <= 0.0f)
                    {
                        // AddDebugMsg($"DamageAreaTargets: char {charHit.m_name} is dead, skipping");
                        continue;
                    }

                    // AddDebugMsg($"DamageAreaTargets: char '{charHit.m_name}', extended debug info:");
                    // HelperLib.PrintAllReflectionOfObject(hit);

                    var stunMult = 1.0f;

                    // override any damage multiple, forced to 1.0f
                    if (UniformDamageThroughDamageRadius)
                    {
                        damageRatio = 1.0f;
                    }

                    // if the char hit is a player, set damageMulti to 0.1f but stun to 10x (normal stun, reduced damage)
                    if (charHit.GetFaction() == Character.Faction.Players)
                    {
                        damageRatio = 0.25f;
                        stunMult = 4.0f;
                        // AddDebugMsg($"DamageAreaTargets: char '{charHit.m_name}' hit is a player, set damageMult to {damageRatio}");
                    }

                    var customHitDataForChar = hitData.Clone();
                    customHitDataForChar.m_hitCollider = hit;
                    customHitDataForChar.m_point = hitClosestPosition; // charHit.GetCenterPoint();
                    customHitDataForChar.m_damage.m_blunt *= damageRatio;
                    customHitDataForChar.m_damage.m_slash *= damageRatio;
                    customHitDataForChar.m_damage.m_pierce *= damageRatio;
                    customHitDataForChar.m_damage.m_chop *= damageRatio;
                    customHitDataForChar.m_damage.m_pickaxe *= damageRatio;
                    customHitDataForChar.m_damage.m_fire *= damageRatio;
                    customHitDataForChar.m_damage.m_frost *= damageRatio;
                    customHitDataForChar.m_damage.m_lightning *= damageRatio;
                    customHitDataForChar.m_damage.m_poison *= damageRatio;
                    customHitDataForChar.m_damage.m_spirit *= damageRatio;

                    customHitDataForChar.m_staggerMultiplier *= stunMult;
                    // add more here if we have missiles that use it, later

                    charHit.Damage(customHitDataForChar);

                    // AddDebugMsg($"DamageAreaTargets: damage char {charHit.m_name} ({charHit.GetInstanceID()}), distance: {distanceToHitPosition}, distance (calc): {distanceToHitPositionCalc}, damage-ratio {damageRatio}, bluntDamage: {customHitDataForChar.m_damage.m_blunt}, slashDamage: {customHitDataForChar.m_damage.m_slash}, pieceDamage: {customHitDataForChar.m_damage.m_pierce}, chopDamage: {customHitDataForChar.m_damage.m_chop},  pickaxeDamage: {customHitDataForChar.m_damage.m_pickaxe}, fireDamage: {customHitDataForChar.m_damage.m_fire}, frostDamage: {customHitDataForChar.m_damage.m_frost}, lightningDamage: {customHitDataForChar.m_damage.m_lightning}, poisonDamage: {customHitDataForChar.m_damage.m_poison}, spiritDamage: {customHitDataForChar.m_damage.m_spirit}");

                    continue;
                } // char hit

                var damage = (hitData.m_damage.m_pierce + hitData.m_damage.m_fire) * 1.0f;
                var customHitData = hitData.Clone();
                customHitData.m_hitCollider = hit;
                customHitData.m_point = hitClosestPosition;
                customHitData.m_damage.m_blunt = 0;
                customHitData.m_damage.m_slash = 0;
                customHitData.m_damage.m_pierce = 0;
                customHitData.m_damage.m_chop = (damage * damageRatio);
                customHitData.m_damage.m_pickaxe = (damage * damageRatio) * 0.5f;
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

                    // AddWarningMsg($"DamageAreaTargets: [Tree] {treeHit.name}, maxHealth: {treeHit.m_health}, distance: {distanceToHitPosition}, distance (calc): {distanceToHitPositionCalc}, damage-ratio {damageRatio}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    continue;
                }

                var treeLogHit = hit.transform.root.gameObject.GetComponent<TreeLog>();
                if (treeLogHit != null)
                {
                    treeLogHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [TreeLog] {treeLogHit.name}, maxHealth: {treeLogHit.m_health}, distance: {distanceToHitPosition}, distance (calc): {distanceToHitPositionCalc}, damage-ratio {damageRatio}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    continue;
                }

                var destructibleHit = hit.transform.root.gameObject.GetComponent<Destructible>();
                if (destructibleHit != null)
                {
                    destructibleHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [Destructible] {destructibleHit.name}, maxHealth: {destructibleHit.m_health}, distance: {distanceToHitPosition}, distance (calc): {distanceToHitPositionCalc}, damage-ratio {damageRatio}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    continue;
                }

                var largeRockHit = hit.transform.root.gameObject.GetComponent<MineRock5>();
                if (largeRockHit != null)
                {
                    largeRockHit.Damage(customHitData);

                    // AddWarningMsg($"DamageAreaTargets: [MineRock5] {largeRockHit.name}, maxHealth: {largeRockHit.m_health}, chopDamage: {customHitData.m_damage.m_chop},  pickaxeDamage: {customHitData.m_damage.m_pickaxe}");

                    continue;
                }


                // AddWarningMsg($"DamageAreaTargets: No matching type -- {hit.transform.root.gameObject.name}");
            }
        }

        // if using lasGun?
        private void DamageFlamethrowerTargets(Vector3 hitPosition, HitData hitData)
        {
            var hits = Physics.OverlapCapsule(transform.position, transform.position + transform.forward * Range, 1, _rayMaskSolids);

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out Character character) && character.GetFaction() != Character.Faction.Players && !character.IsTamed() && character.GetHealth() > 0.0f)
                {
                    character.Damage(hitData);
                }
            }
        }

        virtual protected void ControlAndMoveMissilesOrProjectilesOrDrones()
        {
            // implement in relevant child class
        }

        // RPC ----------------

        // We are being told over RPC that a turret fired, so play effects such as launch and impact sfx
        protected void RPC_FireTurretGun(long sender, int barrelId, Vector3 impactLocation, Vector3 impactNormal)
        {
            // AddDebugMsg($"RPC_Fire({barrelId}, {impactPosition.ToString()}) from {sender}");

            // safety, if we have not set up barrels, we quit out
            if (_barrelList == null)
                return;

            // if the barrel id is higher than what we have, something is wrong - skip
            if ((barrelId + 1) > _barrelList.Count)
                return;

            var barrelObject = _barrelList[barrelId];

            // AddDebugMsg($"RPC_Fire() -- Play effect from barrel {barrelId}, {barrelObject.LaunchBarrelGameObject.name}, {barrelObject.LaunchBarrelGameObject.GetInstanceID()}");

            // LAUNCH FIRE EFFECT

            // clone effect, move out into the world, easier to retain due to oversight in API for transforms
            var newEffect = Instantiate(barrelObject.LaunchEffectGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newEffect.transform.SetParent(null);
            Destroy(newEffect, 5.0f); // destroy to force cleanup and remove odd loop/replay issue
            newEffect.SetActive(true);

            // TRIGGER AUDIO from barrel

            var newSfxEffect = Instantiate(barrelObject.LaunchAudioGameObject, barrelObject.ThisBarrelGameObject.transform, false);
            newSfxEffect.transform.SetParent(null);
            Destroy(newSfxEffect, 1.0f); // destroy to force cleanup and remove odd loop/replay issue
            newSfxEffect.SetActive(true);

            // IMPACT EFFECT

            // hit something solid - pop hit-marker at the hit position
            var impactNormalQuat = new Quaternion();
            impactNormalQuat.SetLookRotation(impactNormal);

            var impactSfx = Instantiate(_dirtImpactPrefab, null);
            impactSfx.transform.position = impactLocation;
            impactSfx.transform.rotation = impactNormalQuat;

            impactSfx.SetActive(false);
            impactSfx.SetActive(true);

            // HelperLib.PrintOutGameObjectInfo(impactSfx);

            Destroy(impactSfx, 10.0f);

            // End Impact SFX
        }
        
        // RPC END

    }
}