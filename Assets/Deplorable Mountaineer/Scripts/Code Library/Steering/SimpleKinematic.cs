#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class SimpleKinematic : IKinematic {
        public Vector3 Position { get; set; }
        public Vector3 EulerAngles { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public float Radius { get; set; } = 1;
        public string Name { get; } = "";
    }
}