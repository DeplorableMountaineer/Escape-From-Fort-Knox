#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    ///     A moving actor or steering target
    /// </summary>
    public interface IKinematic {
        /// <summary>
        ///     World position of the actor
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        ///     World rotation of the actor
        /// </summary>
        Vector3 EulerAngles { get; set; }

        /// <summary>
        ///     Velocity of the actor
        /// </summary>
        Vector3 Velocity { get; set; }

        /// <summary>
        ///     Rotational velocity of the actor, in radians per second
        /// </summary>
        Vector3 AngularVelocity { get; set; }

        /// <summary>
        ///     Radius of a sphere modelling the collision of the actor
        /// </summary>
        float Radius { get; }

        /// <summary>
        ///     Name of the actor
        /// </summary>
        string Name { get; }
    }
}