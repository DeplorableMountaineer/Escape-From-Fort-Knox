namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class Cohesion : IMovement {
        private readonly IMovement _arrive;

        public Cohesion(Kinematic self){
            Self = self;
            _arrive = new Arrive(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; } //unused

        public SteeringOutput GetSteering(){
            if(!Self.InFlock) return default;
            _arrive.OverrideTarget.Position = Self.InFlock.CenterOfMass;
            return _arrive.GetSteering();
        }
    }
}