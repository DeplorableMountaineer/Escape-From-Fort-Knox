#region

using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    [PublicAPI]
    public static class Prediction {
        public static readonly Vector3 StandardGravityAcceleration = new Vector3(0, -9.81f, 0);

        public static Vector3 PositionAtTime(float time, Vector3 muzzleLocation,
            Vector3 muzzleDirection, float muzzleVelocity, Vector3 constantAcceleration){
            return muzzleLocation + muzzleDirection.normalized*muzzleVelocity*time +
                   constantAcceleration*time*time*.5f;
        }

        public static Vector3 PositionAtTime(float time, Vector3 muzzleLocation,
            Vector3 muzzleDirection, float muzzleVelocity, float gravity){
            return PositionAtTime(time, muzzleLocation, muzzleDirection, muzzleVelocity,
                Mathf.Abs(gravity)*Vector3.down);
        }

        public static Vector3 PositionAtTime(float time, Vector3 muzzleLocation,
            Vector3 muzzleDirection, float muzzleVelocity){
            return PositionAtTime(time, muzzleLocation, muzzleDirection, muzzleVelocity,
                StandardGravityAcceleration);
        }

        /// <summary>
        ///     Compute impact time of a ballistic object (i.e. affected only by gravity)
        ///     without taking air resistance into consideration.
        /// </summary>
        /// <param name="floorY">Y value of the floor where impact is expected</param>
        /// <param name="currentY">Y value of the ballistic object at time 0</param>
        /// <param name="currentYVel">Y value of the velocity at time 0</param>
        /// <param name="gravityY">
        ///     (optional) Y value of gravity acceleration;
        ///     most likely negative;  Default value is standard Earth gravity -9.81
        /// </param>
        /// <returns>
        ///     Impact time, seconds from now, or infinity if
        ///     y value can never reach floorY
        /// </returns>
        public static float ImpactTime(float floorY, float currentY, float currentYVel,
            float gravityY = -9.81f){
            float disc = currentYVel*currentYVel - 2*gravityY*(currentY - floorY);
            if(disc < 0) return Mathf.Infinity;
            return (-currentYVel + Mathf.Sqrt(disc))/gravityY;
        }

        public static Vector3? FiringSolution(Vector3 start, Vector3 end, float muzzleVelocity,
            Vector3 gravity, bool useHighSolution = false){
            Vector3 delta = end - start;
            float a = gravity.sqrMagnitude;
            float b = -4*(Vector3.Dot(gravity, delta) + muzzleVelocity*muzzleVelocity);
            float c = 4*delta.sqrMagnitude;
            float disc = b*b - 4*a*c;
            if(disc < 0) return null;
            float t0 = Mathf.Sqrt((-b + Mathf.Sqrt(disc))/(2*a));
            float t1 = Mathf.Sqrt((-b - Mathf.Sqrt(disc))/(2*a));
            float ttt;
            if(t0 < 0)
                if(t1 < 0)
                    return null;
                else ttt = t1;
            else if(t1 < 0) ttt = t0;
            else
                ttt = useHighSolution ? Mathf.Max(t1, t0) : Mathf.Min(t1, t0);
            return (delta*2 - gravity*ttt*ttt)/(2*muzzleVelocity*ttt);
        }

        public static Vector3? FiringSolution(Vector3 start, Vector3 end,
            float muzzleVelocity, bool useHighSolution = false){
            return FiringSolution(start, end, muzzleVelocity, StandardGravityAcceleration,
                useHighSolution);
        }

        public static Vector3? FiringSolution(Vector3 start, Vector3 end, float muzzleVelocity,
            float gravity, bool useHighSolution = false){
            return FiringSolution(start, end, muzzleVelocity, Mathf.Abs(gravity)*Vector3.down,
                useHighSolution);
        }
    }
}