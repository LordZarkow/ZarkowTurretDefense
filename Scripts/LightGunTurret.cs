using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    public class LightGunTurret : TurretBase
    {

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("LightGunTurret.SetUpTurretLimits()");

            // much slower than normal turrets, can rotate less up and down
            _rotationDefinitions.MaxRotationHorizontalLeft = 120f;
            _rotationDefinitions.MaxRotationHorizontalRight = 120f;
            _rotationDefinitions.MaxRotationVerticalUp = 45.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 25.0f;
            _rotationDefinitions.RotationSpeed = 50.0f;
            _rotationDefinitions.AllowedAimDeviance = 15.0f;
        }
    }
}
