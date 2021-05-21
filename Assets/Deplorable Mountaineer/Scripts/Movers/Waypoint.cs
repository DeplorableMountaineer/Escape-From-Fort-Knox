using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;

namespace Deplorable_Mountaineer.Movers {
    public class Waypoint : MonoBehaviour {
        [SerializeField] private bool usePosition = true;
        [SerializeField] private bool useRotation;
        [SerializeField] private bool useScale;

        [SerializeField] private bool useIncomingDirection;

        [ShowWhen(nameof(useIncomingDirection))] [SerializeField]
        private Vector3 incomingDirection = Vector3.back;

        [SerializeField] private bool useOutgoingDirection;

        [ShowWhen(nameof(useOutgoingDirection))] [SerializeField]
        private Vector3 outgoingDirection = Vector3.forward;

        [SerializeField] private bool useHoldTime;

        [ShowWhen(nameof(useHoldTime))] [SerializeField] [Min(0)]
        private float holdTime = 5;

        [ShowWhen(nameof(useHoldTime), false)] [SerializeField]
        private bool useIncomingSpeed;

        [HideInInspector] [SerializeField] private bool isUsingIncomingSpeed;

        [ShowWhen(nameof(isUsingIncomingSpeed))] [SerializeField] [Min(0)]
        private float incomingSpeed = 1;

        [SerializeField] private bool useOutgoingSpeed;

        [ShowWhen(nameof(useOutgoingSpeed))] [SerializeField] [Min(0)]
        private float outgoingSpeed = 1;

        [SerializeField] private bool jump;

        private Transform _transform;

        public Vector3? Position => usePosition ? (Vector3?) _transform.position : null;
        public Quaternion? Rotation => useRotation ? (Quaternion?) _transform.rotation : null;
        public Vector3? Scale => useScale ? (Vector3?) _transform.localScale : null;

        public Vector3? IncomingDirection =>
            useIncomingDirection ? (Vector3?) incomingDirection : null;

        public Vector3? OutgoingDirection =>
            useOutgoingDirection ? (Vector3?) outgoingDirection : null;

        public float HoldTime => useHoldTime ? holdTime : 0;
        public float? IncomingSpeed => isUsingIncomingSpeed ? (float?) incomingSpeed : null;
        public float? OutgoingSpeed => useOutgoingSpeed ? (float?) outgoingSpeed : null;
        public bool Jump => jump;

        private void OnDrawGizmosSelected(){
            _transform = transform;
            Vector3 position = _transform.position;
            float speed = 1;
            if(useIncomingDirection && incomingDirection.magnitude > Mathf.Epsilon){
                if(isUsingIncomingSpeed && incomingSpeed > .1f) speed = incomingSpeed;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(position, position + incomingDirection.normalized*speed);
            }

            if(useOutgoingDirection && outgoingDirection.magnitude > Mathf.Epsilon){
                speed = useOutgoingSpeed && outgoingSpeed > .1f ? outgoingSpeed : 1;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(position, position + outgoingDirection.normalized*speed);
            }
        }

        private void OnValidate(){
            isUsingIncomingSpeed = !useHoldTime && useIncomingSpeed;
        }

        private void Awake(){
            _transform = transform;
        }
    }
}