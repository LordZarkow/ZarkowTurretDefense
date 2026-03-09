using System;
using System.Collections.Generic;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class Target
    {
        public Character Character;
        public Vector3 Location;
        public float DistanceRating; // not distance in meter, but used for rating targets
        
        public bool IsMoveOrder; // is a MOVE order

        public bool IsTargetWearAndTearOrder; // is a WearAndTear (Repair) Order
        public WearNTear WearAndTearPiece; // as a Repair order we need the actual item to repair

        public bool IsGatherItemOrder; // is a gather order
        public ItemDrop GatherItemDrop; // item to pick up
        public bool OnGatheritemDropReturnTrip; // we have picked up item, is now on way back to drop off (target location is now turret center)

        public bool IsTargetLoggerOrder; // is a TreeBase OR TreeLog Logging Order
        public TreeBase LoggerTreeBaseTarget;
        public TreeLog LoggerTreeLogTarget;
        public Destructible LoggerDestructibleTarget;

        public float TimeToLive;

        // special, for guided only
        public List<Missile> MissilesSharingThisTarget = new List<Missile>();
    }
}
