#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class MatchVelocity : IMovement {
        private IKinematic _target;

        public MatchVelocity(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }


        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            return new SteeringOutput {
                Linear = Vector3.ClampMagnitude(
                    (_target.Velocity - Self.Velocity)/Self.steeringParams.timeToTarget,
                    Self.steeringParams.maxAcceleration)
            };
        }
    }
}