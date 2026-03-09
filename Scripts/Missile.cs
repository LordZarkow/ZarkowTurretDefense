using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class Missile
    {
        public int Id;
        public GameObject MissileGameObject;
        public Target Target;
        public float TimeLivedLeft; // let targeting wire burn out

        public bool ActiveInGame; // If the missile is active via RPC, i.e. SHOWING
        public bool ActiveInFlight; // if this missile has been triggered to be launched by controlling OWNER

        public bool TargetingActive => (TimeLivedLeft > 0.0f);

        public Trail Trail;

        public void RegisterMissileAndAddTarget(Target target)
        {
            if (target.IsMoveOrder == false)
            {
                target.MissilesSharingThisTarget.Add(this);
            }

            Target = target;
        }

        public void DeRegisterMissileAndRemoveTarget()
        {
            if (Target == null)
            {
                Jotunn.Logger.LogDebug($"### Missile -- Target is NULL - most likely killed already (Id: {this.Id})");
                return;
            }

            if (Target.IsMoveOrder == false)
            {
                Target.MissilesSharingThisTarget.Remove(this);
            }

            Target = null;
        }
    }
}
