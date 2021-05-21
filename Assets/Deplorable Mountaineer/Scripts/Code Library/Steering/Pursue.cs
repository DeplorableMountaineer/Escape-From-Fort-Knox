#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Pursue : IMovement {
        private readonly IMovement _seek;
        private IKinematic _target;

        public Pursue(Kinematic self){
            Self = self;
            _seek = new Seek(self) {OverrideTarget = new SimpleKinematic()};
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
            _seek.OverrideTarget.Position = _target.Position + _target.Velocity*prediction;
            return _seek.GetSteering();
        }
    }
}