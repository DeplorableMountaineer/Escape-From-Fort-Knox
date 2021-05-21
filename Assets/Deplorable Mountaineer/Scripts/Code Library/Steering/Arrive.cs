#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Arrive : IMovement {
        private IKinematic _target;

        public Arrive(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            SteeringOutput result = new SteeringOutput();
            Vector3 direction = _target.Position - Self.Position;
            float distance = direction.magnitude;
            if(distance < Self.steeringParams.acceptanceRadius){
                //prevent oscillation
                result.Linear = Vector3.ClampMagnitude(
                    -Self.Velocity/Self.steeringParams.timeToTarget,
                    Self.steeringParams.maxAcceleration);
                return result;
            }

            float targetSpeed;
            if(distance > Self.steeringParams.slowRadius)
                targetSpeed = Self.steeringParams.maxSpeed;
            else
                targetSpeed = Self.steeringParams.maxSpeed*distance/
                              Self.steeringParams.slowRadius;
            Vector3 targetVelocity = direction.normalized*targetSpeed;
            result.Linear =
                Vector3.ClampMagnitude(
                    (targetVelocity - Self.Velocity)/Self.steeringParams.timeToTarget,
                    Self.steeringParams.maxAcceleration);
            return result;
        }
    }
}