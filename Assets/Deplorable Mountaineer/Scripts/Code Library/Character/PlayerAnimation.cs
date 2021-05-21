using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Sync's third-person animation with character movement
    /// </summary>
    public class PlayerAnimation : MonoBehaviour {
        [Tooltip("The animator controller for the third-person character mesh")]
        [SerializeField]
        private Animator animator;

        private State _state = State.None;

        private static readonly int AnimatorIsCrouched =
            Animator.StringToHash("IsCrouched");

        private static readonly int AnimatorSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimatorIdle = Animator.StringToHash("Idle");
        private static readonly int AnimatorForward = Animator.StringToHash("Forward");
        private static readonly int AnimatorBackward = Animator.StringToHash("Backward");
        private static readonly int AnimatorStrafeLeft = Animator.StringToHash("Strafe Left");

        private static readonly int
            AnimatorStrafeRight = Animator.StringToHash("Strafe Right");

        /// <summary>
        /// Set move animation
        /// </summary>
        /// <param name="motion">Speed and relative direction of motion</param>
        /// <param name="isCrouched">If true, should be crouched while moving</param>
        public void Move(Vector3 motion, bool isCrouched = false){
            if(Mathf.Abs(motion.z) > Mathf.Abs(motion.x)){
                if(motion.z > 0) Forward(motion.magnitude, isCrouched);
                else Backward(-motion.magnitude, isCrouched);
                return;
            }

            float s = 1;
            if(motion.z < 0) s = -1;

            if(motion.x > 0) StrafeRight(motion.magnitude*s, isCrouched);
            else StrafeLeft(motion.magnitude*s, isCrouched);
        }

        /// <summary>
        /// Idle, possibly while crouched
        /// </summary>
        /// <param name="isCrouched">If true, should be crouched</param>
        public void Idle(bool isCrouched = false){
            animator.SetBool(AnimatorIsCrouched, isCrouched);
            if(_state == State.Idle) return;
            animator.SetTrigger(AnimatorIdle);
            _state = State.Idle;
        }

        /// <summary>
        /// Set move forward animation
        /// </summary>
        /// <param name="speed">speed</param>
        /// <param name="isCrouched">If true, should be crouched while moving</param>
        public void Forward(float speed, bool isCrouched = false){
            animator.SetFloat(AnimatorSpeed, speed);
            animator.SetBool(AnimatorIsCrouched, isCrouched);
            if(_state == State.Forward) return;
            animator.SetTrigger(AnimatorForward);
            _state = State.Forward;
        }

        /// <summary>
        /// Set move backward animation
        /// </summary>
        /// <param name="speed">speed</param>
        /// <param name="isCrouched">If true, should be crouched while moving</param>
        public void Backward(float speed, bool isCrouched = false){
            animator.SetFloat(AnimatorSpeed, speed);
            animator.SetBool(AnimatorIsCrouched, isCrouched);
            if(_state == State.Backward) return;
            animator.SetTrigger(AnimatorBackward);
            _state = State.Backward;
        }

        /// <summary>
        /// Set strafe left animation
        /// </summary>
        /// <param name="speed">speed</param>
        /// <param name="isCrouched">If true, should be crouched while moving</param>
        public void StrafeLeft(float speed, bool isCrouched = false){
            animator.SetFloat(AnimatorSpeed, speed);
            animator.SetBool(AnimatorIsCrouched, isCrouched);
            if(_state == State.StrafeLeft) return;
            animator.SetTrigger(AnimatorStrafeLeft);
            _state = State.StrafeLeft;
        }

        /// <summary>
        /// Set strafe right animation
        /// </summary>
        /// <param name="speed">speed</param>
        /// <param name="isCrouched">If true, should be crouched while moving</param>
        public void StrafeRight(float speed, bool isCrouched = false){
            animator.SetFloat(AnimatorSpeed, speed);
            animator.SetBool(AnimatorIsCrouched, isCrouched);
            if(_state == State.StrafeRight) return;
            animator.SetTrigger(AnimatorStrafeRight);
            _state = State.StrafeRight;
        }

        private enum State {
            /// <summary>
            /// No animation
            /// </summary>
            None,

            /// <summary>
            /// Idle animation
            /// </summary>
            Idle,

            /// <summary>
            /// Move forward animation
            /// </summary>
            Forward,

            /// <summary>
            /// Move backward animation
            /// </summary>
            Backward,

            /// <summary>
            /// Strafe left animation
            /// </summary>
            StrafeLeft,

            /// <summary>
            /// Strafe right animation
            /// </summary>
            StrafeRight
        }
    }
}