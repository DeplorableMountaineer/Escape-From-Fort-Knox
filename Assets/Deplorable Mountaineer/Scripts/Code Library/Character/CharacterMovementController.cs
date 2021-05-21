using System;
using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library.Mover;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    /// <summary>
    /// Character Controller-based Movement controller for a doom-like humanoid character
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnimation))]
    class CharacterMovementController : MovementControllerBase {
        [Tooltip("Layers to block movement and walk on")] [SerializeField]
        private LayerMask colliderMask = Physics.DefaultRaycastLayers;

        [Tooltip("Standing character capsule")] [SerializeField]
        private CapsuleDimensions standingSize = new CapsuleDimensions()
            {center = new Vector3(0, .9f, 0), height = 1.8f, radius = .3f};

        [Tooltip("Crouching character capsule")] [SerializeField]
        private CapsuleDimensions crouchingSize =
            new CapsuleDimensions()
                {center = new Vector3(0, .45f, 0), height = .9f, radius = .3f};

        [Tooltip("Horizontal speed in walk mode")] [SerializeField]
        private float walkSpeed = 6;

        [Tooltip("Horizontal speed in run mode")] [SerializeField]
        private float runSpeed = 10;

        [Tooltip("Horizontal speed in crouch mode")] [SerializeField]
        private float crouchWalkSpeed = 3;

        [Tooltip("Horizontal (yaw) rotation speed")] [SerializeField]
        private float turnSpeed = 360;

        [Tooltip("Vertical (pitch) rotation speed")] [SerializeField]
        private float aimSpeed = 90;

        [Tooltip("Vertical speed of jump; " +
                 "Jump height = jumpSpeed*jumpTime")]
        [SerializeField]
        private float jumpSpeed = 10;

        [Tooltip("Total time for upward movement in jump; " +
                 "Jump height = jumpSpeed*jumpTime")]
        [SerializeField]
        private float jumpTime = .2f;

        [Tooltip("Acceleration of gravity applied to character when falling")]
        [SerializeField]
        [Min(0)]
        private float gravity = 20;

        [Tooltip("Impulse (per character velocity) applied to nonkinematic" +
                 "rigid body when character bumps it")]
        [SerializeField]
        private float bumpImpulse = .1f;

        [Tooltip("Do not consider it landing if falling slower than this speed " +
                 "(so stepping down doesn't count as a landing)")]
        [SerializeField]
        private float minLandSpeed = 10;

        [Tooltip("Damage done if landing after falling longer than this")] [SerializeField]
        private float fallTimeMinDamage = 1;

        [Tooltip("100% damage done if landing after falling longer than thiw")]
        [SerializeField]
        private float fallTimeMaxDamage = 2;

        [Tooltip("Assume fallen out of world if falling longer than this time")]
        [SerializeField]
        private float maxFallTimeAllowed = 10;

        [Tooltip("Motion applied when grounded to stick to ground")] [SerializeField]
        private float stickToGroundVelocity = 1;

        [SerializeField] private Transform transformComponent;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private CharacterTop characterTop;
        [SerializeField] private CharacterHead characterHead;
        [SerializeField] private CharacterCenter characterCenter;
        [SerializeField] private CharacterThirdPersonGun characterThirdPersonGun;
        [SerializeField] private PlayerAnimation playerAnimation;
        [SerializeField] private Health health;
        [SerializeField] private ActorComponentBase actorComponent;

        /// <summary>
        /// Keep current computed direction of motion for this tick
        /// </summary>
        private Vector3 _direction;

        /// <summary>
        /// Keep current computed speed of motion for this tick
        /// </summary>
        private float _speed;

        /// <summary>
        /// Signed turn speed input this tick
        /// </summary>
        private float _turnSpeed;

        /// <summary>
        /// Signed aim speed input this tick
        /// </summary>
        private float _aimSpeed;

        /// <summary>
        /// When jumping, this is set to the time that the vertical boost stops, allowing control
        /// of jump height.  (Jumping is linear rather than accelerating for easier control)
        /// </summary>
        private float _stopJumpTime;

        /// <summary>
        /// Current absolute downward velocity of falling character.  
        /// </summary>
        private float _fallVelocity;

        /// <summary>
        /// Time falling starts, so damage can be computed from time spent falling
        /// </summary>
        private float _startFallTime;

        /// <summary>
        /// True if character was jumping.  Used to force return to ground to be treated
        /// as a landing rather than a step down.
        /// </summary>
        private bool _wasJumping;

        /// <summary>
        /// Time in which auto crouch is released.  When character auto-crouches due to low overhead,
        /// it waits a little before releasing to prevent hysteresis.
        /// </summary>
        private float _autoCrouchReleaseTime;

        /// <summary>
        /// Coroutine for uncrouching with a delay (to prevent hysteresis)
        /// </summary>
        private Coroutine _uncrouchCoroutine = null;

        /// <summary>
        /// Used to compute delta-position added to the character externally (such as with a
        /// moving platform).
        /// </summary>
        private Vector3 _savePosition;

        /// <summary>
        /// Holds colliders overlapping character capsule when checking for collisions 
        /// </summary>
        private readonly Collider[] _colliders = new Collider[5];

        /// <summary>
        /// Name of this actor
        /// </summary>
        public string Name => actorComponent != null ? actorComponent.Name : name;

        /// <summary>
        /// Description of this actor
        /// </summary>
        public string Description => actorComponent != null
            ? actorComponent.Name
            : "Character Controller-based Movement controller" +
              " for a doom-like humanoid character";

        /// <summary>
        /// True if input has running mode selected
        /// </summary>
        public override bool IsRunning { get; set; } = true;

        /// <summary>
        /// True if input requests crouching
        /// </summary>
        public override bool WantsToCrouch { get; set; }

        /// <summary>
        /// True when character is actually crouching
        /// </summary>
        public bool IsCrouching { get; set; }

        /// <summary>
        /// True when character is being boosted vertically in a jump
        /// </summary>
        public bool IsJumping { get; set; }

        /// <summary>
        /// True when character is on the ground
        /// </summary>
        public bool IsGrounded { get; set; }

        private void Reset(){
            transformComponent = transform;
            characterController = GetComponent<CharacterController>();
            characterTop = GetComponentInChildren<CharacterTop>();
            characterHead = GetComponentInChildren<CharacterHead>();
            characterCenter = GetComponentInChildren<CharacterCenter>();
            characterThirdPersonGun = GetComponentInChildren<CharacterThirdPersonGun>();
            playerAnimation = GetComponent<PlayerAnimation>();
            health = GetComponent<Health>();
            actorComponent = GetComponent<ActorComponentBase>();
        }

        private void OnValidate(){
            //set character controller dimensions based on standing (uncrouched) size parameters.
            SetSize(GetComponent<CharacterController>(), standingSize);

            if(!transformComponent) transformComponent = transform;
            if(!characterController) characterController = GetComponent<CharacterController>();
            if(!characterTop) characterTop = GetComponentInChildren<CharacterTop>();
            if(!characterHead) characterHead = GetComponentInChildren<CharacterHead>();
            if(!characterCenter) characterCenter = GetComponentInChildren<CharacterCenter>();
            if(!characterThirdPersonGun)
                characterThirdPersonGun = GetComponentInChildren<CharacterThirdPersonGun>();
            if(!playerAnimation) playerAnimation = GetComponent<PlayerAnimation>();
            if(health == null) health = GetComponent<Health>();
            if(actorComponent == null) actorComponent = GetComponent<ActorComponentBase>();
        }

        private void Awake(){
            _savePosition = transformComponent.position;
        }

        private void OnEnable(){
            _savePosition = transformComponent.position;
            //Set up collision layers for the character controller
            int thisLayer = characterController.gameObject.layer;
            for(int layer = 0; layer < 32; layer++){
                Physics.IgnoreLayerCollision(thisLayer, layer,
                    ((colliderMask.value >> layer) & 1) == 0);
            }

            //We do not want the character controller to be blocked by triggers
            Physics.queriesHitTriggers = false;
        }

        private void FixedUpdate(){
            //compute movement applied directly to the transform, such as from a moving platform
            Vector3 externalMovement = transformComponent.position - _savePosition;
            if(IsGrounded){
                //Check slope of ground
                float radius = characterController.radius;
                bool groundDetected =
                    Physics.Raycast(
                        transformComponent.position + Vector3.up*radius,
                        Vector3.down, out RaycastHit hit, radius*3, colliderMask,
                        QueryTriggerInteraction.Ignore);
                if(!groundDetected || hit.normal.y <
                    Mathf.Cos(characterController.slopeLimit*Mathf.Deg2Rad)){
                    //Motion can only go downslope if too steep
                    Vector3 downSlope =
                        Vector3.ProjectOnPlane(Vector3.down*jumpSpeed, hit.normal);
                    Vector3 v = _direction*_speed + downSlope;
                    _direction = v.normalized;
                    _speed = v.magnitude;

                    IsJumping = false;
                    //can't stand on a platform that's too steep
                    if(transformComponent.parent) transformComponent.SetParent(null, true);
                }

                //Look for a moving platform
                bool triggerDetected = Physics.Raycast(
                    transformComponent.position + Vector3.up*radius,
                    Vector3.down, out hit, radius, colliderMask,
                    QueryTriggerInteraction.Collide);
                if(triggerDetected && hit.collider.isTrigger &&
                   transformComponent.parent != hit.transform &&
                   hit.collider.GetComponent<PlatformObjectCarrier>()){
                    //attach to platform
                    transformComponent.SetParent(hit.transform, true);
                }
                else if(transformComponent.parent &&
                        (!triggerDetected || transformComponent.parent != hit.transform))
                    transformComponent.SetParent(null, true);
            }

            if(IsGrounded && IsCrouching && WantsToCrouch){
                //ledge protection
                float radius = characterController.radius;
                if(!Physics.Raycast(
                    transformComponent.position + Vector3.up*radius + _direction*radius,
                    Vector3.down, out _, radius*3, colliderMask,
                    QueryTriggerInteraction.Ignore)){
                    _direction = Vector3.zero;
                    _speed = 0;
                }
            }


            if(IsJumping){
                //boost vertically while jumping
                _wasJumping = true;
                characterController.Move(externalMovement +
                                         _direction*(_speed*Time.deltaTime) +
                                         Vector3.up*(jumpSpeed*Time.deltaTime));
                if(Time.time >= _stopJumpTime){
                    //end of jump, beginning of fall
                    IsJumping = false;
                    _startFallTime = Time.time;
                }
            }
            else{
                if(IsGrounded){
                    //landed, unless slow enough to treat as a step down
                    if(_fallVelocity >= minLandSpeed || (_wasJumping && _fallVelocity > 0))
                        OnLanded(_fallVelocity, Time.time - _startFallTime);
                    _wasJumping = false;
                    _fallVelocity = 0;
                    _startFallTime = Time.time;

                    //apply grounded movement
                    characterController.Move(externalMovement +
                                             _direction*(_speed*Time.deltaTime) +
                                             Vector3.down*(stickToGroundVelocity*
                                                           Time.deltaTime));
                }
                else{
                    //add gravity
                    _fallVelocity += gravity*Time.deltaTime;
                    if(Time.time - _startFallTime > maxFallTimeAllowed)
                        health.TakeDamage(Mathf.Infinity, new DamageInfo() {
                            damageInstigator = new DamageInfo.Instigator() {
                                damageCauser = new DamageInfo.Causer() {
                                    causerType = DamageInfo.DamageCauserType.Environment
                                }
                            },
                            name = "Fall",
                            description = $"{Name} fell into an abyss",
                            type = new List<DamageInfo.WeightedDamageType>() {
                                new DamageInfo.WeightedDamageType() {
                                    type = DamageInfo.DamageType.Other,
                                    weight = 1
                                }
                            }
                        });

                    //falling movement
                    characterController.Move(externalMovement +
                                             _direction*(_speed*Time.deltaTime) +
                                             Vector3.down*(_fallVelocity*Time.deltaTime));
                }
            }

            //Update IsGrounded
            IsGrounded = characterController.isGrounded;

            //Update rotations
            float orientation = transformComponent.eulerAngles.y;
            orientation += _turnSpeed*Time.deltaTime;
            transformComponent.eulerAngles = new Vector3(0, orientation, 0);
            if(characterHead) characterHead.AddPitch(_aimSpeed*Time.deltaTime);

            //Update crouching
            if(WantsToCrouch && !IsCrouching) TryCrouch();
            if(!WantsToCrouch && IsCrouching) TryUncrouch();

            //Detect external motion applied directly to transform
            _savePosition = transformComponent.position;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit){
            //if hit above, stop jump or autocrouch
            if(hit.point.y - transformComponent.position.y >
               characterController.height - characterController.radius &&
               hit.point.y - transformComponent.position.y <
               characterController.height + characterController.skinWidth
            ){
                if(IsJumping){
                    _stopJumpTime = Time.time;
                    IsJumping = false;
                    _startFallTime = Time.time;
                    return;
                }

                if(!IsCrouching && hit.normal.y < -.1f){
                    //don't autocrouch unless it was a ceiling that was hit
                    _autoCrouchReleaseTime = Time.time + .1f;
                    TryCrouch();
                    return;
                }
            }

            //if run into physics body, push it
            if(hit.point.y - transformComponent.position.y > characterController.radius &&
               hit.point.y - transformComponent.position.y <
               characterController.height + characterController.skinWidth){
                Rigidbody body = hit.rigidbody;
                if(body == null || body.isKinematic) return;
                body.AddForceAtPosition(characterController.velocity*bumpImpulse,
                    hit.point, ForceMode.Impulse);
                return;
            }

            float radius = characterController.radius;
            int numColliders = Physics.OverlapSphereNonAlloc(
                characterCenter.transform.position, radius/2, _colliders, colliderMask,
                QueryTriggerInteraction.Ignore);
            for(int i = 0; i < numColliders; i++){
                if(_colliders[i] == characterController) continue;
                if(_colliders[i].attachedRigidbody &&
                   !_colliders[i].attachedRigidbody.isKinematic) continue;

                health.TakeDamage(Mathf.Infinity, new DamageInfo() {
                    name = "Flattened",
                    description = $"{Name} was flattened",
                    type = new List<DamageInfo.WeightedDamageType>() {
                        new DamageInfo.WeightedDamageType() {
                            type = DamageInfo.DamageType.Impact,
                            weight = 1
                        }
                    },
                    damageInstigator = new DamageInfo.Instigator() {
                        damageCauser = new DamageInfo.Causer() {
                            causerType = DamageInfo.DamageCauserType.Environment
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Move the character forward/backward/left/right
        /// </summary>
        /// <param name="motion">The motion to apply, y ignored, magnitude of 1 is max speed</param>
        public override void Move(Vector3 motion){
            _direction = transformComponent.TransformDirection(motion).normalized;
            _speed = motion.magnitude;
            if(_speed < Mathf.Epsilon){
                _direction = Vector3.zero;
                _speed = 0;
                playerAnimation.Idle(IsCrouching);
                return;
            }

            if(IsGrounded) playerAnimation.Move(motion*(IsRunning ? 2 : 1), IsCrouching);
            else playerAnimation.Idle(IsCrouching);


            if(_speed > 1) _speed = 1;
            _speed *= IsCrouching ? crouchWalkSpeed : IsRunning ? runSpeed : walkSpeed;
        }

        /// <summary>
        /// Rotate the character horizontally (yaw)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public override void Turn(float amount){
            float rate = Mathf.Abs(amount) > 1 ? Mathf.Sign(amount) : amount;
            _turnSpeed = rate*turnSpeed;
        }

        /// <summary>
        /// Aim the character's head and gun vertically (pitch)
        /// </summary>
        /// <param name="amount">Rotation amount, +/- 1=max speed</param>
        public override void Aim(float amount){
            float rate = Mathf.Abs(amount) > 1 ? Mathf.Sign(amount) : amount;
            _aimSpeed = rate*aimSpeed;
        }

        /// <summary>
        /// If character is grounded on walkable ground, unground and add vertical motion
        /// </summary>
        public override void Jump(){
            if(IsGrounded && !IsJumping && !IsCrouching && !WantsToCrouch){
                IsJumping = true;
                _stopJumpTime = Time.time + jumpTime;
            }
        }

        /// <summary>
        /// Crouch if grounded, not jumping, and not already crouched
        /// </summary>
        private void TryCrouch(){
            if(IsCrouching || IsJumping || !IsGrounded) return;
            SetSize(characterController, crouchingSize);
            IsCrouching = true;
        }

        //Uncrouch if there's headroom, not wanting to crouch, not already uncrouched, or
        //auto-crouched too recently
        private void TryUncrouch(){
            if(!IsCrouching || WantsToCrouch || Time.time < _autoCrouchReleaseTime) return;
            Vector3 position = transformComponent.position;
            Vector3 origin = position + crouchingSize.center;
            Vector3 target = position +
                             (standingSize.height + characterController.skinWidth)*
                             Vector3.up;
            float distance = target.y - origin.y - standingSize.radius;
            bool overheadDetected =
                Physics.SphereCast(
                    origin, standingSize.radius,
                    Vector3.up, out _, distance, colliderMask,
                    QueryTriggerInteraction.Ignore);
            if(overheadDetected) return;
            _uncrouchCoroutine = StartCoroutine(SetSize(standingSize, .05f));
            IsCrouching = false;
        }

        //This is called to handle landing on ground
        private void OnLanded(float fallVelocity, float fallTime){
            if(fallTime >= fallTimeMinDamage){
                float damage =
                    Mathf.InverseLerp(fallTimeMinDamage, fallTimeMaxDamage, fallTime)*
                    health.MaxAmount;
                health.TakeDamage(damage, new DamageInfo() {
                    damageInstigator = new DamageInfo.Instigator() {
                        damageCauser = new DamageInfo.Causer() {
                            causerType = DamageInfo.DamageCauserType.Ground
                        }
                    },
                    name = "Fall",
                    description = $"{Name} fell to his death",
                    type = new List<DamageInfo.WeightedDamageType>() {
                        new DamageInfo.WeightedDamageType() {
                            type = DamageInfo.DamageType.Impact,
                            weight = 1
                        }
                    }
                });
                Debug.Log(
                    $"Time={fallTime} Speed={fallVelocity} Energy={0.5f*80*fallVelocity*fallVelocity/1000}kj");
            }
        }

        /// <summary>
        ///Coroutine to change character size, for crouching and uncrouching 
        /// </summary>
        /// <param name="size">Capsule size structure</param>
        /// <param name="delay">How long to wait</param>
        /// <returns>The coroutine enumerator</returns>
        private IEnumerator SetSize(CapsuleDimensions size, float delay){
            yield return new WaitForSeconds(delay);
            SetSize(characterController, size);
        }

        /// <summary>
        /// Change character size instantly, for crouching and uncrouching
        /// </summary>
        /// <param name="cc">The character controller</param>
        /// <param name="size">The capsule size structure</param>
        private void SetSize(CharacterController cc, CapsuleDimensions size){
            if(_uncrouchCoroutine != null){
                StopCoroutine(_uncrouchCoroutine);
                _uncrouchCoroutine = null;
            }

            //Adjust character controller
            cc.radius = size.radius;
            cc.height = size.height;
            cc.center = size.center;

            //adjust subobjects to match character controller
            CharacterCenter center = characterCenter
                ? characterCenter
                : GetComponentInChildren<CharacterCenter>();
            if(center) center.transform.localPosition = size.center;
            CharacterTop top = characterTop
                ? characterTop
                : GetComponentInChildren<CharacterTop>();
            if(top) top.transform.localPosition = Vector3.up*size.height;
        }

        /// <summary>
        /// Store the capsule dimensions for the character controller
        /// </summary>
        [Serializable]
        private class CapsuleDimensions {
            [Tooltip("Radius of character capsule")]
            public float radius = .3f;

            [Tooltip("Height of character capsule")]
            public float height = 1.8f;

            [Tooltip("Relative center of character capsule")]
            public Vector3 center = new Vector3(0, .9f, 0);
        }
    }

    internal class DamageInfo {
        public object damageInstigator { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<WeightedDamageType> type { get; set; }

        public class Instigator {
            public object damageCauser { get; set; }
        }

        public class Causer {
            public object causerType { get; set; }
        }

        public class DamageCauserType {
            public static object Environment { get; set; }
            public static object Ground { get; set; }
        }

        public class WeightedDamageType {
            public object type { get; set; }
            public int weight { get; set; }
        }

        public class DamageType {
            public static object Other { get; set; }
            public static object Impact { get; set; }
        }
    }

    internal class ActorComponentBase {
        public string Name { get; set; }
    }

    internal class Health {
        public void TakeDamage(float infinity, DamageInfo damageInfo){
            throw new NotImplementedException();
        }

        public float MaxAmount { get; set; }
    }
}