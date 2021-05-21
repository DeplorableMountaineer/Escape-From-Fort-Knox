#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Align : IMovement {
        private IKinematic _target;

        public Align(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            Vector3 targetOrientation = _target.EulerAngles;
            Vector3 result = default;
            result.x = GetAlignSteering(Self.EulerRotation.x, targetOrientation.x,
                Self.EulerAngles.x, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxPitchRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxPitchAcceleration);
            result.y = GetAlignSteering(Self.EulerRotation.y, targetOrientation.y,
                Self.EulerAngles.y, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxYawRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxYawAcceleration);
            result.z = GetAlignSteering(Self.EulerRotation.z, targetOrientation.z,
                Self.EulerAngles.z, Self.steeringParams.eulerAcceptanceRadius,
                Self.steeringParams.eulerSlowRadius,
                Self.steeringParams.maxRollRotation, Self.steeringParams.timeToTarget,
                Self.steeringParams.maxRollAcceleration);
            return new SteeringOutput {
                Eulers = result
            };
        }

        public static float GetAlignSteering(float selfRotation, float targetOrientation,
            float selfOrientation,
            float acceptanceRadius,
            float slowRadius, float maxRotation, float stoppingTime, float maxAngular){
            float rotation = Mathf.DeltaAngle(selfOrientation, targetOrientation);
            float rotationSize = Mathf.Abs(rotation);
            float targetRotation;
            if(rotationSize > slowRadius)
                targetRotation = maxRotation;
            else if(rotationSize > acceptanceRadius)
                targetRotation = maxRotation*rotationSize/slowRadius;
            else //prevent oscillation
                return -selfRotation/Time.fixedDeltaTime;

            if(rotationSize > Mathf.Epsilon)
                targetRotation *= rotation/rotationSize;
            float result = (targetRotation - selfRotation)/stoppingTime;
            float mag = Mathf.Abs(result);
            if(mag > maxAngular) result *= maxAngular/mag;
            return result;
        }
    }
}