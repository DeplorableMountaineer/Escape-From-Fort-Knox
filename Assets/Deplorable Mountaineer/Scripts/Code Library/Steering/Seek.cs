namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Seek : IMovement {
        private IKinematic _target;

        public Seek(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;

            return new SteeringOutput {
                Linear = (_target.Position - Self.Position).normalized*
                         Self.steeringParams.maxAcceleration
            };
        }
    }
}