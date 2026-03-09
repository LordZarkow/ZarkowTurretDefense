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
    public class BuildingpartConfigRequirement
    {
        public string item;
        public int amount;

        public static RequirementConfig Convert(BuildingpartConfigRequirement buildingpartConfigRequirement)
        {
            return new RequirementConfig()
            {
                Amount = MathHelper.MultiplyWithFloatWithFloor(buildingpartConfigRequirement.amount, ZTurretDefense.CostModifier.Value, 1),
                Item = buildingpartConfigRequirement.item,
                Recover = true,
            };
        }
    }

    [Serializable]
    public class BuildingpartConfig
    {
        public string name;
        public string bundleName;
        public string prefabPath;
        public string description;
        public string pieceTable;
        public string craftingStation;
        public bool enabled;
        public string type;

        public List<BuildingpartConfigRequirement> resources;

        public static CustomPiece Convert(GameObject prefab, BuildingpartConfig buildingpartConfig)
        {
            return new CustomPiece(prefab,
                true,
                new PieceConfig
                {
                    Name = buildingpartConfig.name,
                    Description = buildingpartConfig.description,
                    Enabled = buildingpartConfig.enabled,
                    PieceTable = buildingpartConfig.pieceTable,
                    CraftingStation = buildingpartConfig.craftingStation, // "Workbench" or something else - or blank
                    Category = "BuildingParts",// "Misc",
                    Requirements = buildingpartConfig.resources.Select(BuildingpartConfigRequirement.Convert).ToArray(),
                    AllowedInDungeons = true,
                }
            );
        }
    }
}
