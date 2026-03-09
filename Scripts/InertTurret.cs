using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class InertTurret : TurretBase
    {
        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"InertTurret.GetSpecialBodyPartsOfTurret()");

            _lightYellow = HelperLib.GetChildGameObject(gameObject, "Light (Yellow)");
            _lightRed = HelperLib.GetChildGameObject(gameObject, "Light (Red)");
            _lightLaser = HelperLib.GetChildGameObject(gameObject, "Light (Laser)");

            if (ZTurretDefense.DisableTurretLight.Value)
            {
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightYellow);
                HelperLib.DisableLightsOnGameObjectAndChildren(_lightRed);
            }
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("InertTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 120f;
            _rotationDefinitions.MaxRotationHorizontalRight = 120f;
            _rotationDefinitions.MaxRotationVerticalUp = 65.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 35.0f;
            _rotationDefinitions.RotationSpeed = 120.0f;
            _rotationDefinitions.AllowedAimDeviance = 45.0f;
        }

        override protected void TriggerTurretFiring()
        {
            // not really firing, just ticking the code
            _nextShootDelayTimer += FireInterval;

            // safety -- maximum bad value is 'half fire interval away'
            if (_nextShootDelayTimer < (FireInterval / 2.0f))
                _nextShootDelayTimer = (FireInterval / 2.0f);
        }
    }
}
