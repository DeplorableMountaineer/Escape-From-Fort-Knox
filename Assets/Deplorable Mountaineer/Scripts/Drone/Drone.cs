using System;
using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library;
using Deplorable_Mountaineer.Code_Library.Mover;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Deplorable_Mountaineer.Drone {
    /// <summary>
    /// A floating AI drone that chases and attacks the player
    /// </summary>
    [RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
    public class Drone : MonoBehaviour, IObjectState {
        [Tooltip("The tag used to search for the player to target")] [SerializeField]
        private string targetTag = "Player";

        [Tooltip(
            "Subobject at the front of the mesh, Z axis pointing forward, for eyes and other" +
            "senses, and perhaps a weapon")]
        [SerializeField]
        private Transform eyes;

        [Tooltip("Sense (e.g. sight and hearing) component, attached to eyes or a subobject")]
        [SerializeField]
        private Sensing sense;

        [Tooltip("How close to target before performing an attack maneuver")] [SerializeField]
        private float attackDistance = 1;

        [Tooltip(" How far off (max) from facing target before performing an attack maneuver")]
        [SerializeField]
        private float attackHalfAngle = 10;

        [Tooltip("The target to chase")] [SerializeField]
        private Transform target;

        [Tooltip("Current state of the drone's AI")] [SerializeField]
        public DroneState state = DroneState.Guarding;

        [Tooltip(
            " Camera allowing drone to be used as a security camera, displayed on monitors " +
            "in the level")]
        [SerializeField]
        private Camera droneCam;

        [Tooltip("Seconds between frames to display; too high will slow game down")]
        [SerializeField]
        private float cameraRenderInterval = .1f;

        [Tooltip("Properties for each AI state")] [SerializeField]
        private StateProperties[] stateProperties;

        [Tooltip("Audio for drone sounds")] [SerializeField]
        private AudioSource audioSourceComponent;

        [Tooltip(
            "Home location, where drone returns when no longer chasing, in the guarding state")]
        [SerializeField, ReadOnly]
        private Vector3 home;

        [Tooltip("Direction facing when home, in the guarding state")]
        [SerializeField, ReadOnly]
        private Vector3 homeFacing;

        [SerializeField] public StateData stateData = new StateData();

        /// <summary>
        /// Waypoints allowing drone to do 3D navigation and pathfinding
        /// </summary>
        private readonly List<DroneWaypoint> _waypoints = new List<DroneWaypoint>();

        /// <summary>
        /// If true, waypoints were found in the level and will be used
        /// </summary>
        private bool _usingWaypoints;

        /// <summary>
        /// Index state properties by state for easy retrieval
        /// </summary>
        private readonly Dictionary<DroneState, StateProperties> _stateProperties =
            new Dictionary<DroneState, StateProperties>();

        /// <summary>
        /// Maintain list of update coroutines by state so they can be stopped whe
        /// transitioning out of a state
        /// </summary>
        private readonly Dictionary<DroneState, Coroutine> _stateUpdateCoroutines =
            new Dictionary<DroneState, Coroutine>();

        /// <summary>
        /// How many seconds between updates in the current state
        /// </summary>
        private float _updateRate = 1;

        /// <summary>
        /// Health component of the target, to determine if it is dead
        /// </summary>
        private Health _targetHealth;

        private Health _health;

        /// <summary>
        /// The drone's rigidbody, through which it is moved
        /// </summary>
        private Rigidbody _rigidbody;

        /// <summary>
        /// When true, drone turns to face target
        /// </summary>
        private bool _shouldFaceTarget = false;

        /// <summary>
        /// When true, drone turns to face a specified direction
        /// </summary>
        private bool _shouldFaceDirection = false;

        /// <summary>
        /// When _shouldFaceDirection is true, turn to face this direction
        /// </summary>
        private Vector3 _directionToFace = Vector3.forward;

        /// <summary>
        /// Current waypoint being pursued, or null if none
        /// </summary>
        private DroneWaypoint _currentWaypoint;

        /// <summary>
        /// How many times has the update been called since this state was entered
        /// </summary>
        private int _numStateUpdates;

        /// <summary>
        /// Coroutine for rendering droneCam image to security monitor
        /// </summary>
        private Coroutine _renderCoroutine;

        private void Reset(){
            //Give drone a unique id for saving and restoring game
#if UNITY_EDITOR
            stateData.id = GUID.Generate().ToString();
#endif
        }

        private void OnValidate(){
            Transform t = transform;
            //try to find the droneCam
            if(eyes && !droneCam) droneCam = eyes.GetComponentInChildren<Camera>();

            //Set home and home direction
            home = t.position;
            homeFacing = t.forward;
            //Find an audio source component
            if(!audioSourceComponent) audioSourceComponent = GetComponent<AudioSource>();

            //Get a sensing component
            if(eyes && !sense) sense = eyes.GetComponentInChildren<Sensing>();

            //Give drone a unique id for saving and restoring game
#if UNITY_EDITOR
            if(string.IsNullOrWhiteSpace(stateData.id))
                stateData.id = GUID.Generate().ToString();
#endif

            //Try to automatically determine the target to chase
            if(target || string.IsNullOrWhiteSpace(targetTag)) return;
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if(go) target = go.transform;
        }

        private void Awake(){
            //reference the rigid body component
            _rigidbody = GetComponent<Rigidbody>();

            _health = GetComponent<Health>();

            //reference the target's health component
            if(target) _targetHealth = target.GetComponent<Health>();

            //Warn if drone has no eyes
            if(!eyes) Debug.LogWarning("Inspector property \"eyes\" hasn't been set");

            //Store list of waypoints in this level
            foreach(DroneWaypoint wp in FindObjectsOfType<DroneWaypoint>()){
                _waypoints.Add(wp);
            }

            //Determine if there are any waypoints in this level
            _usingWaypoints = _waypoints.Count > 0;

            //Populate state properties dictionary
            foreach(StateProperties afs in stateProperties){
                _stateProperties[afs.state] = afs;
            }
        }

        private void OnEnable(){
            //resume previous state on re-enabling, or start the starting state
            EnterState(state);

            //set update rate based on state properties, defaulting to once per second if
            //no properties available
            if(_stateProperties.ContainsKey(state))
                _updateRate = _stateProperties[state].deltaTime;
            else _updateRate = 1;

            //Begin updating the state
            StartUpdating(state, Rng.Random.NextFloat(0, _updateRate));

            //Begin rendering the drone cam
            _renderCoroutine = StartCoroutine(RenderCoroutine());
        }

        private void OnDisable(){
            //Stop rendering the drone cam
            if(_renderCoroutine != null) StopCoroutine(_renderCoroutine);
            //put brakes on updating in case of failure to stop coroutine
            _updateRate = 1000;
            //stop updating state and then exit state, and set drone state to disabled
            ExitState(state, DroneState.Disabled);
        }

        private void FixedUpdate(){
            if(_health.Amount < Mathf.Epsilon) return;
            //turn toward target if requested
            if(_shouldFaceTarget) FaceTarget(Time.deltaTime);

            //turn toward requested direction, if any
            if(_shouldFaceDirection) FaceDirection(_directionToFace, Time.deltaTime);

            //try to keep drone right-side up
            Transform t = transform;
            Vector3 forward = t.forward;
        }

        /// <summary>
        /// Begin updating the current drone state
        /// </summary>
        /// <param name="droneState">AI state to update</param>
        /// <param name="delay">how long to wait before first update, to prevent too
        /// many updates happening on the same frame</param>
        private void StartUpdating(DroneState droneState, float delay){
            StartCoroutine(StartUpdatingCoroutine(droneState, delay));
        }

        /// <summary>
        /// After specified delay, start the coroutine to update drone state;
        /// Use the StartUpdating() routine instead of calling this directly.
        /// </summary>
        /// <param name="droneState">AI state to update</param>
        /// <param name="delay">ow long to wait before first update, to prevent too
        /// many updates happening on the same frame</param>
        /// <returns>The coroutine</returns>
        /// <exception cref="ArgumentOutOfRangeException">if state is invalid</exception>
        private IEnumerator StartUpdatingCoroutine(DroneState droneState, float delay){
            yield return new WaitForSeconds(delay);
            //Debug.Log($"{this} updating {droneState}, deltaTime = {_updateRate}");

            //Select correct state, start a coroutine to update it, and store the
            //coroutine so it can be stopped on state exit
            switch(droneState){
                case DroneState.Guarding:
                    _stateUpdateCoroutines[droneState] = StartCoroutine(UpdateGuardingState());
                    break;
                case DroneState.Wandering:
                    break;
                case DroneState.Seeking:
                    _stateUpdateCoroutines[droneState] = StartCoroutine(UpdateSeekingState());
                    break;
                case DroneState.Fleeing:
                    break;
                case DroneState.Hiding:
                    break;
                case DroneState.Chasing:
                    _stateUpdateCoroutines[droneState] = StartCoroutine(UpdateChasingState());
                    break;
                case DroneState.Attacking:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateAttackingState());
                    break;
                case DroneState.Exploding:
                    break;
                case DroneState.GoingHome:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateGoingHomeState());
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateReacquiringState());
                    break;
                case DroneState.FollowingWaypointsHome:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateFollowingWaypointsHomeState());
                    break;
                case DroneState.DirectlyHome:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateDirectlyHomeState());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(droneState), droneState,
                        null);
            }
        }

        /// <summary>
        /// Stop updating the specified AI state; stop the coroutine that updates it
        /// </summary>
        /// <param name="droneState">The AI state to stop</param>
        private void StopUpdating(DroneState droneState){
            //Debug.Log($"{this} no longer updating {droneState}");
            if(_stateUpdateCoroutines.ContainsKey(droneState))
                if(_stateUpdateCoroutines[droneState] != null)
                    StopCoroutine(_stateUpdateCoroutines[droneState]);
            _stateUpdateCoroutines.Remove(droneState);
        }

        /// <summary>
        /// Transition to another state; this stops updating the current
        /// state and exits it, then enters the new state and starts
        /// updating it
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        public void TransitionState(DroneState newState){
            if(!enabled) return;
            //put brakes on state updates
            _updateRate = 1000;
            //Stop the state update coroutine
            StopUpdating(state);
            //stop updating state and then exit the old state
            ExitState(state, newState);
            //enter a new state
            state = newState;
            EnterState(state);
            //set update rate for new state
            if(_stateProperties.ContainsKey(state))
                _updateRate = _stateProperties[state].deltaTime;
            else _updateRate = 1;
            //start updating new state
            //random delay to keep too much from happening at the same time
            StartUpdating(state, Rng.Random.NextFloat(0, _updateRate));
        }

        /// <summary>
        /// Enter a new state by calling its enter routine
        /// </summary>
        /// <param name="s">The state to enter</param>
        /// <exception cref="ArgumentOutOfRangeException">if the state is invalid</exception>
        private void EnterState(DroneState s){
            //Debug.Log($"{this} entering {s}");

            switch(s){
                case DroneState.Guarding:
                    EnterGuardingState();
                    break;
                case DroneState.Wandering:
                    break;
                case DroneState.Seeking:
                    EnterSeekingState();
                    break;
                case DroneState.Fleeing:
                    break;
                case DroneState.Hiding:
                    break;
                case DroneState.Chasing:
                    EnterChasingState();
                    break;
                case DroneState.Attacking:
                    EnterAttackingState();
                    break;
                case DroneState.Exploding:
                    GetComponent<Health>().Amount = 0;
                    break;
                case DroneState.GoingHome:
                    EnterGoingHomeState();
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    EnterReacquiringState();
                    break;
                case DroneState.FollowingWaypointsHome:
                    EnterFollowingWaypointsHomeState();
                    break;
                case DroneState.DirectlyHome:
                    EnterDirectlyHomeState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(s), s, null);
            }
        }

        /// <summary>
        /// Exit the specified running state, and signal what state is intended to be next
        /// </summary>
        /// <param name="s">The state to stop</param>
        /// <param name="next">The next state</param>
        /// <exception cref="ArgumentOutOfRangeException">if the state is invalid</exception>
        private void ExitState(DroneState s, DroneState next){
            //Debug.Log($"{this} exiting {s} to transition to {next}");
            switch(s){
                case DroneState.Guarding:
                    ExitGuardingState(next);
                    break;
                case DroneState.Wandering:
                    break;
                case DroneState.Seeking:
                    ExitSeekingState(next);
                    break;
                case DroneState.Fleeing:
                    break;
                case DroneState.Hiding:
                    break;
                case DroneState.Chasing:
                    ExitChasingState(next);
                    break;
                case DroneState.Attacking:
                    ExitAttackingState(next);
                    break;
                case DroneState.Exploding:
                    break;
                case DroneState.GoingHome:
                    ExitGoingHomeState(next);
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    ExitReacquiringState(next);
                    break;
                case DroneState.FollowingWaypointsHome:
                    ExitFollowingWaypointsHomeState(next);
                    break;
                case DroneState.DirectlyHome:
                    ExitDirectlyHomeState(next);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(s), s, null);
            }
        }

        /// <summary>
        /// Enter the state to stand guard at a location facing a specific direction
        /// </summary>
        private void EnterGuardingState(){
            if(_stateProperties.ContainsKey(DroneState.Guarding)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.Guarding]);

                //set velocity for state (zero for guarding)
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Guarding].velocity;
            }

            //no target being chased
            _shouldFaceTarget = false;

            //face requested home direction
            _shouldFaceDirection = true;
            _directionToFace = homeFacing;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateGuardingState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //look for target if it is alive
                if(TargetIsAlive()){
                    if(sense && sense.CanSenseTarget()){
                        //target sensed; next state will turn to face and see if drone can
                        //locate it by sight
                        TransitionState(DroneState.Reacquiring);
                        break;
                    }
                }

                //keep facing home
                _shouldFaceDirection = true;
                _directionToFace = homeFacing;

                //maintain (zero) velocity
                if(_stateProperties.ContainsKey(DroneState.Guarding)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Guarding].velocity;
                }

                //wait till time to update again
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitGuardingState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            StopUpdating(state);
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
        }

        private void EnterReacquiringState(){
            if(_stateProperties.ContainsKey(DroneState.Reacquiring)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.Reacquiring]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Reacquiring].velocity;
            }

            //face target if it is alive
            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateReacquiringState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //go home if target dead
                if(!TargetIsAlive()){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //chase if target spotted and locatable
                if(sense && sense.CanLocateTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }

                //if turned toward target direction but not spotted, start seeking
                if(FacingTarget()){
                    TransitionState(DroneState.Seeking);
                    break;
                }

                //maintain velocity
                if(_stateProperties.ContainsKey(DroneState.Reacquiring)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Reacquiring].velocity;
                }

                //wait till time to update again
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitReacquiringState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterChasingState(){
            if(_stateProperties.ContainsKey(DroneState.Chasing)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.Chasing]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Chasing].velocity;
            }

            //face target if it is alive
            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateChasingState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //go home if target dead
                if(!TargetIsAlive()){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //if target lost, try to turn to see it
                if(sense && !sense.CanLocateTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //when close enough, attack
                if(CanAttackTarget()){
                    TransitionState(DroneState.Attacking);
                    break;
                }

                //maintain chase velocity
                if(_stateProperties.ContainsKey(DroneState.Chasing)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Chasing].velocity;
                }

                //wait till time to update again
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitChasingState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterSeekingState(){
            if(_stateProperties.ContainsKey(DroneState.Seeking)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.Seeking]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Seeking].velocity;
            }

            //can't find target to face, yet
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateSeekingState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //go home if target dead
                if(!TargetIsAlive()){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //if target spotted, chase it
                if(sense && sense.CanLocateTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }

                //maintain seek velocity
                if(_stateProperties.ContainsKey(DroneState.Seeking)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Seeking].velocity;
                }

                //count the updates
                _numStateUpdates++;

                //after 10 seconds of seeking, give up and go home
                if(_numStateUpdates > 10/_updateRate){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //if target detected but not spotted, try turning that way to look for it
                Vector3 pos = transform.position;
                if(sense && sense.CanSenseTarget() &&
                   CanSeeLocation(pos, target.position)){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //randomly turn, and check for collisions
                for(int i = 0; i < 50; i++){
                    _shouldFaceDirection = true;
                    _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 56f/(51f - i));
                    if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                }


                //wait till time ot update again
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitSeekingState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterAttackingState(){
            if(_stateProperties.ContainsKey(DroneState.Attacking)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.Attacking]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Attacking].velocity;
            }

            //face target if it is alive
            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateAttackingState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //go home if target dead
                if(!TargetIsAlive()){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //if target slips out of range, resume chasing
                if(!CanAttackTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }

                //if target lost, try to reacquire by turning
                if(sense && !sense.CanLocateTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //maintain attack velocity
                if(_stateProperties.ContainsKey(DroneState.Attacking)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Attacking].velocity;
                }

                Vector3 position = transform.position;
                Vector3 targetPos = target.position;
                Vector3 offset = targetPos - position;
                float distance = offset.magnitude;
                if(distance < attackDistance/2){
                    TransitionState(DroneState.Exploding);
                }

                //count the updates
                _numStateUpdates++;


                //wait till time for next update
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitAttackingState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterGoingHomeState(){
            if(_stateProperties.ContainsKey(DroneState.GoingHome)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.GoingHome]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.GoingHome].velocity;
            }

            //no target being chased
            _shouldFaceTarget = false;

            //face forward; update will turn as it seeks a path home
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateGoingHomeState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //maintain going home velocity
                if(_stateProperties.ContainsKey(DroneState.GoingHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.GoingHome].velocity;
                }

                //count the updates
                _numStateUpdates++;

                //find a waypoint
                _currentWaypoint = FindWaypointNear(transform.position);
                if(_currentWaypoint){
                    //Debug.Log($"First waypoint: {_currentWaypoint}");
                    //if found, start following waypoint path
                    TransitionState(DroneState.FollowingWaypointsHome);
                    break;
                }

                //wander until waypoint found
                Vector3 pos = transform.position;
                //randomly turn, turning wider if collisions impending
                for(int i = 0; i < 50; i++){
                    _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 56f/(51f - i));
                    if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                }


                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitGoingHomeState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterFollowingWaypointsHomeState(){
            if(_stateProperties.ContainsKey(DroneState.FollowingWaypointsHome)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.FollowingWaypointsHome]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.FollowingWaypointsHome]
                        .velocity;
            }

            //no target being chased
            _shouldFaceTarget = false;

            //face forward; update will turn as it seeks a path home
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateFollowingWaypointsHomeState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                //if target sensed, try to acquire it by rotating toward sensed direction
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //maintain path following velocity 
                if(_stateProperties.ContainsKey(DroneState.FollowingWaypointsHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.FollowingWaypointsHome]
                            .velocity;
                }

                //count the state updates
                _numStateUpdates++;

                //if no waypoint, go back to seeking a path home
                if(!_currentWaypoint){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                //if arrived at waypoint
                Vector3 pos = transform.position;
                if(IsAtWaypoint(pos, _currentWaypoint.Position)){
                    //if home spotted, go straight there
                    if(CanSeeLocation(pos, home)){
                        TransitionState(DroneState.DirectlyHome);
                    }

                    //get next waypoint on path to home
                    _currentWaypoint = FindNextWaypointOnPath(transform.position, home);
                    //Debug.Log($"Next waypoint: {_currentWaypoint}");

                    //if none found, seek path home
                    if(!_currentWaypoint){
                        TransitionState(DroneState.GoingHome);
                        break;
                    }
                }

                //move toward next waypoint on path
                _directionToFace = (_currentWaypoint.Position - pos).normalized;

                //if way blocked, rotate wider and wider arcs to try to reacquire
                if(!CanSeeLocation(pos, _currentWaypoint.Position))
                    for(int i = 0; i < 50; i++){
                        if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                        _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 5);
                    }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitFollowingWaypointsHomeState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterDirectlyHomeState(){
            if(_stateProperties.ContainsKey(DroneState.DirectlyHome)){
                //set audio for state
                SetAudio(_stateProperties[DroneState.DirectlyHome]);
                //set velocity for state 
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.DirectlyHome]
                        .velocity;
            }

            //no target being chased
            _shouldFaceTarget = false;

            //face forward; update will turn to face home
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;

            //count the state updates
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateDirectlyHomeState(){
            //update as long as enabled
            while(enabled && _updateRate > 0){
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                //maintain go directly home velocity
                if(_stateProperties.ContainsKey(DroneState.DirectlyHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.DirectlyHome].velocity;
                }

                //if home, resume guarding
                Vector3 pos = transform.position;
                if(IsAtWaypoint(pos, home)){
                    TransitionState(DroneState.Guarding);
                    break;
                }

                //count the updates
                _numStateUpdates++;

                //face home
                _directionToFace = (home - pos).normalized;

                //if can't see clear way home, rotate until can see
                if(!CanSeeLocation(pos, home)){
                    for(int i = 0; i < 10; i++){
                        if(CanSeeLocation(pos, pos + _directionToFace*2)) break;
                        _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 1);
                    }

                    //after 20 seconds of blocked path, go back to seeking a path home
                    if(_numStateUpdates > 20/_updateRate && !CanSeeLocation(pos, home)){
                        TransitionState(DroneState.GoingHome);
                        break;
                    }
                }

                //wait till time for next update
                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitDirectlyHomeState(DroneState nextState){
            //stop audio and stop updating state; clear
            //the face flags so next state has a clean start
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        /// <summary>
        /// Stop drone audio
        /// </summary>
        private void StopAudio(){
            if(audioSourceComponent) audioSourceComponent.Stop();
        }

        /// <summary>
        /// Return true if facing target
        /// </summary>
        /// <returns></returns>
        private bool FacingTarget(){
            Vector3 position = transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            return Vector3.Angle(direction, transform.forward) <= attackHalfAngle;
        }

        /// <summary>
        /// Apply one delta time's worth of turning to face target
        /// </summary>
        /// <param name="deltaTime">update rate</param>
        private void FaceTarget(float deltaTime){
            Vector3 position = transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return;
            Vector3 direction = offset/distance;
            FaceDirection(direction, deltaTime);
        }

        /// <summary>
        /// Apply one delta time's worth of turning to face desired direction
        /// </summary>
        /// <param name="direction">Direction to face</param>
        /// <param name="deltaTime">update rate</param>
        private void FaceDirection(Vector3 direction, float deltaTime){
            Vector3 current = transform.forward;

            //if not moving almost vertically, try to rotate right-side up
            if(Mathf.Abs(Vector3.Dot(current, Vector3.up)) < .7f){
                transform.up = Vector3.MoveTowards(transform.up, Vector3.up,
                    deltaTime);
            }

            transform.forward = Vector3.MoveTowards(current, direction,
                deltaTime*2);
        }

        /// <summary>
        /// set drone audio, with volume and pitch, and play it
        /// </summary>
        /// <param name="afs"></param>
        private void SetAudio(StateProperties afs){
            if(!audioSourceComponent) return;
            audioSourceComponent.clip = afs.audio;
            audioSourceComponent.volume = afs.volume;
            audioSourceComponent.pitch = afs.pitchMultiplier;
            audioSourceComponent.loop = true;
            audioSourceComponent.Play();
        }

        /// <summary>
        /// Return true if we have a living target to chase
        /// </summary>
        /// <returns></returns>
        private bool TargetIsAlive(){
            if(!target){
                _targetHealth = null;
                return false;
            }

            if(!_targetHealth) _targetHealth = target.GetComponent<Health>();
            if(!_targetHealth) return false;
            return _targetHealth.Amount > 0;
        }


        /// <summary>
        /// Return true if in range and facing target and thus able to attack
        /// </summary>
        /// <returns></returns>
        private bool CanAttackTarget(){
            Vector3 position = transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance > attackDistance) return false;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            return Vector3.Angle(direction, transform.forward) <= attackHalfAngle;
        }

        /// <summary>
        /// Find next waypoint on path from start to end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private DroneWaypoint FindNextWaypointOnPath(Vector3 start, Vector3 end){
            //get a waypoint at the end
            DroneWaypoint lastWaypoint = FindWaypointNear(end);

            //if can go there directly, just do so
            if(CanSeeLocation(start, lastWaypoint.Position)) return lastWaypoint;

            //get a waypoint at the start
            DroneWaypoint firstWaypoint = FindWaypointNear(start);

            //if it is the same as the last waypoint, return it
            if(firstWaypoint == lastWaypoint) return firstWaypoint;

            //if not already there, return first waypoint
            if(!IsAtWaypoint(start, firstWaypoint.Position)) return firstWaypoint;

            //At the waypoint, so make a path from there and return the next waypoint
            //on that path
            return MakeWaypointPath(firstWaypoint, lastWaypoint);
        }

        /// <summary>
        /// Search for a waypoint path connecting two waypoints
        /// </summary>
        /// <param name="firstWaypoint"></param>
        /// <param name="lastWaypoint"></param>
        /// <returns></returns>
        private DroneWaypoint MakeWaypointPath(DroneWaypoint firstWaypoint,
            DroneWaypoint lastWaypoint){
            //initialize all as infinite distance from goal
            foreach(DroneWaypoint wp in _waypoints){
                wp.PathfindingDistanceToGoal = Mathf.Infinity;
            }

            //last waypoint is the goal, so make it 0 distance
            lastWaypoint.PathfindingDistanceToGoal = 0;

            //Keep a queue of neighbors found but not expanded
            Queue<DroneWaypoint> openSet = new Queue<DroneWaypoint>();

            //add the last waypoint (with known distance) to the queue
            openSet.Enqueue(lastWaypoint);

            //repeat until no more in queue
            while(openSet.Count > 0){
                DroneWaypoint w = openSet.Dequeue();
                //pop and expand a waypoint from the queue;
                //expanding may add waypoints back to the queue,
                //so repeat until this no longer happens
                PathfindingUpdateNeighbors(w, openSet);
            }

            //now, go through waypoints adjacent to first, and
            //find the neighbor that takes us closest to the goal 
            //(in terms of number of waypoints on the path)
            DroneWaypoint next = null;
            float bestScore = Mathf.Infinity;
            foreach(DroneWaypoint wp in firstWaypoint.neighbors){
                if(wp.PathfindingDistanceToGoal < bestScore){
                    bestScore = wp.PathfindingDistanceToGoal;
                    next = wp;
                }
            }

            return next;
        }

        /// <summary>
        /// Expand a waypoint, querying its neighbors,
        /// putting those on the queue that need further expansion
        /// (breadth-first search, or A* with a trivial heuristic)
        /// </summary>
        /// <param name="wp"></param>
        /// <param name="openSet"></param>
        private void PathfindingUpdateNeighbors(DroneWaypoint wp,
            Queue<DroneWaypoint> openSet){
            //anything that is expanded must be within this distance to goal
            float l = wp.PathfindingDistanceToGoal + 1;
            foreach(DroneWaypoint n in wp.neighbors){
                //unless already connected by a shorter path, set to this
                //distance and enqueue it for further expansion
                if(n.PathfindingDistanceToGoal > l){
                    n.PathfindingDistanceToGoal = l;
                    if(!openSet.Contains(n)) openSet.Enqueue(n);
                }
            }
        }

        //return true if one location is visible from the other
        private bool CanSeeLocation(Vector3 location, Vector3 waypoint){
            Vector3 offset = waypoint - location;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            bool blocked = Physics.SphereCast(location, .5f, direction, out RaycastHit hit,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return !blocked || hit.collider.transform.root == transform;
        }

        //return true if one location is close to the other
        private bool IsAtWaypoint(Vector3 location, Vector3 waypoint){
            return Vector3.Distance(location, waypoint) < .5f;
        }

        //find a waypoint close to the location, optionally one that
        //is also visible from the location as well
        private DroneWaypoint FindWaypointNear(Vector3 location,
            bool checkStraightLine = true){
            DroneWaypoint best = null;
            DroneWaypoint bestStraightLine = null;
            float bestDistance = Mathf.Infinity;
            float bestStraightLineDistance = Mathf.Infinity;
            foreach(DroneWaypoint wp in _waypoints){
                float distance = Vector3.Distance(location, wp.Position);
                if(distance < bestDistance){
                    best = wp;
                    bestDistance = distance;
                }

                if(!checkStraightLine || distance >= bestStraightLineDistance) continue;
                if(distance < Mathf.Epsilon){
                    return wp;
                }

                Vector3 direction = (wp.Position - location)/distance;
                bool blocked = Physics.SphereCast(location, .5f, direction, out RaycastHit hit,
                    distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if(blocked && hit.collider.transform.root != transform) continue;
                bestStraightLine = wp;
                bestStraightLineDistance = distance;
            }

            if(checkStraightLine && !float.IsInfinity(bestStraightLineDistance))
                return bestStraightLine;
            return best;
        }


        //coroutine to render frames to security monitor
        private IEnumerator RenderCoroutine(){
            if(!droneCam) yield break;
            yield return new WaitForSeconds(Rng.Random.NextFloat(0, cameraRenderInterval));
            while(enabled){
                yield return new WaitForSeconds(cameraRenderInterval);
                droneCam.Render();
            }
        }

        public void GetState(){
            stateData.velocity = _rigidbody.velocity;
            stateData.position = _rigidbody.position;
            stateData.eulerAngles = _rigidbody.rotation.eulerAngles;
            stateData.state = state;
            stateData.health = _health.Amount;
        }

        public void SetState(){
            _health.SetHealthSilently(stateData.health);
            _rigidbody.velocity = stateData.velocity;
            _rigidbody.position = stateData.position;
            transform.eulerAngles = stateData.eulerAngles;
            TransitionState(stateData.state);
        }

        /// <summary>
        /// Properties for AI state
        /// </summary>
        [Serializable]
        public class StateProperties {
            public DroneState state;
            public AudioClip audio;
            public float pitchMultiplier = 1;
            public float volume = .5f;
            public float deltaTime;
            public float velocity;
        }

        /// <summary>
        /// Available drone AI states
        /// </summary>
        public enum DroneState {
            Guarding,
            Wandering,
            Seeking,
            Fleeing,
            Hiding,
            Chasing,
            Attacking,
            Exploding,
            GoingHome,
            Disabled,
            Dead,
            Reacquiring,
            FollowingWaypointsHome,
            DirectlyHome,
        }

        /// <summary>
        /// for saving/loading drone; different from AI states
        /// </summary>
        [Serializable]
        public class StateData {
            public Vector3 velocity;
            public Vector3 position;
            public Vector3 eulerAngles;
            public DroneState state;
            public float health;
            public string id;
        }
    }
}