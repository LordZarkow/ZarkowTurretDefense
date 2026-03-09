using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ZarkowTurretDefense.Models
{
    using Scripts;

    [Serializable]
    public class TurretConfigRequirement
    {
        public string item;
        public int amount;

        public static RequirementConfig Convert(TurretConfigRequirement turretConfigRequirement)
        {
            return new RequirementConfig()
            {
                Amount = MathHelper.MultiplyWithFloatWithFloor(turretConfigRequirement.amount, ZTurretDefense.CostModifier.Value, 1),
                Item = turretConfigRequirement.item,
                Recover = true,
            };
        }
    }

    [Serializable]
    public class TurretConfig
    {
        public string name;
        public string bundleName;
        public string prefabPath;
        public string description;
        public string pieceTable;
        public string craftingStation;
        public bool enabled;
        public string type;
        public float fireInterval;

        public float reloadTime;
        public int ammoCount = 0;
        public int maximumNumberOfTrackedTargets = 1;
        public bool mustHaveLineOfSightToTrack;
        public bool ignorePlayerForTargeting;
        public bool onlyPlayerForTargeting;

        public bool ignoreThatTargetIsBehindCover; // allow target without prio-penalty for behind behind cover for the turret (drone has separate final targeting)

        public float missileTurnRate;
        public float missileVelocity;

        public float projectileVelocity;

        // damage info, for direct hit
        public float damage;
        public float bluntDamage;
        public float pierceDamage;
        public float chopDamage;
        public float pickaxeDamage;
        public float fireDamage;
        public float frostDamage;
        public float lightningDamage;
        public float poisonDamage;
        public float spiritDamage;

        public float range;
        public float minimumRange;

        public float droneAttackRange;

        // ranged related damage
        public float damageRadius;

        public float rangedDamage;
        public float rangedBluntDamage;
        public float rangedPierceDamage;
        public float rangedChopDamage;
        public float rangedPickaxeDamage;
        public float rangedFireDamage;
        public float rangedFrostDamage;
        public float rangedLightningDamage;
        public float rangedPoisonDamage;
        public float rangedSpiritDamage;

        public bool uniformDamageThroughDamageRadius;

        // special flag
        public bool healingTarget;
        public float healingAmount;

        public bool repairTarget;

        public bool patrolCloseToTurret;

        public List<TurretConfigRequirement> resources;

        public static CustomPiece Convert(GameObject prefab, TurretConfig turretConfig)
        {
            return new CustomPiece(prefab,
                true,
                new PieceConfig
                {
                    Name = turretConfig.name,
                    Description = turretConfig.description,
                    Enabled = turretConfig.enabled,
                    PieceTable = turretConfig.pieceTable,
                    CraftingStation = turretConfig.craftingStation, // "Workbench" or something else - or blank
                    Category = "Turrets",// "Misc",
                    Requirements = turretConfig.resources.Select(TurretConfigRequirement.Convert).ToArray(),
                    AllowedInDungeons = true,
                }
            );
        }
    }
}
