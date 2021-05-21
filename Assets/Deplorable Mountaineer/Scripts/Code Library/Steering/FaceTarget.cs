#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class FaceTarget : IMovement {
        private IKinematic _target;

        public FaceTarget(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            Vector3 targetOrientation =
                Quaternion.LookRotation(_target.Position - Self.Position,
                    Vector3.up).eulerAngles;
            Vector3 result = default;
            result.x = Align.GetAlignSteering(Self.EulerRotation.x, targetOrientation.x,
                Self.EulerAngles.x, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxPitchRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxPitchAcceleration);
            result.y = Align.GetAlignSteering(Self.EulerRotation.y, targetOrientation.y,
                Self.EulerAngles.y, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxYawRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxYawAcceleration);
            result.z = Align.GetAlignSteering(Self.EulerRotation.z, targetOrientation.z,
                Self.EulerAngles.z, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxRollRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxRollAcceleration);
            return new SteeringOutput {
                Eulers = result
            };
        }
    }
}