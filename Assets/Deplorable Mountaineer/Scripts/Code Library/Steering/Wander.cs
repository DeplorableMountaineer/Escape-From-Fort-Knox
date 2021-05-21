#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Wander : IMovement {
        private readonly IMovement _seek;
        private float _wanderOrientation;

        public Wander(Kinematic self){
            Self = self;
            _seek = new Seek(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _wanderOrientation += RandomBinomial()*Self.steeringParams.wanderRate;
            float targetOrientation = (_wanderOrientation + Self.EulerAngles.y)*Mathf.Deg2Rad;
            Vector3 target = Self.Position + Self.steeringParams.wanderOffset*Self.Forward;
            target += Self.steeringParams.wanderRadius*new Vector3(
                Mathf.Sin(targetOrientation),
                Mathf.Sin(RandomBinomial()*Self.steeringParams.maxWanderPitch*Mathf.Deg2Rad),
                Mathf.Cos(targetOrientation));
            _seek.OverrideTarget.Position = target;
            return _seek.GetSteering();
        }

        public float RandomBinomial(){
            return Random.value - Random.value;
        }
    }
}