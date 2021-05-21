#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class KinematicStop : IMovement {
        public KinematicStop(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; } //unused

        public SteeringOutput GetSteering(){
            if(Self.steeringParams.keepUpright){
                Vector3 e = Self.EulerAngles;
                e.x = 0;
                e.z = 0;
                Self.EulerAngles = e;
            }

            Self.Velocity = Vector3.zero;
            Self.EulerRotation = Vector3.zero;
            Self.AngularVelocity = Vector3.zero;
            return default;
        }
    }
}