using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class MineTurret : TurretBase
    {
        private GameObject _mineLaunchEffect;
        private GameObject _mineTriggerEffect;

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"MineTurret.GetSpecialBodyPartsOfTurret()");

            // signal turret specific
            _mineLaunchEffect = HelperLib.GetChildGameObject(gameObject, "MineLaunchEffect");
            _mineTriggerEffect = HelperLib.GetChildGameObject(gameObject, "MineTriggerEffect");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("MineTurret.SetUpTurretLimits()");

            _rotationDefinitions.MaxRotationHorizontalLeft = 180f;
            _rotationDefinitions.MaxRotationHorizontalRight = 180f;
            _rotationDefinitions.MaxRotationVerticalUp = 90.0f;
            _rotationDefinitions.MaxRotationVerticalDown = 85.0f;
            _rotationDefinitions.RotationSpeed = 360.0f;
            _rotationDefinitions.AllowedAimDeviance = 45.0f;
        }

        override protected void TriggerTurretFiring()
        {
            _nextShootDelayTimer += FireInterval;

            // safety -- maximum bad value is 'half fire interval away'
            if (_nextShootDelayTimer < (FireInterval / 2.0f))
                _nextShootDelayTimer = (FireInterval / 2.0f);

            Vector3 signalFlareSpawnLocation = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

            // AddDebugMsg($"Trigger Mine: {signalFlareSpawnLocation.ToString()}");

            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_FireMine", signalFlareSpawnLocation);

            DamageAreaTargets(signalFlareSpawnLocation, _rangedHitData, true);
        }
        
        override protected void RegisterRemoteProcedureCalls()
        {
            _zNetView.Register<Vector3>("ZTD_FireMine", RPC_FireMine);
        }

        private void RPC_FireMine(long sender, Vector3 startLocation)
        {
            // AddDebugMsg($"RPC_FireMine({startLocation.ToString()}) from {sender}");

            // LAUNCH EFFECT
            var launchFlareSfx = Instantiate(_mineLaunchEffect, null);
            launchFlareSfx.transform.position = _turretTilt.transform.position;
            launchFlareSfx.transform.rotation = _turretTilt.transform.rotation;

            launchFlareSfx.SetActive(false);
            launchFlareSfx.SetActive(true);

            Destroy(launchFlareSfx, 3.0f);
            // end launch SFX

            // TRIGGERED EFFECT
            var mineTriggeredSfx = Instantiate(_mineTriggerEffect, null);
            mineTriggeredSfx.transform.position = startLocation;

            mineTriggeredSfx.SetActive(false);
            mineTriggeredSfx.SetActive(true);

            Destroy(mineTriggeredSfx, 10.0f);
            // End signal SFX
        }
    }
}
