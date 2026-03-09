using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class Projectile
    {
        public int Id;
        public GameObject ProjectileGameObject;

        public bool AnnouncedAndVisible; // If the projectile is active via RPC, i.e. SHOWING for Owner AND Clients (only way for non-Owners to know it needs to pull info)
        public bool OwnerHasActiveControl; // if this projectile has been triggered to be launched by controlling OWNER

        public Trail Trail;
    }
}