using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Movement controller for a doom-like humanoid character
    /// </summary>
    public abstract class MovementControllerBase : MonoBehaviour {
        /// <summary>
        /// True if input has running mode selected
        /// </summary>
        public abstract bool IsRunning { get; set; }

        /// <summary>
        /// True if input requests crouching
        /// </summary>
        public abstract bool WantsToCrouch { get; set; }

        /// <summary>
        /// Move the character forward/backward/left/right
        /// </summary>
        /// <param name="motion">The motion to apply, y ignored, magnitude of 1 is max speed</param>
        public abstract void Move(Vector3 motion);

        /// <summary>
        /// Rotate the character horizontally (yaw)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public abstract void Turn(float amount);

        /// <summary>
        /// Aim the character's head and gun vertically (pitch)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public abstract void Aim(float amount);

        /// <summary>
        /// If character is grounded on walkable ground, unground and add vertical motion
        /// </summary>
        public abstract void Jump();
    }
}