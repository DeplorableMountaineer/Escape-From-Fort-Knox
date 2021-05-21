#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class PursueArrive : IMovement {
        private readonly IMovement _arrive;
        private IKinematic _target;

        public PursueArrive(Kinematic self){
            Self = self;
            _arrive = new Arrive(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;
            Vector3 direction = _target.Position - Self.Position;
            float distance = direction.magnitude;
            float speed = Self.Velocity.magnitude;
            float prediction = Self.steeringParams.maxPrediction;
            if(speed > distance/Self.steeringParams.maxPrediction)
                prediction = distance/speed;
            _arrive.OverrideTarget.Position = _target.Position + _target.Velocity*prediction;
            return _arrive.GetSteering();
        }
    }
}