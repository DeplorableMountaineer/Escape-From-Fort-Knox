#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class BlendedSteering : IMovement {
        public BlendedSteering(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            return GetSteeringBlend(Self.behaviorBlend, Self);
        }

        public static SteeringOutput GetSteeringBlend(List<WeightedBehavior> blend,
            Kinematic self){
            SteeringOutput result = new SteeringOutput();
            foreach(WeightedBehavior b in blend){
                SteeringOutput s = self.Movements[b.behavior].GetSteering();
                result += s*b.weight;
            }

            if(result.Linear.HasValue)
                result.Linear = Vector3.ClampMagnitude(result.Linear.Value,
                    self.steeringParams.maxAcceleration);

            if(result.Eulers.HasValue){
                Vector3 e = result.Eulers.Value;
                e.x = Mathf.Sign(e.x)*Mathf.Max(Mathf.Abs(e.x),
                    self.steeringParams.maxPitchAcceleration);
                e.y = Mathf.Sign(e.y)*Mathf.Max(Mathf.Abs(e.y),
                    self.steeringParams.maxYawAcceleration);
                e.z = Mathf.Sign(e.z)*Mathf.Max(Mathf.Abs(e.z),
                    self.steeringParams.maxRollAcceleration);
                result.Eulers = e;
            }

            if(result.Angular.HasValue)
                result.Angular = Vector3.ClampMagnitude(result.Angular.Value,
                    self.steeringParams.maxAngularAcceleration);
            return result;
        }
    }

    [Serializable]
    public class WeightedBehavior {
        public MovementType behavior;
        public float weight;
    }
}