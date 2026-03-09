using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using Jotunn.Managers;
    using UnityEngine;

    public class FishingDroneTurret : GatherDroneTurret
    {
        override protected float SetDroneMovementHeight(float heightIn)
        {
            return (_droneTarget.OnGatheritemDropReturnTrip == false) ? 30.5f : (heightIn + 3.0f);
        }
        
        override protected void FindNewTarget()
        {
            // AddDebugMsg("FishingDroneTurret.FindNewTarget()");

            float rangeCompareValue = float.MaxValue;

            var newTargetList = new List<Target>();

            List<ItemDrop> droppedItemList = ItemDrop.s_instances;

            // AddLogInfo($"Target search: Items Found: {droppedItemList.Count}");

            foreach (ItemDrop fishItem in droppedItemList)
            {
                // evaluate if this target is ok, else continue to next

                if (fishItem == null)
                    continue;

                // not enabled, so not really there in the world
                if (fishItem.enabled == false)
                {
                    AddDebugMsg($"itemDrop, Target search: Item {fishItem.name} is not enabled -- skipping");
                    continue;
                }

                // AddDebugMsg($"itemDrop, Target search: Item {item.name} is in layer {item.gameObject.layer}");

                if (fishItem.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Fish)
                {
                    // AddDebugMsg($"itemDrop, Target search: Item {item.name} is detected as NOT a fish (character) -- skipping");
                    continue;
                }

                // get range to char, as we also base our point-evaluation on it
                var rangeToPiece = GetRangeToItemDrop(fishItem);

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
                    if (_droneTarget.GatherItemDrop == fishItem)
                        continue;
                }

                // make sure this fisk isn't already caught and stranded, aka above water surface
                if (fishItem.transform.position.y >= 30.0f)
                {
                    // AddDebugMsg($"Skipping to grab: {item.name}, range: {rangeToPiece} as it is not in the water, at the height of {item.transform.position.y}");
                    continue;
                }

                // evaluate best choice, add if needed, return rangeCompare of lowest value for easy compare
                if (rangeToPiece < rangeCompareValue)
                {
                    rangeCompareValue = AddItemToTargetList(newTargetList, fishItem, rangeToPiece, MaximumNumberOfTrackedTargets);
                }
            }

            // adjust for target-mode set back to BEST target
            if (newTargetList.Count > 0)
            {
                rangeCompareValue = newTargetList[0].DistanceRating;
            }

            // set new list of targets and new (BEST) range compare, to set MODE
            SetTargets(newTargetList, rangeCompareValue);

            // AddDebugMsg($"FishingDroneTurret.SetTargets({newTargetList.Count.ToString()}, {rangeCompareValue})");

            // inject higher time-spread of finding new items
            _updateTargetTimer += 5.0f;
        }

    }
}
