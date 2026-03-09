using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    public struct RotationDefinitions
    {
        public float MaxRotationHorizontalLeft;
        public float MaxRotationHorizontalRight;
        public float MaxRotationVerticalUp;
        public float MaxRotationVerticalDown;
        public float RotationSpeed;
        public float AllowedAimDeviance;
    }

    public static class MathHelper
    {
        public static int MultiplyWithFloatWithFloor(int inAmount, float multiple, int floor)
        {
            var newAmountFloat = inAmount * multiple;

            var newAmountInt = (int)(Math.Round(newAmountFloat, MidpointRounding.AwayFromZero));

            if (newAmountInt < floor)
                newAmountInt = floor;

            return newAmountInt;
        }

        public static double NthRoot(double A, int N)
        {
            return Math.Pow(A, 1.0 / N);
        }

        public static void GetDegreesBetweenAimAndTarget(DegreesSpecifier aimResult, Transform aimTransform, Vector3 targetLocation, Transform debug)
        {
            // backup old
            var oldAimrotation = aimTransform.rotation;

            aimTransform.LookAt(targetLocation, Vector3.up);

            var rotY = aimTransform.localRotation.eulerAngles.y;

            if (rotY > 180.0f)
                rotY -= 360.0f;
            if (rotY < -180.0f)
                rotY += 360.0f;

            aimResult.DegreesY = rotY;

            var rotX = aimTransform.localRotation.eulerAngles.x;

            if (rotX > 180.0f)
                rotX -= 360.0f;
            if (rotX < -180.0f)
                rotX += 360.0f;

            aimResult.DegreesX = rotX;

            // reset aim transform
            if (debug != null)
                debug.rotation = aimTransform.rotation;

            aimTransform.rotation = oldAimrotation;
        }

        public static Vector3 FirstOrderIntercept(Vector3 shooterPosition, Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, Vector3 targetVelocity)
        {
            Vector3 targetRelativePosition = targetPosition - shooterPosition;
            Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;

            // calc time to intercept
            float t = FirstOrderInterceptTime
            (
                shotSpeed,
                targetRelativePosition,
                targetRelativeVelocity
            );

            // calc position we therefor need to aim for
            return targetPosition + t * (targetRelativeVelocity);
        }

        //first-order intercept using relative target position
        public static float FirstOrderInterceptTime(float shotSpeed, Vector3 targetRelativePosition, Vector3 targetRelativeVelocity)
        {
            float velocitySquared = targetRelativeVelocity.sqrMagnitude;
            if (velocitySquared < 0.001f)
                return 0f;

            float a = velocitySquared - shotSpeed * shotSpeed;

            //handle similar velocities
            if (Mathf.Abs(a) < 0.001f)
            {
                float t = -targetRelativePosition.sqrMagnitude /
                (
                    2f * Vector3.Dot
                    (
                        targetRelativeVelocity,
                        targetRelativePosition
                    )
                );
                return Mathf.Max(t, 0f); //don't shoot back in time
            }

            float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
            float c = targetRelativePosition.sqrMagnitude;
            float determinant = b * b - 4f * a * c;

            if (determinant > 0f)
            { //determinant > 0; two intercept paths (most common)
                float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a),
                        t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
                if (t1 > 0f)
                {
                    if (t2 > 0f)
                        return Mathf.Min(t1, t2); //both are positive
                    else
                        return t1; //only t1 is positive
                }
                else
                    return Mathf.Max(t2, 0f); //don't shoot back in time
            }
            else if (determinant < 0f) //determinant < 0; no intercept path
                return 0f;
            else //determinant = 0; one intercept path, pretty much never happens
                return Mathf.Max(-b / (2f * a), 0f); //don't shoot back in time
        }

        public static Rect GetBoundingBoxOnScreen(Bounds bounds, Camera camera)
        {
            // get the 8 vertices of the bounding box
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            Vector3[] vertices = new Vector3[] {
                center + Vector3.right * size.x / 2f + Vector3.up * size.y / 2f + Vector3.forward * size.z / 2f,
                center + Vector3.right * size.x / 2f + Vector3.up * size.y / 2f - Vector3.forward * size.z / 2f,
                center + Vector3.right * size.x / 2f - Vector3.up * size.y / 2f + Vector3.forward * size.z / 2f,
                center + Vector3.right * size.x / 2f - Vector3.up * size.y / 2f - Vector3.forward * size.z / 2f,
                center - Vector3.right * size.x / 2f + Vector3.up * size.y / 2f + Vector3.forward * size.z / 2f,
                center - Vector3.right * size.x / 2f + Vector3.up * size.y / 2f - Vector3.forward * size.z / 2f,
                center - Vector3.right * size.x / 2f - Vector3.up * size.y / 2f + Vector3.forward * size.z / 2f,
                center - Vector3.right * size.x / 2f - Vector3.up * size.y / 2f - Vector3.forward * size.z / 2f,
            };
            Rect retVal = Rect.MinMaxRect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

            // iterate through the vertices to get the equivalent screen projection
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = camera.WorldToScreenPoint(vertices[i]);
                if (v.x < retVal.xMin)
                    retVal.xMin = v.x;
                if (v.y < retVal.yMin)
                    retVal.yMin = v.y;
                if (v.x > retVal.xMax)
                    retVal.xMax = v.x;
                if (v.y > retVal.yMax)
                    retVal.yMax = v.y;
            }

            return retVal;
        }
    }
}
