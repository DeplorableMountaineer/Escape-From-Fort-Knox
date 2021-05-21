#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class ObstacleAvoidance : IMovement {
        private readonly IMovement _seek;

        public ObstacleAvoidance(Kinematic self){
            Self = self;
            _seek = new Seek(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            Vector3 direction = Self.Velocity;
            float distance = direction.magnitude;
            direction /= distance;
            distance *= Self.steeringParams.avoidanceLookAhead;
            if(!Physics.SphereCast(Self.Position, Self.Radius, direction, out RaycastHit hit,
                distance)) return default;
            _seek.OverrideTarget.Position = hit.point +
                                            hit.normal*(Self.steeringParams.avoidDistance +
                                                        Self.Radius);
            return _seek.GetSteering();
        }
    }
}