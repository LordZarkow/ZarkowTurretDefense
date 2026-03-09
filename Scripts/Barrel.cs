using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    /// <summary>
    /// Describes a barrel of the gun, holding a sfx for fire-effect and relative position from base (or aim?)
    /// </summary>
    public class Barrel
    {
        public int Id;
        public GameObject ThisBarrelGameObject;
        public GameObject LaunchEffectGameObject;
        public GameObject LaunchAudioGameObject;

        public Barrel(int id, GameObject thisGameObject, GameObject launchEffectGameObject, GameObject launchAudioGameObject)
        {
            Id = id;
            ThisBarrelGameObject = thisGameObject;
            LaunchEffectGameObject = launchEffectGameObject;
            LaunchAudioGameObject = launchAudioGameObject;
        }
    }
}
