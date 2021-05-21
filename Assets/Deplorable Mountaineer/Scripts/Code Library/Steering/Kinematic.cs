#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    /// Component for a moving actor to allow steering
    /// </summary>
    [SelectionBase]
    public class Kinematic : SimpleKinematicComponent {
        [SerializeField] public MovementType movement;
        [SerializeField] public WeightedBehavior alwaysBehaviorForPriority;
        [SerializeField] public SimpleKinematicComponent steeringTarget;
        [SerializeField] public PathBase path;
        [SerializeField] public float pathOffset = 1;
        [SerializeField] public SteeringParams steeringParams;
        [SerializeField] public SimpleKinematicComponent[] collisionAvoidTargets;
        [SerializeField] private bool automaticSetCollisionAvoidTargets;
        [SerializeField] public List<WeightedBehavior> behaviorBlend;
        [SerializeField] public List<SteeringGroup> priorityGroups;

        private Transform _transform;

        public Vector3 EulerRotation { get; set; }
        public Vector3 Forward => _transform.forward;
        public Vector3 Up => _transform.up;
        public Vector3 Right => _transform.right;
        public Vector3 Backward => -_transform.forward;
        public Vector3 Down => -_transform.up;
        public Vector3 Left => -_transform.right;

        public Dictionary<MovementType, IMovement> Movements { get; } =
            new Dictionary<MovementType, IMovement>();

        protected override void Awake(){
            base.Awake();
            _transform = transform;
            SetUpMovements();
        }

        private void Reset(){
            collisionAvoidTargets = FindObjectsOfType<SimpleKinematicComponent>();
        }

        private void FixedUpdate(){
            SteeringOutput? steering = Movements[movement]?.GetSteering();
            Position += Velocity*Time.deltaTime;
            _transform.position = Position;
            EulerAngles += EulerRotation*Time.deltaTime;
            _transform.eulerAngles = EulerAngles;
            _transform.rotation = Quaternion.AngleAxis(AngularVelocity.magnitude*Mathf.Rad2Deg,
                AngularVelocity.normalized)*_transform.rotation;
            EulerAngles = _transform.eulerAngles;

            AngularVelocity -= Mathf.Pow(steeringParams.angularDrag, Time.deltaTime)*
                               AngularVelocity;
            Velocity -= Mathf.Pow(steeringParams.linearDrag, Time.deltaTime)*Velocity;
            Vector3 d = new Vector3(Mathf.Pow(steeringParams.eulerDrag.x, Time.deltaTime),
                Mathf.Pow(steeringParams.eulerDrag.y, Time.deltaTime),
                Mathf.Pow(steeringParams.eulerDrag.z, Time.deltaTime));
            EulerRotation -= Vector3.Scale(EulerRotation, d);
            if(steering.HasValue){
                if(steering.Value.Linear.HasValue)
                    Velocity += steering.Value.Linear.Value*Time.deltaTime;

                if(steering.Value.Eulers.HasValue)
                    EulerRotation += steering.Value.Eulers.Value*Time.deltaTime;
                if(steering.Value.Angular.HasValue)
                    AngularVelocity += steering.Value.Angular.Value*Time.deltaTime;
            }

            Velocity = Vector3.ClampMagnitude(Velocity, steeringParams.maxSpeed);
            AngularVelocity =
                Vector3.ClampMagnitude(AngularVelocity, steeringParams.maxAngularVelocity);
            Vector3 e = EulerRotation;
            e.x = Math.Sign(e.x)*Mathf.Min(Mathf.Abs(e.x), steeringParams.maxPitchRotation);
            e.y = Math.Sign(e.y)*Mathf.Min(Mathf.Abs(e.y), steeringParams.maxYawRotation);
            e.z = Math.Sign(e.z)*Mathf.Min(Mathf.Abs(e.z), steeringParams.maxRollRotation);
            EulerRotation = e;
        }

        private void OnValidate(){
            if(automaticSetCollisionAvoidTargets){
                automaticSetCollisionAvoidTargets = false;
                collisionAvoidTargets = FindObjectsOfType<SimpleKinematicComponent>();
            }

            steeringParams.eulerDrag.x = Mathf.Clamp01(steeringParams.eulerDrag.x);
            steeringParams.eulerDrag.y = Mathf.Clamp01(steeringParams.eulerDrag.y);
            steeringParams.eulerDrag.z = Mathf.Clamp01(steeringParams.eulerDrag.z);
        }

        public void Look(Vector3 forward, Vector3 up){
            EulerAngles = LookEulers(forward, up);
        }

        public Vector3 LookEulers(Vector3 forward, Vector3 up){
            Vector3 f = forward == default ? _transform.forward : forward;
            Vector3 u = up == default ? _transform.up : up;
            return Quaternion.LookRotation(f, u).eulerAngles;
        }

        private void SetUpMovements(){
            Movements[MovementType.None] = null;
            Movements[MovementType.KinematicStop] = new KinematicStop(this);
            Movements[MovementType.KinematicArrive] = new KinematicArrive(this);
            Movements[MovementType.KinematicFlee] = new KinematicFlee(this);
            Movements[MovementType.KinematicSeek] = new KinematicSeek(this);
            Movements[MovementType.KinematicWander] = new KinematicWander(this);
            Movements[MovementType.Seek] = new Seek(this);
            Movements[MovementType.Flee] = new Flee(this);
            Movements[MovementType.Arrive] = new Arrive(this);
            Movements[MovementType.Pursue] = new Pursue(this);
            Movements[MovementType.PursueArrive] = new PursueArrive(this);
            Movements[MovementType.Evade] = new Evade(this);
            Movements[MovementType.Wander] = new Wander(this);
            Movements[MovementType.MatchVelocity] = new MatchVelocity(this);
            Movements[MovementType.Align] = new Align(this);
            Movements[MovementType.FaceTarget] = new FaceTarget(this);
            Movements[MovementType.FaceVelocity] = new FaceVelocity(this);
            Movements[MovementType.FollowPath] = new FollowPath(this);
            Movements[MovementType.Separation] = new Separation(this);
            Movements[MovementType.Cohesion] = new Cohesion(this);
            Movements[MovementType.CollisionAvoidance] = new CollisionAvoidance(this);
            Movements[MovementType.ObstacleAvoidance] = new ObstacleAvoidance(this);
            Movements[MovementType.BlendedSteering] = new BlendedSteering(this);
            Movements[MovementType.PrioritySteering] = new PrioritySteering(this);
        }
    }

    [Serializable]
    public class SteeringParams {
        [SerializeField] [Range(0, 1)] public float linearDrag;
        [SerializeField] [Range(0, 1)] public float angularDrag = .05f;
        [SerializeField] public Vector3 eulerDrag;
        [SerializeField] public Facing kinematicFacing = Facing.None;
        [SerializeField] public bool keepUpright = true;
        [SerializeField] public float maxAcceleration = 50;
        [SerializeField] public float maxAngularAcceleration = 360;
        [SerializeField] public float maxPitchAcceleration = 360;
        [SerializeField] public float maxYawAcceleration = 720;
        [SerializeField] public float maxRollAcceleration = 360;
        [SerializeField] public float maxSpeed = 1;
        [SerializeField] public float maxWanderPitch = 60;
        [SerializeField] public float maxWanderYaw = 180;
        [SerializeField] public float maxWanderRoll;
        [SerializeField] public float acceptanceRadius = .25f;
        [SerializeField] public float slowRadius = .5f;
        [SerializeField] public float eulerAcceptanceRadius = .5f;
        [SerializeField] public float eulerSlowRadius = 2f;
        [SerializeField] public float timeToTarget = .1f;
        [SerializeField] public float maxPitchRotation = 60;
        [SerializeField] public float maxYawRotation = 180;
        [SerializeField] public float maxRollRotation = 60;
        [SerializeField] public float maxAngularVelocity = 8;
        [SerializeField] public float maxPrediction = 1;
        [SerializeField] public float wanderOffset = 20;
        [SerializeField] public float wanderRadius = 1;
        [SerializeField] public float wanderRate = 5;
        [SerializeField] public float separationThreshold = 4;
        [SerializeField] public float separationDecayCoefficient = 10;
        [SerializeField] public float avoidanceLookAhead = 2;
        [SerializeField] public float avoidDistance = .5f;
        [SerializeField] public float priorityEpsilon = .1f;
    }

    public enum Facing {
        None,
        FaceVelocity,
        FaceTarget
    }

    public enum MovementType {
        None,
        KinematicStop,
        KinematicSeek,
        KinematicFlee,
        KinematicArrive,
        KinematicWander,
        Seek,
        Flee,
        Arrive,
        Pursue,
        PursueArrive,
        Evade,
        Wander,
        MatchVelocity,
        Align,
        FaceTarget,
        FaceVelocity,
        FollowPath,
        Separation,
        Cohesion,
        CollisionAvoidance,
        ObstacleAvoidance,
        BlendedSteering,
        PrioritySteering
    }
}