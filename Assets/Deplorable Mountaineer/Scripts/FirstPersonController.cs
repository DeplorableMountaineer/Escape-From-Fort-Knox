using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;

#pragma warning disable 618, 649
namespace Deplorable_Mountaineer {
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour {
        [SerializeField] private bool isWalking;
        [SerializeField] private float walkSpeed = 5;
        [SerializeField] private float runSpeed = 10;
        [SerializeField] [Range(0f, 1f)] private float runstepLenghten = .7f;
        [SerializeField] private float jumpSpeed = 10;
        [SerializeField] private float stickToGroundForce = 10;

        [SerializeField] private float gravityMultiplier = 2;

        //TDV made mouseLook public
        [SerializeField] public MouseLook mouseLook;
        [SerializeField] private bool useFovKick = true;
        [SerializeField] private FOVKick fovKick = new FOVKick();
        [SerializeField] private bool useHeadBob = true;
        [SerializeField] private CurveControlledBob headBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob jumpBob = new LerpControlledBob();
        [SerializeField] private float stepInterval = 5;

        [SerializeField] private AudioClip[]
            footstepSounds; // an array of footstep sounds that will be randomly selected from.

        [SerializeField]
        private AudioClip jumpSound; // the sound played when character leaves the ground.

        [SerializeField] private AudioClip
            landSound; // the sound played when character touches back on ground.

        private Camera _camera;
        private bool _jump;
        private Vector2 _input;
        private Vector3 _moveDir = Vector3.zero;
        private CharacterController _characterController;
        private CollisionFlags _collisionFlags;
        private bool _previouslyGrounded;
        private Vector3 _originalCameraPosition;
        private float _stepCycle;
        private float _nextStep;
        private bool _jumping;
        private AudioSource _audioSource;
        private Transform _transform;

        //TDV added this
        private Vector3 _addMotion = Vector3.zero;

        // Use this for initialization
        private void Start(){
            _transform = transform;
            _characterController = GetComponent<CharacterController>();
            _camera = Camera.main;
            Debug.Assert(_camera != null,
                nameof(_camera) + " != null required for this script");
            _originalCameraPosition = _camera.transform.localPosition;
            fovKick.Setup(_camera);
            headBob.Setup(_camera, stepInterval);
            _stepCycle = 0f;
            _nextStep = _stepCycle/2f;
            _jumping = false;
            _audioSource = GetComponent<AudioSource>();
            mouseLook.Init(transform, _camera.transform);
        }


        // Update is called once per frame
        private void Update(){
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if(!_jump){
                _jump = CrossPlatformInputManager.GetButtonDown("Jump") && !_jumping;
                //don't allow jump when already jumping; without the && !_jumping,
                //releasing and re-pressing jump while character is falling
                //will cause a jump on landing
            }

            //Character just landed
            if(!_previouslyGrounded && _characterController.isGrounded){
                StartCoroutine(jumpBob.DoBobCycle());
                PlayLandingSound();
                _moveDir.y = 0f;
                _jumping = false;
            }

            //walked off ledge
            if(!_characterController.isGrounded && !_jumping && _previouslyGrounded){
                _moveDir.y = 0f;
            }

            _previouslyGrounded = _characterController.isGrounded;
        }


        private void PlayLandingSound(){
            _audioSource.clip = landSound;
            _audioSource.Play();
            _nextStep = _stepCycle + .5f;
        }


        private void FixedUpdate(){
            GetInput(out float speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = _transform.forward*_input.y + _transform.right*_input.x;

            // get a normal for the surface that is being touched to move along it
            Physics.SphereCast(_transform.position, _characterController.radius,
                Vector3.down, out RaycastHit hitInfo,
                _characterController.height/2f, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            _moveDir.x = desiredMove.x*speed;
            _moveDir.z = desiredMove.z*speed;


            if(_characterController.isGrounded){
                _moveDir.y = -stickToGroundForce;

                if(_jump){
                    _moveDir.y = jumpSpeed;
                    PlayJumpSound();
                    _jump = false;
                    _jumping = true;
                }
            }
            else{
                _moveDir += Physics.gravity*(gravityMultiplier*Time.fixedDeltaTime);
            }

            //TDV added "addMotion" part
            _collisionFlags =
                _characterController.Move(_moveDir*Time.fixedDeltaTime +
                                          _addMotion*Time.fixedDeltaTime);

            ProcessStepCycle(speed);
            UpdateCameraPosition(speed);

            mouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound(){
            _audioSource.clip = jumpSound;
            _audioSource.Play();
        }


        private void ProcessStepCycle(float speed){
            if(_characterController.velocity.sqrMagnitude > Mathf.Epsilon &&
               (_input.x != 0 || _input.y != 0)){
                _stepCycle += (_characterController.velocity.magnitude +
                               (speed*(isWalking ? 1f : runstepLenghten)))*
                              Time.fixedDeltaTime;
            }

            if(_stepCycle <= _nextStep) return;

            _nextStep = _stepCycle + stepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio(){
            if(!_characterController.isGrounded){
                return;
            }

            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, footstepSounds.Length);
            _audioSource.clip = footstepSounds[n];
            _audioSource.PlayOneShot(_audioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            footstepSounds[n] = footstepSounds[0];
            footstepSounds[0] = _audioSource.clip;
        }


        /// <summary>
        /// Apply head bob to camera
        /// </summary>
        /// <param name="speed"></param>
        private void UpdateCameraPosition(float speed){
            Vector3 newCameraPosition;
            if(!useHeadBob){
                return;
            }

            if(_characterController.velocity.magnitude > 0 &&
               _characterController.isGrounded){
                Vector3 localPosition = headBob.DoHeadBob(
                    _characterController.velocity.magnitude +
                    (speed*(isWalking ? 1f : runstepLenghten)));
                _camera.transform.localPosition = localPosition;
                newCameraPosition = localPosition;
                newCameraPosition.y = localPosition.y - jumpBob.Offset();
            }
            else{
                newCameraPosition = _camera.transform.localPosition;
                newCameraPosition.y = _originalCameraPosition.y - jumpBob.Offset();
            }

            _camera.transform.localPosition = newCameraPosition;
        }

        private void GetInput(out float speed){
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = isWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            isWalking = !Input.GetButton("Run");
#endif
            // set the desired speed to be walking or running
            speed = isWalking ? walkSpeed : runSpeed;
            _input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if(_input.sqrMagnitude > 1){
                _input.Normalize();
            }
            else speed *= _input.magnitude;

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if(isWalking != waswalking && useFovKick &&
               _characterController.velocity.sqrMagnitude > 0){
                StopAllCoroutines();
                StartCoroutine(!isWalking ? fovKick.FOVKickUp() : fovKick.FOVKickDown());
            }
        }


        private void RotateView(){
            mouseLook.LookRotation(transform, _camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit){
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if(_collisionFlags == CollisionFlags.Below){
                return;
            }

            if(body == null || body.isKinematic){
                return;
            }

            body.AddForceAtPosition(_characterController.velocity*0.1f, hit.point,
                ForceMode.Impulse);
        }

        //TDV added this
        public void SpeedBoost(){
            _addMotion = Vector3.forward*10;
        }
    }
}