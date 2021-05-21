#region

using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    /// Component for a moving actor to allow steering
    /// </summary>
    public class SimpleKinematicComponent : MonoBehaviour, IKinematic {
        [SerializeField] private float radius = 1;
        private Vector3 _angularVelocity;
        private Rigidbody _rigidbody;
        private Vector3 _velocity = Vector3.zero;

        /// <summary>
        /// The flock this body is a member of, or null if a loner
        /// </summary>
        public Flock InFlock { get; set; }

        protected virtual void Awake(){
            _rigidbody = GetComponent<Rigidbody>();
            Radius = radius;
        }

        public Vector3 Position {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 EulerAngles {
            get => transform.eulerAngles;
            set => transform.eulerAngles = value;
        }

        public Vector3 Velocity {
            get => _rigidbody ? _rigidbody.velocity : _velocity;
            set {
                if(_rigidbody) _rigidbody.velocity = value;
                else _velocity = value;
            }
        }

        public Vector3 AngularVelocity {
            get => _rigidbody ? _rigidbody.angularVelocity : _angularVelocity;
            set {
                if(_rigidbody) _rigidbody.angularVelocity = value;
                else _angularVelocity = value;
            }
        }

        public float Radius { get; private set; }
        public string Name => name;
    }
}