namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    ///     A steering behavior object
    /// </summary>
    public interface IMovement {
        /// <summary>
        ///     The actor doing the steering
        /// </summary>
        Kinematic Self { get; set; }

        /// <summary>
        ///     Overrides default target set in Self
        /// </summary>
        IKinematic OverrideTarget { get; set; }

        /// <summary>
        ///     Return the acceleration and angular acceleration for the desired steering
        /// </summary>
        /// <returns>A SteeringOutput object</returns>
        SteeringOutput GetSteering();
    }
}