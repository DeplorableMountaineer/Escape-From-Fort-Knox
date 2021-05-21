#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class CollisionAvoidance : IMovement {
        public CollisionAvoidance(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }


        public SteeringOutput GetSteering(){
            float shortestTime = Mathf.Infinity;
            IKinematic firstTarget = null;
            float firstMinSeparation = 0;
            float firstDistance = 0;
            Vector3 firstRelativePos = Vector3.zero;
            Vector3 firstRelativeVel = Vector3.zero;

            foreach(IKinematic target in Self.collisionAvoidTargets){
                if(ReferenceEquals(target, Self)) continue;
                Vector3 relativePos = Self.Position - target.Position;
                Vector3 relativeVel = target.Velocity - Self.Velocity;
                float relativeSpeed = relativeVel.magnitude;
                if(relativeSpeed < Mathf.Epsilon) continue;
                float timeToCollision = Vector3.Dot(relativePos, relativeVel)/
                                        (relativeSpeed*relativeSpeed);
                float distance = relativePos.magnitude;
                float minSeparation = distance - relativeSpeed*timeToCollision;
                if(minSeparation > Self.Radius + target.Radius) continue;
                if(timeToCollision <= 0 || timeToCollision >= shortestTime) continue;
                shortestTime = timeToCollision;
                firstTarget = target;
                firstMinSeparation = minSeparation;
                firstDistance = distance;
                firstRelativePos = relativePos;
                firstRelativeVel = relativeVel;
            }

            if(firstTarget == null) return default;

            if(firstMinSeparation <= 0 || firstDistance < Self.Radius + firstTarget.Radius)
                firstRelativePos = firstTarget.Position - Self.Position;
            else
                firstRelativePos += firstRelativeVel*shortestTime;

            return new SteeringOutput {
                Linear = firstRelativePos*Self.steeringParams.maxAcceleration
            };
        }
    }
}