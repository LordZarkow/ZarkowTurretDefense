using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public class SignalTurret : TurretBase
    {
        protected GameObject _signalFlare;
        protected GameObject _signalLaunchEffect;

        override protected void GetSpecialBodyPartsOfTurret()
        {
            // AddDebugMsg($"SignalTurret.GetSpecialBodyPartsOfTurret()");

            // signal turret specific
            _signalFlare = HelperLib.GetChildGameObject(gameObject, "LightFlareEffect");
            _signalLaunchEffect = HelperLib.GetChildGameObject(gameObject, "SignalTurretLaunchEffect");
        }

        override protected void SetUpTurretSpecificData()
        {
            // AddDebugMsg("SignalTurret.SetUpTurretLimits()");

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

            // Todo: grab direction towards enemy, in y, normalize, try calculating a position towards target, but 10m up
            Vector3 signalFlareSpawnLocation = new Vector3(transform.position.x, transform.position.y + 15.0f, transform.position.z);

            // AddDebugMsg($"Trigger Signal: {signalFlareSpawnLocation.ToString()}");

            _zNetView.InvokeRPC(ZNetView.Everybody, "ZTD_FireSignalFlare", signalFlareSpawnLocation);
        }


        override protected void RegisterRemoteProcedureCalls()
        {
            _zNetView.Register<Vector3>("ZTD_FireSignalFlare", RPC_FireSignalFlare);
        }
        
        private void RPC_FireSignalFlare(long sender, Vector3 startLocation)
        {
            // AddDebugMsg($"RPC_FireSignalFlare({startLocation.ToString()}) from {sender}");

            // LAUNCH EFFECT
            var launchFlareSfx = Instantiate(_signalLaunchEffect, null);
            launchFlareSfx.transform.position = _turretTilt.transform.position;
            launchFlareSfx.transform.rotation = _turretTilt.transform.rotation;

            launchFlareSfx.SetActive(false);
            launchFlareSfx.SetActive(true);

            Destroy(launchFlareSfx, 3.0f);
            // end launch SFX

            // SIGNAL EFFECT
            var signalFlareSfx = Instantiate(_signalFlare, null);
            signalFlareSfx.transform.position = startLocation;
            // signalFlareSfx.transform.rotation = Vector3.Zero;

            signalFlareSfx.SetActive(false);
            signalFlareSfx.SetActive(true);

            signalFlareSfx.GetComponent<Rigidbody>().AddForce(Vector3.down * 25.0f);

            Destroy(signalFlareSfx, 19.0f);
            // End signal SFX
        }
    }
}
