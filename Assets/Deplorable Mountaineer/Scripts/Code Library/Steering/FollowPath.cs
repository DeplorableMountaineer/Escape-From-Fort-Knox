#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class FollowPath : IMovement {
        private readonly IMovement _seek;
        private float _param;

        public FollowPath(Kinematic self){
            Self = self;
            _seek = new Seek(self) {OverrideTarget = new SimpleKinematic()};
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            _param = Self.path.GetParam(Self.Position, _param);
            float targetParam = _param + Self.pathOffset;
            Vector3 targetPosition = Self.path.GetPosition(targetParam);
            _seek.OverrideTarget.Position = targetPosition;
            return _seek.GetSteering();
        }
    }
}