using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Mover {
    public class MovingPlatform : MonoBehaviour {
        [SerializeField] private WaypointCircuit circuit;
        [SerializeField] private float speed = 1;
        [SerializeField] private float pauseAtWaypoint = 5;
        [SerializeField] private float rotationSpeed = 0;
        [SerializeField] private bool teleportToFirstWaypoint = false;
        private Transform _transform;
        private float _pauseTime = 0;

        private void Reset(){
            circuit = FindObjectOfType<WaypointCircuit>();
        }

        private void Awake(){
            _transform = transform;
            if(circuit && teleportToFirstWaypoint) _transform.position = circuit.GetPosition();
        }

        private void FixedUpdate(){
            if(Mathf.Abs(rotationSpeed) > Mathf.Epsilon){
                Vector3 e = _transform.eulerAngles;
                e.y += rotationSpeed*Time.deltaTime;
                _transform.eulerAngles = e;
            }

            if(!circuit || circuit.Count == 0) return;
            if(Vector3.Distance(_transform.position, circuit.GetPosition()) < Mathf.Epsilon){
                circuit.NextWaypoint();
                _pauseTime = pauseAtWaypoint;
            }

            if(_pauseTime > 0){
                _pauseTime -= Time.deltaTime;
                return;
            }

            _transform.position = Vector3.MoveTowards(_transform.position,
                circuit.GetPosition(), speed*Time.deltaTime);
        }
    }
}