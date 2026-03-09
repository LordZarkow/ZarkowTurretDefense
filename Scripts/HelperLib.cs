using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;
    using Console = System.Console;

    public enum TurretType
    {
        Gun,
        HeavyGun,
        SignalTurret,
        LightGun,
        MissileGun,
        MineTurret,
        Drone,
        RepairDrone,
        GatherDrone,
        FishingDrone,
        LoggerDrone,

        InertTurret,
    }

    public enum BuildingpartType
    {
        Static,
    }

    public static class HelperLib
    {
        static public GameObject GetChildGameObject(GameObject fromGameObject, string withName)
        {
            Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in ts)
            {
                // Jotunn.Logger.LogDebug($"### Turret: >> {t.name}");

                if (t.gameObject.name == withName) 
                    return t.gameObject;
            }

            return null;
        }

        static public void DisableLightsOnGameObjectAndChildren(GameObject gameObjectRoot)
        {
            var lights = gameObjectRoot.GetComponentsInChildren<Light>(true);
            foreach (var light in lights)
            {
                light.enabled = false;
            }
        }

        static public bool UpdateAimInfoForCurrentTarget(Target primaryTarget, GameObject turretAimPoint, DegreesSpecifier aimResult, DegreesSpecifier aimResultTempCalcHolder)
        {
            // if for some reason this target lacks a character, quit out
            if (primaryTarget.Character == null)
            {
                return false;
            }

            var targetPosition = primaryTarget.IsMoveOrder ? primaryTarget.Location : primaryTarget.Character.GetCenterPoint(); // _target.m_eye.position
            Vector3 aimPoint = MathHelper.FirstOrderIntercept(turretAimPoint.transform.position, Vector3.zero, 1000f, targetPosition, primaryTarget.IsMoveOrder ? Vector3.zero : primaryTarget.Character.m_currentVel);

            aimResultTempCalcHolder.Distance = Vector3.Distance(turretAimPoint.transform.position, targetPosition);

            // update deg data object to be used below
            MathHelper.GetDegreesBetweenAimAndTarget(aimResultTempCalcHolder, turretAimPoint.transform, aimPoint, null);

            // move info to aim
            aimResult.DegreesX = aimResultTempCalcHolder.DegreesX;
            aimResult.DegreesY = aimResultTempCalcHolder.DegreesY;

            aimResult.Distance = aimResultTempCalcHolder.Distance;

            // if we are targeting char, we need to update location too
            if (primaryTarget.IsMoveOrder == false)
            {
                primaryTarget.Location = aimPoint;
            }

            // HelperLib.PrintOutAimResult(_aimResult);
            return true;
        }

        static public bool UpdateAimInfoForCurrentPieceTarget(Target primaryTarget, GameObject droneAimPoint, DegreesSpecifier aimResult, DegreesSpecifier aimResultTempCalcHolder)
        {
            // if for some reason this target lacks a character, quit out
            if (primaryTarget.WearAndTearPiece == null)
            {
                return false;
            }

            var targetPosition = primaryTarget.WearAndTearPiece.gameObject.transform.position;
            Vector3 aimPoint = MathHelper.FirstOrderIntercept(droneAimPoint.transform.position, Vector3.zero, 1000f, targetPosition, Vector3.zero);

            aimResultTempCalcHolder.Distance = Vector3.Distance(droneAimPoint.transform.position, targetPosition);

            // update deg data object to be used below
            MathHelper.GetDegreesBetweenAimAndTarget(aimResultTempCalcHolder, droneAimPoint.transform, aimPoint, null);

            // move info to aim
            aimResult.DegreesX = aimResultTempCalcHolder.DegreesX;
            aimResult.DegreesY = aimResultTempCalcHolder.DegreesY;

            aimResult.Distance = aimResultTempCalcHolder.Distance;

            // if we are targeting char, we need to update location too
            if (primaryTarget.IsMoveOrder == false)
            {
                primaryTarget.Location = aimPoint;
            }

            // HelperLib.PrintOutAimResult(aimResult);
            return true;
        }

        static public bool UpdateAimInfoForCurrentItemDropTarget(Target primaryTarget, GameObject droneAimPoint, DegreesSpecifier aimResult, DegreesSpecifier aimResultTempCalcHolder)
        {
            // if for some reason this target lacks a character, quit out
            if (primaryTarget.GatherItemDrop == null)
            {
                return false;
            }

            var targetPosition = primaryTarget.GatherItemDrop.gameObject.transform.position;
            Vector3 aimPoint = MathHelper.FirstOrderIntercept(droneAimPoint.transform.position, Vector3.zero, 1000f, targetPosition, Vector3.zero);

            aimResultTempCalcHolder.Distance = Vector3.Distance(droneAimPoint.transform.position, targetPosition);

            // update deg data object to be used below
            MathHelper.GetDegreesBetweenAimAndTarget(aimResultTempCalcHolder, droneAimPoint.transform, aimPoint, null);

            // move info to aim
            aimResult.DegreesX = aimResultTempCalcHolder.DegreesX;
            aimResult.DegreesY = aimResultTempCalcHolder.DegreesY;

            aimResult.Distance = aimResultTempCalcHolder.Distance;

            // if we are targeting char, we need to update location too
            if (primaryTarget.IsMoveOrder == false)
            {
                primaryTarget.Location = aimPoint;
            }

            // HelperLib.PrintOutAimResult(aimResult);
            return true;
        }

        static public bool UpdateAimInfoForCurrentTreeTarget(Target primaryTarget, GameObject droneAimPoint, DegreesSpecifier aimResult, DegreesSpecifier aimResultTempCalcHolder)
        {
            var targetPosition = (primaryTarget.LoggerTreeBaseTarget != null) 
                ? primaryTarget.LoggerTreeBaseTarget.gameObject.transform.position 
                : primaryTarget.LoggerTreeLogTarget != null 
                    ? primaryTarget.LoggerTreeLogTarget.gameObject.transform.position 
                    : primaryTarget.LoggerDestructibleTarget.gameObject.transform.position;

            // add 0.5f in height to any tree to increase chance of not hit dirt etc
            targetPosition.y += (primaryTarget.LoggerTreeBaseTarget != null
                ? 1.0f
                : primaryTarget.LoggerTreeLogTarget != null
                    ? 0.2f
                    : 0.15f);

            Vector3 aimPoint = MathHelper.FirstOrderIntercept(droneAimPoint.transform.position, Vector3.zero, 1000f, targetPosition, Vector3.zero);

            aimResultTempCalcHolder.Distance = Vector3.Distance(droneAimPoint.transform.position, targetPosition);

            // update deg data object to be used below
            MathHelper.GetDegreesBetweenAimAndTarget(aimResultTempCalcHolder, droneAimPoint.transform, aimPoint, null);

            // move info to aim
            aimResult.DegreesX = aimResultTempCalcHolder.DegreesX;
            aimResult.DegreesY = aimResultTempCalcHolder.DegreesY;

            aimResult.Distance = aimResultTempCalcHolder.Distance;

            // if we are targeting char, we need to update location too
            if (primaryTarget.IsMoveOrder == false)
            {
                primaryTarget.Location = aimPoint;
            }

            // HelperLib.PrintOutAimResult(aimResult);
            return true;
        }

        static public bool ValidateTargetAgainstRotateRestrictions(GameObject turretAimPoint, Vector3 targetVelocity, Character characterEvaluated, DegreesSpecifier aimResult, RotationDefinitions rotationDefinitions)
        {
            var aimPoint = characterEvaluated.transform.position;

            // update aimpoint based on predict from projectile speed and target speed + direction
            aimPoint = MathHelper.FirstOrderIntercept(turretAimPoint.transform.position, Vector3.zero, 1000.0f, aimPoint, targetVelocity);

            // the predict based on speed on target and projectiles make more sense if we actually run projectiles, but for now, it is all hitscan

            // update deg data object to be used below
            MathHelper.GetDegreesBetweenAimAndTarget(aimResult, turretAimPoint.transform, aimPoint, null);

            // now always allow some minor outside-step, we have a 10 deg fire-allowance anyway
            var addedAimRotationAmount = rotationDefinitions.AllowedAimDeviance;

            // verify limits
            if (rotationDefinitions.MaxRotationHorizontalLeft > 0.0f && (-rotationDefinitions.MaxRotationHorizontalLeft + -addedAimRotationAmount) > aimResult.DegreesY) // left is negative
                return false;

            if (rotationDefinitions.MaxRotationHorizontalRight > 0.0f && (rotationDefinitions.MaxRotationHorizontalRight + addedAimRotationAmount) < aimResult.DegreesY)
                return false;

            if (rotationDefinitions.MaxRotationVerticalUp > 0.0f && (rotationDefinitions.MaxRotationVerticalUp + addedAimRotationAmount) < -aimResult.DegreesX) // since X is negative on up
                return false;

            if (rotationDefinitions.MaxRotationVerticalDown > 0.0f && (rotationDefinitions.MaxRotationVerticalDown + addedAimRotationAmount) < aimResult.DegreesX)
                return false;

            return true;
        }

        static public void RotateTurretTowardsTarget(GameObject turretTurn, GameObject turretTilt, DegreesSpecifier aimResult, RotationDefinitions rotationDefinitions, RotationData rotationData)
        {
            // ROTATE TURRET

            // -- ROTATE Y --

            // distance in rotation for Y
            rotationData.DistanceRotY = rotationData.CurrentRotationY - aimResult.DegreesY;

            if (Math.Abs(rotationData.DistanceRotY) > 0.00001f)
            {
                // are we less than one move away, if so, only move what is needed
                var rotationSpeedY = rotationDefinitions.RotationSpeed * Time.deltaTime;
                if (rotationSpeedY > Math.Abs(rotationData.DistanceRotY))
                    rotationSpeedY = Math.Abs(rotationData.DistanceRotY);

                // AddDebugMsg($"Update({Time.deltaTime}), rotationSpeedY: {rotationSpeedY}");

                // do we need to turn left?
                if (rotationData.CurrentRotationY > aimResult.DegreesY)
                {
                    rotationData.CurrentRotationY -= rotationSpeedY;

                    // limit
                    if (rotationDefinitions.MaxRotationHorizontalLeft > 0 && rotationData.CurrentRotationY < -rotationDefinitions.MaxRotationHorizontalLeft)
                        rotationData.CurrentRotationY = -rotationDefinitions.MaxRotationHorizontalLeft;

                    // normalize
                    if (rotationData.CurrentRotationY < -180.0f)
                        rotationData.CurrentRotationY += 360.0f;
                }
                else // we need to step right
                {
                    rotationData.CurrentRotationY += rotationSpeedY;

                    // limit
                    if (rotationDefinitions.MaxRotationHorizontalRight > 0 && rotationData.CurrentRotationY > rotationDefinitions.MaxRotationHorizontalRight)
                        rotationData.CurrentRotationY = rotationDefinitions.MaxRotationHorizontalRight;

                    // normalize
                    if (rotationData.CurrentRotationY > 180.0f)
                        rotationData.CurrentRotationY -= 360.0f;
                }

                // turn turret
                turretTurn.transform.localEulerAngles = new Vector3(0.0f, rotationData.CurrentRotationY, 0.0f);
            }

            // -- ROTATE X --

            // distance in rotation for Y
            rotationData.DistanceRotX = rotationData.CurrentRotationX - aimResult.DegreesX;

            if (Math.Abs(rotationData.DistanceRotX) > 0.001f)
            {
                // TODO:
                // var adjustedRotationSpeed = _rotationSpeed;
                //
                // are we less than one move away, if so, only move what is needed
                var rotationSpeedX = rotationDefinitions.RotationSpeed * Time.deltaTime;
                if (rotationSpeedX > Math.Abs(rotationData.DistanceRotX))
                    rotationSpeedX = Math.Abs(rotationData.DistanceRotX);

                // do we need to turn up?
                if (rotationData.CurrentRotationX > aimResult.DegreesX)
                {
                    rotationData.CurrentRotationX -= rotationSpeedX;

                    // limit
                    if (rotationDefinitions.MaxRotationVerticalUp > 0 && rotationData.CurrentRotationX < -rotationDefinitions.MaxRotationVerticalUp)
                        rotationData.CurrentRotationX = -rotationDefinitions.MaxRotationVerticalUp;

                    // normalize (shouldn't be needed for X, but meh)
                    if (rotationData.CurrentRotationX < -180.0f)
                        rotationData.CurrentRotationX += 360.0f;
                }
                else // we need to turn down
                {
                    rotationData.CurrentRotationX += rotationSpeedX;

                    // limit
                    if (rotationDefinitions.MaxRotationVerticalDown > 0 && rotationData.CurrentRotationX > rotationDefinitions.MaxRotationVerticalDown)
                        rotationData.CurrentRotationX = rotationDefinitions.MaxRotationVerticalDown;

                    // normalize (shouldn't be needed for X, but meh)
                    if (rotationData.CurrentRotationX > 180.0f)
                        rotationData.CurrentRotationX -= 360.0f;
                }

                // tilt turret
                turretTilt.transform.localEulerAngles = new Vector3(rotationData.CurrentRotationX, 0.0f, 0.0f);
            }
        }

        static public void PrintOutObjectHierarchy(GameObject fromGameObject)
        {
            Jotunn.Logger.LogDebug($"### Turret: --------------------------------------------------------------------------");
            Jotunn.Logger.LogDebug($"### Turret: >> Print hierarchy info for: {fromGameObject.name}, {fromGameObject.GetInstanceID()}");

            Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in ts)
            {
                Jotunn.Logger.LogDebug($"### Turret: >> {t.name} - Active: {t.gameObject.activeInHierarchy}");
            }

            Jotunn.Logger.LogDebug($"### Turret: >> Sum parts: {ts.Length} --------------------------------------------------");
        }

        static public void PrintOutGameObjectInfo(GameObject fromGameObject)
        {
            Jotunn.Logger.LogDebug($"### Turret: >> Print info for: {fromGameObject.name}, {fromGameObject.GetInstanceID()}");

            Jotunn.Logger.LogDebug($"### Turret: >> position: {fromGameObject.transform.position}");
            Jotunn.Logger.LogDebug($"### Turret: >> localPosition: {fromGameObject.transform.localPosition}");
            Jotunn.Logger.LogDebug($"### Turret: >> rotation: {fromGameObject.transform.eulerAngles}");
            Jotunn.Logger.LogDebug($"### Turret: >> localRotation: {fromGameObject.transform.localEulerAngles}");
            Jotunn.Logger.LogDebug($"### Turret: >> lossyScale: {fromGameObject.transform.lossyScale}");
            Jotunn.Logger.LogDebug($"### Turret: >> localScale: {fromGameObject.transform.localScale}");

            Jotunn.Logger.LogDebug($"### Turret: >> ------------------------------------------------------------------------");
        }

        static public void PrintOutAimResult(DegreesSpecifier aimResult)
        {
            Jotunn.Logger.LogDebug($"### Turret: >> Print info for aimResult: {aimResult.GetHashCode()}");
            Jotunn.Logger.LogDebug($"### Turret: >> DegreesX: {aimResult.DegreesX}");
            Jotunn.Logger.LogDebug($"### Turret: >> DegreesY: {aimResult.DegreesY}");
            Jotunn.Logger.LogDebug($"### Turret: >> Distance: {aimResult.Distance}");
        }

        static public void PrintAllReflectionOfCharacter(Character character)
        {
            Jotunn.Logger.LogDebug($"### Turret: >> Print info for Character: {character.m_name}, {character.GetInstanceID()}");

            var props = character.GetType().GetProperties();
            Jotunn.Logger.LogDebug($"### Turret: >> Properties: {props.Length}");

            foreach (var prop in props)
            {
                var value = prop.GetValue(character, null);
                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {prop.Name}, value: {value?.ToString()}");
            }

            var fields = character.GetType().GetFields();
            Jotunn.Logger.LogDebug($"### Turret: >> Fields: {fields.Length}");

            foreach (var field in fields)
            {
                var value = field.GetValue(character);
                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {field.Name}, value: {value?.ToString()}");
            }

            var members = character.GetType().GetMembers();
            Jotunn.Logger.LogDebug($"### Turret: >> Members: {members.Length}");

            foreach (var member in members)
            {
                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {member.Name}, value: {member}");
            }

            Jotunn.Logger.LogDebug($"### Turret: >> ------------------------------------------------------------------------");
        }


        static public void PrintAllReflectionOfObject(object yourObject)
        {
            Jotunn.Logger.LogDebug($"### Turret: >> Print info for Object: {yourObject}, {yourObject.GetHashCode()}");

            var props = yourObject.GetType().GetProperties();
            Jotunn.Logger.LogDebug($"### Turret: >> Properties: {props.Length}");

            foreach (var prop in props)
            {
                object value;

                try
                {
                    value = prop.GetValue(yourObject, null);
                }
                catch (Exception)
                {
                    value = "[NULL-EXCEPTION]";
                }

                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {prop.Name}, value: {value?.ToString()}");
            }

            var fields = yourObject.GetType().GetFields();
            Jotunn.Logger.LogDebug($"### Turret: >> Fields: {fields.Length}");

            foreach (var field in fields)
            {
                var value = field.GetValue(yourObject);
                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {field.Name}, value: {value?.ToString()}");
            }

            var members = yourObject.GetType().GetMembers();
            Jotunn.Logger.LogDebug($"### Turret: >> Members: {members.Length}");

            foreach (var member in members)
            {
                Jotunn.Logger.LogDebug($"### Turret:  --- >> : {member.Name}, value: {member}");
            }

            Jotunn.Logger.LogDebug($"### Turret: >> ------------------------------------------------------------------------");
        }

    }
}
