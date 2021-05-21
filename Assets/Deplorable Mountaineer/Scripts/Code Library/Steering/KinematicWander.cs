#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class KinematicWander : IMovement {
        public KinematicWander(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; } //Unused

        public SteeringOutput GetSteering(){
            Self.AngularVelocity = Vector3.zero;
            Self.Velocity = Self.Forward*Self.steeringParams.maxSpeed;
            Self.EulerRotation = new Vector3(
                RandomBinomial()*Self.steeringParams.maxWanderPitch,
                RandomBinomial()*Self.steeringParams.maxWanderYaw,
                RandomBinomial()*Self.steeringParams.maxWanderRoll);
            if(Self.steeringParams.keepUpright){
                Vector3 e = Self.EulerAngles;
                e.z = 0;
                Self.EulerAngles = e;
            }

            return default;
        }

        public float RandomBinomial(){
            return Random.value - Random.value;
        }
    }
}