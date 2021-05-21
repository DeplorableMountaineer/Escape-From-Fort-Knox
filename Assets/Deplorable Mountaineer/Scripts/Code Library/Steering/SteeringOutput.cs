#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    ///     Object encapsulating acceleration and rotational acceleration from steering behaviors
    /// </summary>
    public struct SteeringOutput {
        /// <summary>
        ///     Acceleration
        /// </summary>
        public Vector3? Linear;

        /// <summary>
        ///     Rotational acceleration for pitch, yaw, and roll, respectively,
        ///     in degrees per second squared.
        /// </summary>
        public Vector3? Eulers;

        /// <summary>
        ///     Rotational acceleration as a scaled vector whose direction is the axis
        ///     (right-hand rule), and whose magnitude is radians per second squared.
        /// </summary>
        public Vector3? Angular;

        /// <summary>
        ///     Linear combination of two steering outputs
        /// </summary>
        /// <param name="a">First steering</param>
        /// <param name="b">Second steering</param>
        /// <returns>Their sum</returns>
        public static SteeringOutput operator +(SteeringOutput a, SteeringOutput b){
            SteeringOutput result = new SteeringOutput();
            result.Linear = a.Linear + b.Linear;
            if(!a.Linear.HasValue) result.Linear = b.Linear;
            else if(!b.Linear.HasValue) result.Linear = a.Linear;
            else result.Linear = a.Linear + b.Linear;

            if(!a.Eulers.HasValue) result.Eulers = b.Eulers;
            else if(!b.Eulers.HasValue) result.Eulers = a.Eulers;
            else result.Eulers = a.Eulers + b.Eulers;

            if(!a.Angular.HasValue) result.Angular = b.Angular;
            else if(!b.Angular.HasValue) result.Angular = a.Angular;
            else result.Angular = a.Angular + b.Angular;

            return result;
        }

        /// <summary>
        ///     Weighting of steering outputs;  will eventually be capped by max acceleration
        ///     parameters of the kinematic being steered.
        /// </summary>
        /// <param name="a">Steering</param>
        /// <param name="b">weight, should be positive</param>
        /// <returns></returns>
        public static SteeringOutput operator *(SteeringOutput a, float b){
            return new SteeringOutput {
                Linear = b*a.Linear, Eulers = b*a.Eulers, Angular = b*a.Angular
            };
        }
    }
}