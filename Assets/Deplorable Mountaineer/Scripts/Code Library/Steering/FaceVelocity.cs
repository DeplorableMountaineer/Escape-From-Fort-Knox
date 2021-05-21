#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class FaceVelocity : IMovement {
        private readonly IMovement _align;
        private IKinematic _target;

        public FaceVelocity(Kinematic self){
            Self = self;
            _align = new Align(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            if(Self.Velocity.normalized.magnitude < Mathf.Epsilon)
                return default;
            float dp = Vector3.Dot(Self.Velocity.normalized, Vector3.up);
            if(dp > 1 - Mathf.Epsilon || dp < -1 + Mathf.Epsilon){
                Vector3 up = Self.Up;
                if(Vector3.Distance(up, Vector3.up) < Mathf.Epsilon)
                    up = Self.Backward;
                _align.OverrideTarget.EulerAngles = Quaternion.LookRotation(Self.Velocity,
                    up).eulerAngles;
            }
            else{
                _align.OverrideTarget.EulerAngles = Quaternion.LookRotation(Self.Velocity,
                    Vector3.up).eulerAngles;
            }

            return _align.GetSteering();
        }
    }
}