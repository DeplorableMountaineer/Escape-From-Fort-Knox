#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Separation : IMovement {
        public Separation(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }


        public SteeringOutput GetSteering(){
            Vector3 linear = Vector3.zero;
            foreach(IKinematic target in Self.collisionAvoidTargets){
                if(ReferenceEquals(target, Self)) continue;
                Vector3 direction = target.Position - Self.Position;
                float distance = direction.magnitude;
                if(distance > Self.steeringParams.separationThreshold) continue;
                float strength = Mathf.Min(Self.steeringParams.separationDecayCoefficient/
                                           (distance*distance),
                    Self.steeringParams.maxAcceleration);
                linear += strength*direction/distance;
            }

            return new SteeringOutput {Linear = linear};
        }
    }
}