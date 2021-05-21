using UnityEngine;

namespace Deplorable_Mountaineer {
    public class VelocityFinder : MonoBehaviour {
        private Rigidbody _rigidbody;
        private Transform _transform;
        private Vector3 _lastPosition;

        public Vector3 Velocity { get; private set; }
        public Vector3 DeltaPosition { get; private set; }

        private void Awake(){
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable(){
            _lastPosition = _transform.position;
        }

        private void OnDisable(){
            Velocity = Vector3.zero;
            DeltaPosition = Vector3.zero;
        }
        
        private void FixedUpdate(){
            Vector3 currentPosition = _transform.position;
            DeltaPosition = currentPosition - _lastPosition;
            _lastPosition = currentPosition;
            if(_rigidbody && !_rigidbody.isKinematic){
                Velocity = _rigidbody.velocity;
                return;
            }

            Velocity = DeltaPosition/Time.deltaTime;
        }
    }
}