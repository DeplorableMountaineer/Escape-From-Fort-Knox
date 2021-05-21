using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;

namespace Deplorable_Mountaineer.Movers {
    public class Mover : MonoBehaviour {
        [SerializeField] private WaypointCircuit waypointCircuit;
        [SerializeField] private bool useDefaultSpeed = true;

        [ShowWhen(nameof(useDefaultSpeed))] [SerializeField]
        private float defaultSpeed = 1;

        private Waypoint _targetWaypoint;
        private int _computeDistanceFrame;
        private float _distanceToWaypoint;

        public Waypoint TargetWaypoint {
            get => _targetWaypoint;
            set {
                _targetWaypoint = value;
                CopyWaypointValues(_targetWaypoint);
            }
        }

        public float DistanceToWaypoint {
            get {
                if(_computeDistanceFrame == Time.frameCount) return _distanceToWaypoint;
                _computeDistanceFrame = Time.frameCount;
                _distanceToWaypoint = TargetPosition.HasValue
                    ? Vector3.Distance(TargetPosition.Value, transform.position)
                    : Mathf.Infinity;
                if(TargetWaypoint == null || !float.IsInfinity(_distanceToWaypoint))
                    return _distanceToWaypoint;

                _distanceToWaypoint = Vector3.Distance(TargetWaypoint.transform.position,
                    transform.position);
                return _distanceToWaypoint;
            }
        }

        public Vector3? TargetPosition { get; private set; } = null;
        public Quaternion? TargetRotation { get; private set; } = null;
        public Vector3? TargetScale { get; private set; } = null;
        public Vector3? TargetIncomingDirection { get; private set; } = null;
        public Vector3? TargetOutgoingDirection { get; private set; } = null;
        public float TargetHoldTime { get; private set; } = 0;
        public float? TargetIncomingSpeed { get; private set; } = null;
        public float? TargetOutgoingSpeed { get; private set; } = null;
        public bool TargetJump { get; private set; } = false;

        private void Start(){
            TargetWaypoint = waypointCircuit.GetNearestWaypoint(transform.position);
        }

        private void FixedUpdate(){
            //simplest position-only case
            if(TargetPosition.HasValue && useDefaultSpeed && defaultSpeed > Mathf.Epsilon){
                Vector3 position = transform.position;
                position = Vector3.MoveTowards(position, TargetPosition.Value,
                    defaultSpeed*Time.deltaTime);
                transform.position = position;
                if(DistanceToWaypoint < defaultSpeed*Time.deltaTime){
                    TargetWaypoint = waypointCircuit.GetNextWaypoint(TargetWaypoint);
                }
            }

            if(!TargetPosition.HasValue){
                TargetWaypoint = waypointCircuit.GetNearestWaypoint(transform.position);
                Debug.LogWarning("No target waypoint found; disabling");
                if(!TargetPosition.HasValue) enabled = false;
            }
        }

        private void CopyWaypointValues(Waypoint waypoint){
            if(waypoint == null){
                TargetPosition = null;
                TargetRotation = null;
                TargetScale = null;
                TargetIncomingDirection = null;
                TargetOutgoingDirection = null;
                TargetHoldTime = 0;
                TargetIncomingSpeed = null;
                TargetOutgoingSpeed = null;
                TargetJump = false;
                return;
            }

            TargetPosition = waypoint.Position;
            TargetRotation = waypoint.Rotation;
            TargetScale = waypoint.Scale;
            TargetIncomingDirection = waypoint.IncomingDirection?.normalized;
            TargetOutgoingDirection = waypoint.OutgoingDirection?.normalized;
            TargetHoldTime = waypoint.HoldTime > Mathf.Epsilon ? waypoint.HoldTime : 0;
            TargetIncomingSpeed = waypoint.IncomingSpeed;
            TargetOutgoingSpeed = waypoint.OutgoingSpeed;
            TargetJump = waypoint.Jump;
            if(TargetIncomingDirection.HasValue &&
               TargetIncomingDirection.Value.magnitude <= Mathf.Epsilon)
                TargetIncomingDirection = null;
            if(TargetOutgoingDirection.HasValue &&
               TargetOutgoingDirection.Value.magnitude <= Mathf.Epsilon)
                TargetOutgoingDirection = null;
        }
    }
}