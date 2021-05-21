using System;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Movement controller for a doom-like humanoid character
    /// 
    /// Tries to handle going up/down stairs, but you get smoother
    /// motion if you use a ramp-shaped collider on steps.
    /// </summary>
    [SelectionBase, RequireComponent(typeof(Collider),
         typeof(Rigidbody))]
    public class MovementController : MovementControllerBase {
        [SerializeField] private MovementConstraints movementConstraints;

        [Tooltip("Transform that gets aimed vertically;" +
                 " attaches to camera and first person gun")]
        [SerializeField]
        private Transform head;

        [Tooltip("Another transform that gets aimed vertically; " +
                 "attaches to third person gun")]
        [SerializeField]
        private Transform thirdPersonGun;

        [Tooltip("Layers to block movement and walk on")] [SerializeField]
        private LayerMask colliderMask = Physics.AllLayers;

        [Tooltip("added to ground check raycast to be robust against floating point errors")]
        [SerializeField]
        private float skinWidth = .01f;

        [Tooltip("Try to protect from walking off ledge when crouched")] [SerializeField]
        private bool crouchWalkLedgeProtection = true;

        /// <summary>
        /// Transform component of character
        /// </summary>
        private Transform _transform;

        /// <summary>
        /// Rigidbody (nonkinematic, with rotation frozen) component
        /// </summary>
        private Rigidbody _rigidbody;

        /// <summary>
        /// Character capsule collider modelling the character's motion collisions
        /// </summary>
        private CapsuleCollider _collider;

        /// <summary>
        /// computed from max walkable slope in movement constraints; minimum
        /// y value of ground normal for slope to be walkable
        /// </summary>
        private float _minNormalY;

        /// <summary>
        /// Maximum distance from bottom of capsule to ground on a walkable slope.
        /// Distance is nonzero when ground slopes and a part of the capsule other than
        /// the bottommost point is touching the ground.
        /// </summary>
        private float _maxCapsuleGroundDist;

        /// <summary>
        /// Current direction the character is facing in degrees.  0=along world z axis.
        /// Positive is clockwise rotation. 
        /// </summary>
        private float _orientation;

        /// <summary>
        /// Current aim direction of character's head/camera/gun in degrees.  0=horizontal.
        /// 90=straight up.  -90=straight down.
        /// </summary>
        private float _aim;

        private Vector3 _desiredVelocity;

        /// <summary>
        /// Distance from ground of point on capsule closest to ground (might not be
        /// bottom point if ground is sloped) at last ground check; inaccurate
        /// for unwalkable slopes;  Infinite if ground too far away.
        /// </summary>
        private float _distFromGround;

        /// <summary>
        /// Current controlled direction of motion.  May differ from actual motion based
        /// on gravity, external forces, and collision.
        /// </summary>
        private Vector3 _moveDirection;

        /// <summary>
        /// current controlled move speed, based on whether running or walking, or ungrounded
        /// with partial air control.  May differ from actual based on gravity, external forces,
        /// and collision.
        /// </summary>
        private float _moveSpeed;

        /// <summary>
        /// Ground unit normal vector last ground check. 
        /// </summary>
        private Vector3 _groundNormal;

        /// <summary>
        /// True if stepped up before ground check
        /// </summary>
        private bool _steppedUp;

        /// <summary>
        /// force character to act as ungrounded until this time
        /// </summary>
        private float _ungroundUntil;

        /// <summary>
        /// Add motion to character that automatically expires
        /// </summary>
        private Vector3 _additionalMotion;

        /// <summary>
        /// Expiration time of additional motion 
        /// </summary>
        private float _additionalMotionExpiration;

        /// <summary>
        /// To restore character when uncrouching; height of capsule when not crouched.
        /// </summary>
        private float _standingCapsuleHeight;

        /// <summary>
        /// To restore character when uncrouching; local position of head when not crouched.
        /// </summary>
        private Vector3 _standingHeadLocalPosition;

        /// <summary>
        /// To restore character when uncrouching; local position of third person gun when not crouched.
        /// </summary>
        private Vector3 _standingGun3PLocalPosition;

        /// <summary>
        /// To restore character when uncrouching; local position of capsule center when not crouched.
        /// </summary>
        private Vector3 _standingCapsuleCenter;

        /// <summary>
        /// To shrink character when crouching; height of capsule when crouched.
        /// </summary>
        private float _crouchingCapsuleHeight;

        /// <summary>
        /// To shrink character when crouching; local position of head when crouched.
        /// </summary>
        private Vector3 _crouchingHeadLocalPosition;

        /// <summary>
        /// To shrink character when crouching; local position of third person gun when crouched.
        /// </summary>
        private Vector3 _crouchingGun3PLocalPosition;

        /// <summary>
        /// To shrink character when crouching; local position of capsule center when crouched.
        /// </summary>
        private Vector3 _crouchingCapsuleCenter;

        /// <summary>
        /// If _lowClearance is true, direction to the collision point
        /// </summary>
        private Vector3 _lowClearanceDirection;

        /// <summary>
        /// If true, a collision near the top indicating low clearance
        /// </summary>
        private bool _lowClearance;

        /// <summary>
        /// True if input has running mode selected
        /// </summary>
        public override bool IsRunning { get; set; } = true;

        /// <summary>
        /// True if input requests crouching
        /// </summary>
        public override bool WantsToCrouch { get; set; }

        /// <summary>
        /// True if actually crouching
        /// </summary>
        private bool IsCrouching { get; set; }

        /// <summary>
        /// True if last ground check shows character is moving on ground
        /// </summary>
        private bool IsGrounded { get; set; } = true;

        /// <summary>
        /// True if last ground check shows character is on a walkable slope (not
        /// too steep).
        /// </summary>
        private bool IsWalkableSlope { get; set; } = true;

        private void Awake(){
            _transform = transform;
            _collider = GetComponent<CapsuleCollider>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider.contactOffset = .0001f; //needed to prevent some errors
            if(!head && !thirdPersonGun)
                Debug.LogWarning(
                    "Missing head transform or third person " +
                    "gun transform in inspector properties");
        }

        private void OnEnable(){
            _orientation = _transform.localEulerAngles.y;
            _minNormalY = Mathf.Cos(Mathf.Deg2Rad*movementConstraints.maxSlope);
            _maxCapsuleGroundDist = _collider.radius*(1 - _minNormalY)/_minNormalY;
            SetUpCrouchData();
        }

        private void FixedUpdate(){
            UpdateRotation();
            UpdateMovement();
            AdjustFriction();
            if(WantsToCrouch && !IsCrouching) TryCrouch();
            else if(!WantsToCrouch && IsCrouching) TryUncrouch();
        }

        private void OnCollisionEnter(Collision other){
            float minY = Mathf.Infinity;
            _lowClearance = false;
            bool pointFound = false;
            Vector3 minPoint = Vector3.zero;
            for(int i = 0; i < other.contactCount; i++){
                ContactPoint cp = other.GetContact(i);
                if(minY > cp.point.y){
                    minY = cp.point.y;
                    minPoint = cp.point;
                    pointFound = true;
                }
            }

            if(pointFound && minY >= _transform.position.y +
                _collider.height - _collider.radius -
                skinWidth){
                //don't jump through ceiling
                _ungroundUntil = 0;
                _additionalMotion = Vector3.zero;
                //detect low clearance for auto-crouch
                _lowClearance = true;
                _lowClearanceDirection = minPoint - _transform.position;
                _lowClearanceDirection.y = 0;
                _lowClearanceDirection = _lowClearanceDirection.normalized;
            }

            //handle collision with a moving rigidbody
            if(other.rigidbody == null) return;
            AddMotion(
                Vector3.Project(other.rigidbody.velocity,
                    (_transform.position - other.transform.position).normalized) +
                _additionalMotion, .1f);
            if(other.rigidbody.velocity.magnitude <
               movementConstraints.minUngroundSpeed) return;
            Unground(.1f);
        }

        private void OnCollisionStay(Collision other){
            _ = HandleStepUp(other);
        }

        /// <summary>
        /// Move the character forward/backward/left/right
        /// </summary>
        /// <param name="motion">The motion to apply, y ignored, magnitude of 1 is max speed</param>
        public override void Move(Vector3 motion){
            float speed = IsCrouching
                ? movementConstraints.crouchSpeed
                : IsRunning
                    ? movementConstraints.runSpeed
                    : movementConstraints.walkSpeed;
            if(!IsGrounded || !IsWalkableSlope) speed *= movementConstraints.airControl;
            _moveDirection = _transform.TransformDirection(motion.normalized);
            _moveSpeed = motion.magnitude > .1f ? speed*Mathf.Max(motion.magnitude, 1) : 0;
            if(_moveSpeed < Mathf.Epsilon) _moveDirection = Vector3.zero;
        }

        /// <summary>
        /// Rotate the character horizontally (yaw)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public override void Turn(float amount){
            float mag = Mathf.Abs(amount);
            float angle = (mag > 1) ? amount/mag : amount;
            angle *= movementConstraints.turnSpeed;
            _orientation += angle*Time.fixedDeltaTime;
        }

        /// <summary>
        /// Aim the character's head and gun vertically (pitch)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public override void Aim(float amount){
            float mag = Mathf.Abs(amount);
            float angle = (mag > 1) ? amount/mag : amount;
            angle *= movementConstraints.aimSpeed;
            _aim = Mathf.Clamp(_aim + angle*Time.fixedDeltaTime, -89.9f, 89.9f);
        }

        /// <summary>
        /// If character is grounded on walkable ground, unground and add vertical motion
        /// </summary>
        public override void Jump(){
            if(!IsGrounded || !IsWalkableSlope || IsCrouching || WantsToCrouch) return;
            Unground(movementConstraints.jumpHeight/movementConstraints.jumpSpeed);
            AddMotion(movementConstraints.jumpSpeed*Vector3.up,
                movementConstraints.jumpHeight/movementConstraints.jumpSpeed);
        }

        //Read initial capsule collider parameters and determine what they should be for
        //both crouching and uncrouching; uncrouched parameters are assumed at start
        private void SetUpCrouchData(){
            _standingCapsuleHeight = _collider.height;
            _crouchingCapsuleHeight =
                _standingCapsuleHeight*movementConstraints.crouchRatio;
            _standingCapsuleCenter = _collider.center;
            _crouchingCapsuleCenter = _standingCapsuleCenter + (_crouchingCapsuleHeight/2
                    - _standingCapsuleHeight/2)*
                Vector3.up;
            if(head){
                _standingHeadLocalPosition = head.localPosition;
                Vector3 offsetFromTop =
                    _standingHeadLocalPosition - _standingCapsuleHeight*Vector3.up;
                _crouchingHeadLocalPosition =
                    _crouchingCapsuleHeight*Vector3.up + offsetFromTop;
            }

            if(thirdPersonGun){
                _standingGun3PLocalPosition = thirdPersonGun.localPosition;
                Vector3 offsetFromCenter = _standingGun3PLocalPosition -
                                           _standingCapsuleHeight/2*Vector3.up;
                _crouchingGun3PLocalPosition = _crouchingCapsuleHeight/2*Vector3.up +
                                               offsetFromCenter;
            }
        }

        private void TryCrouch(){
            if(!IsGrounded || !IsWalkableSlope || IsCrouching) return;
            _collider.height = _crouchingCapsuleHeight;
            _collider.center = _crouchingCapsuleCenter;
            if(head) head.localPosition = _crouchingHeadLocalPosition;
            if(thirdPersonGun) thirdPersonGun.localPosition = _crouchingGun3PLocalPosition;
            IsCrouching = true;
        }

        private void TryUncrouch(){
            if(!IsCrouching || !HasHeadClearance()) return;
            _collider.height = _standingCapsuleHeight;
            _collider.center = _standingCapsuleCenter;
            if(head) head.localPosition = _standingHeadLocalPosition;
            if(thirdPersonGun) thirdPersonGun.localPosition = _standingGun3PLocalPosition;
            IsCrouching = false;
        }

        private bool HasHeadClearance(){
            Vector3 origin = _crouchingCapsuleCenter + _transform.position;
            float radius = _collider.radius;
            float distance = _standingCapsuleHeight - radius - _crouchingCapsuleHeight/2 +
                             skinWidth;
            bool hasClearance = !Physics.SphereCast(origin, radius, Vector3.up,
                out RaycastHit _, distance, colliderMask, QueryTriggerInteraction.Ignore);
            return hasClearance;
        }

        /// <summary>
        /// Temporarily unstick the character from the ground; overrides any
        /// pending duration, rather than adding to it.
        /// </summary>
        /// <param name="duration">How long before ungrounding expires</param>
        private void Unground(float duration){
            _ungroundUntil = Time.time + duration;
        }

        /// <summary>
        /// Add temporary additional motion to the character.  Overrides any
        /// previous additional motion.
        /// </summary>
        /// <param name="amount">Velocity change</param>
        /// <param name="duration">How long before additional motion expires</param>
        private void AddMotion(Vector3 amount, float duration){
            _additionalMotion = amount;
            _additionalMotionExpiration = Time.time + duration;
        }

        /// <summary>
        /// Add drag when character is standing still on walkable ground to prevent
        /// sliding down slopes
        /// </summary>
        private void AdjustFriction(){
            if(_moveSpeed < Mathf.Epsilon && IsGrounded && IsWalkableSlope &&
               _distFromGround <= movementConstraints.stepHeight){
                _rigidbody.drag = movementConstraints.standingDrag;
            }
            else{
                _rigidbody.drag = movementConstraints.movingDrag;
            }
        }

        /// <summary>
        /// Convert intended motion to actual velocity on possibly-sloped walkable ground
        /// </summary>
        /// <param name="groundNormal">Unit normal vector of the ground</param>
        /// <returns></returns>
        private Vector3 GetWalkableVelocity(Vector3 groundNormal){
            return _moveSpeed > .1f
                ? Vector3.ProjectOnPlane(_moveDirection, groundNormal).normalized*
                  _moveSpeed
                : Vector3.zero;
        }

        private void UpdateRotation(){
            //rotate to desired orientation
            _transform.localEulerAngles = new Vector3(0, _orientation, 0);
            //aim in desired aim direction
            if(head) head.localEulerAngles = new Vector3(-_aim, 0, 0);
            if(thirdPersonGun) thirdPersonGun.localEulerAngles = new Vector3(0, -_aim, 0);
        }

        private bool HandleStepUp(Collision other){
            //don't step on self
            if(other.transform.root == transform.root) return false;

            //don't step up if no controlled motion
            if(_moveSpeed < Mathf.Epsilon) return false;

            //compute step height from collision
            float stepHeight = 0;
            Vector3 position = _transform.position;
            for(int i = 0; i < other.contactCount; i++){
                ContactPoint cp = other.GetContact(i);
                Vector3 point = cp.point;
                Vector3 direction = point - position;
                float height = direction.y;
                direction.y = 0;
                //don't step up if not moving toward step
                if(Vector3.Dot(direction, _moveDirection) <= 0) continue;
                //step up only if within movement constraints
                if(height < 0 || height > movementConstraints.stepHeight) continue;
                Vector3 normal = cp.normal;
                //don't step up onto too steep a slope
                if(normal.y < _minNormalY) continue;
                //highest steppable contact point determines the step height
                stepHeight = Mathf.Max(stepHeight, height);
            }

            if(stepHeight < Mathf.Epsilon) return false;
            //move character up to step level
            _steppedUp = true;
            _transform.position += Vector3.up*(stepHeight + skinWidth) +
                                   _moveDirection*_collider.radius;
            //reapply velocity taken away by collision
            _rigidbody.velocity = _desiredVelocity;
            return true;
        }

        private void UpdateMovement(){
            //Determine if grounded
            _distFromGround = DistanceToWalkableGround(out _groundNormal);
            //Don't unground just from stepping up
            IsGrounded = Time.time >= _ungroundUntil &&
                         (_distFromGround < skinWidth || _steppedUp);
            _steppedUp = false;
            //Determine if slope is not too steep to walk on
            IsWalkableSlope = _groundNormal.y > _minNormalY;

            //Get velocity along plane of slope if walkable
            if(IsWalkableSlope) _desiredVelocity = GetWalkableVelocity(_groundNormal);

            //Move with air control if ungrounded, but slide down unwalkable slopes while falling
            if(!IsGrounded || !IsWalkableSlope){
                //only restrict control when higher than step height from walkable slope
                if(_distFromGround > movementConstraints.stepHeight || !IsWalkableSlope){
                    _desiredVelocity =
                        _moveDirection*(_moveSpeed*movementConstraints.airControl);
                    HandleUnwalkableSlope();
                }

                if(Time.time >= _ungroundUntil) Fall();
            }

            //apply temporary motion
            if(_additionalMotion.magnitude > Mathf.Epsilon)
                _desiredVelocity += _additionalMotion;
            if(Time.time >= _additionalMotionExpiration){
                //frame-rate-independent braking 
                _additionalMotion *=
                    1 - Mathf.Pow(movementConstraints.additionalMotionBrakeRate,
                        Time.deltaTime);
            }

            if(_lowClearance){
                float dot = Vector3.Dot(_lowClearanceDirection, _desiredVelocity);
                //if not crouching, try to crouch; otherwise don't keep walking
                //into obstruction (necessary to prevent character getting
                //forced into floor)
                if(dot > 0){
                    if(IsCrouching){
                        _desiredVelocity -= 2*dot*_lowClearanceDirection;
                    }
                    else TryCrouch();
                }

                _lowClearance = false;
            }

            if(crouchWalkLedgeProtection && IsCrouching && WantsToCrouch){
                LedgeProtection();
            }

            //Apply velocity
            _rigidbody.velocity = _desiredVelocity;
        }

        private void LedgeProtection(){
            //if crouched (and not just autocrouched), protect from walking off ledges
            Vector3 direction = _desiredVelocity.normalized;
            Vector3 origin = _collider.bounds.center + direction*_collider.radius;
            float distance = _collider.height + movementConstraints.stepHeight;
            if(Physics.Raycast(origin, Vector3.down, distance, colliderMask,
                QueryTriggerInteraction.Ignore)) return;
            _desiredVelocity.x = 0;
            _desiredVelocity.z = 0;
        }

        private void Fall(){
            //allow Rigidbody's gravity to affect character if falling
            if(_rigidbody.velocity.y < 0)
                _desiredVelocity.y = _rigidbody.velocity.y;
        }

        private void HandleUnwalkableSlope(){
            //Make sure there is a slope below using spherecast, which
            //handles cases missed by raycasts
            bool isHit = Physics.SphereCast(_collider.bounds.center,
                _collider.radius, Vector3.down,
                out RaycastHit hit, _collider.height/2 + skinWidth - _collider.radius,
                colliderMask, QueryTriggerInteraction.Ignore);
            if(!isHit) return;
            //slide down slope if there is a slope
            Vector3 upSlope = hit.point - _collider.bounds.center;
            upSlope.y = 0;
            upSlope = upSlope.normalized;
            float c = Vector3.Dot(upSlope, _desiredVelocity);
            if(c > 0) _desiredVelocity -= c*upSlope;
        }

        private float DistanceToWalkableGround(out Vector3 normal){
            //raycast to find ground; may miss if slope not walkable
            bool isHit = Physics.Raycast(_collider.bounds.center, Vector3.down,
                out RaycastHit hit,
                _collider.height/2 + _maxCapsuleGroundDist + skinWidth +
                movementConstraints.stepHeight,
                colliderMask, QueryTriggerInteraction.Ignore);
            if(!isHit){
                normal = Vector3.zero;
                return Mathf.Infinity;
            }

            //find actual distance, even if ground is sloped
            normal = hit.normal;
            float dist = hit.distance - _collider.height/2 -
                         _collider.radius*(1 - hit.normal.y)/
                         hit.normal.y;
            return dist < skinWidth ? 0 : dist;
        }

        [Serializable]
        private class MovementConstraints {
            [Tooltip("Horizontal speed in walk mode")] [SerializeField]
            public float walkSpeed = 6;

            [Tooltip("Horizontal speed in crouch mode")] [SerializeField]
            public float crouchSpeed = 3;

            [Tooltip("Horizontal speed in run mode")] [SerializeField]
            public float runSpeed = 10;

            [Tooltip("Horizontal (yaw) rotation speed")] [SerializeField]
            public float turnSpeed = 360;

            [Tooltip("Vertical (pitch) rotation speed")] [SerializeField]
            public float aimSpeed = 90;

            [Tooltip("Height of an unobstructed jump from walkable ground; " +
                     "actual will be a little higher depending on additional " +
                     "motion brake rate")]
            [SerializeField]
            public float jumpHeight = 2;

            [Tooltip("Vertical speed of jump")] [SerializeField]
            public float jumpSpeed = 10;

            [Tooltip("When crouched, character's height is this fraction of normal height")]
            [SerializeField]
            public float crouchRatio = .5f;

            [Tooltip("When ungrounded, multiply run/walk speeds by this")] [SerializeField]
            public float airControl = .5f;

            [Tooltip("Automatically step up if vertical distance is less than this amount")]
            [SerializeField]
            public float stepHeight = .3f;

            [Tooltip("Allow walking on slopes up to this many degrees")] [SerializeField]
            public float maxSlope = 45;

            [Tooltip("Use high enough value to prevent sliding down slopes when not moving")]
            [SerializeField]
            public float standingDrag = 50;

            [Tooltip(
                "Use low enough a nonnegative value to prevent sticking to walls when falling")]
            [SerializeField]
            public float movingDrag = 0;

            [Tooltip(
                "When additional motion, such as jumping, is applied, remove this fraction" +
                "of velocity per second when the motion expires")]
            [SerializeField]
            public float additionalMotionBrakeRate = .5f;

            [Tooltip(
                "Being hit by a rigid body going at least this fast will unground the character")]
            [SerializeField]
            public float minUngroundSpeed = 10;
        }
    }
}