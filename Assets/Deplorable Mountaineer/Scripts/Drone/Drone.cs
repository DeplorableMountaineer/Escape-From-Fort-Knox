using System;
using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.Code_Library;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deplorable_Mountaineer.Drone {
    [RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
    public class Drone : MonoBehaviour {
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private Transform eyes;
        [SerializeField] private Sensing sense;
        [SerializeField] private float attackDistance = 1;
        [SerializeField] private float attackHalfAngle = 10;
        [SerializeField] private Transform target;
        [SerializeField] private DroneState state = DroneState.Guarding;
        [SerializeField] private Camera droneCam;
        [SerializeField] private float cameraRenderInterval = .1f;

        [FormerlySerializedAs("audioForStates")] [SerializeField]
        private StateProperties[] stateProperties;

        [SerializeField] private AudioSource audioSourceComponent;
        [SerializeField, ReadOnly] private Vector3 home;
        [SerializeField, ReadOnly] private Vector3 homeFacing;

        private readonly List<DroneWaypoint> _waypoints = new List<DroneWaypoint>();
        private bool _usingWaypoints;

        private readonly Dictionary<DroneState, StateProperties> _stateProperties =
            new Dictionary<DroneState, StateProperties>();

        private readonly Dictionary<DroneState, Coroutine> _stateUpdateCoroutines =
            new Dictionary<DroneState, Coroutine>();

        private float _updateRate = 1;
        private Health _targetHealth;
        private Rigidbody _rigidbody;

        private bool _shouldFaceTarget = false;
        private bool _shouldFaceDirection = false;
        private Vector3 _directionToFace = Vector3.forward;
        private DroneWaypoint _currentWaypoint;
        private int _numStateUpdates;
        private Coroutine _renderCoroutine;

        private void OnValidate(){
            Transform t = transform;
            if(eyes && !droneCam) droneCam = eyes.GetComponentInChildren<Camera>();
            home = t.position;
            homeFacing = t.forward;
            if(!audioSourceComponent) audioSourceComponent = GetComponent<AudioSource>();
            if(eyes && !sense) sense = eyes.GetComponentInChildren<Sensing>();
            if(target || string.IsNullOrWhiteSpace(targetTag)) return;
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if(go) target = go.transform;
        }

        private void Awake(){
            _rigidbody = GetComponent<Rigidbody>();
            if(target) _targetHealth = target.GetComponent<Health>();
            if(!eyes) Debug.LogWarning("Inspector property \"eyes\" hasn't been set");
            foreach(DroneWaypoint wp in FindObjectsOfType<DroneWaypoint>()){
                _waypoints.Add(wp);
            }

            _usingWaypoints = _waypoints.Count > 0;
            foreach(StateProperties afs in stateProperties){
                _stateProperties[afs.state] = afs;
            }
        }

        private void OnEnable(){
            EnterState(state);
            if(_stateProperties.ContainsKey(state))
                _updateRate = _stateProperties[state].deltaTime;
            else _updateRate = 1;
            StartUpdating(state, Rng.Random.NextFloat(0, _updateRate));
            _renderCoroutine = StartCoroutine(RenderCoroutine());
        }

        private void OnDisable(){
            if(_renderCoroutine != null) StopCoroutine(_renderCoroutine);
            _updateRate = 1000;
            StopUpdating(state);
            ExitState(state, DroneState.Disabled);
        }

        private void FixedUpdate(){
            if(_shouldFaceTarget) FaceTarget(Time.deltaTime);
            if(_shouldFaceDirection) FaceDirection(_directionToFace, Time.deltaTime);

            //try to keep drone right-side up
            Transform t = transform;
            Vector3 forward = t.forward;
        }

        private void StartUpdating(DroneState droneState, float delay){
            StartCoroutine(StartUpdatingCoroutine(droneState, delay));
        }

        private IEnumerator StartUpdatingCoroutine(DroneState droneState, float delay){
            yield return new WaitForSeconds(delay);
            Debug.Log($"{this} updating {droneState}, deltaTime = {_updateRate}");
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

        private void StopUpdating(DroneState droneState){
            Debug.Log($"{this} no longer updating {droneState}");
            if(_stateUpdateCoroutines.ContainsKey(droneState))
                if(_stateUpdateCoroutines[droneState] != null)
                    StopCoroutine(_stateUpdateCoroutines[droneState]);
            _stateUpdateCoroutines.Remove(droneState);
        }

        public void TransitionState(DroneState newState){
            if(!enabled) return;
            _updateRate = 0;
            ExitState(state, newState);
            state = newState;
            EnterState(state);
            if(_stateProperties.ContainsKey(state))
                _updateRate = _stateProperties[state].deltaTime;
            else _updateRate = 1;
            StartUpdating(state, Rng.Random.NextFloat(0, _updateRate));
        }

        private void EnterState(DroneState s){
            Debug.Log($"{this} entering {s}");

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

        private void ExitState(DroneState s, DroneState next){
            Debug.Log($"{this} exiting {s} to transition to {next}");
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

        private void EnterGuardingState(){
            if(_stateProperties.ContainsKey(DroneState.Guarding)){
                SetAudio(_stateProperties[DroneState.Guarding]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Guarding].velocity;
            }

            _shouldFaceTarget = false;
            _shouldFaceDirection = true;
            _directionToFace = homeFacing;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateGuardingState(){
            while(enabled && _updateRate > 0){
                if(TargetIsAlive()){
                    if(sense && sense.CanSenseTarget()){
                        TransitionState(DroneState.Reacquiring);
                        break;
                    }
                }

                _shouldFaceDirection = true;
                _directionToFace = homeFacing;

                if(_stateProperties.ContainsKey(DroneState.Guarding)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Guarding].velocity;
                }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitGuardingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            StopUpdating(state);
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
        }

        private void EnterReacquiringState(){
            if(_stateProperties.ContainsKey(DroneState.Reacquiring)){
                SetAudio(_stateProperties[DroneState.Reacquiring]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Reacquiring].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateReacquiringState(){
            while(enabled && _updateRate > 0){
                if(!TargetIsAlive()){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                if(sense && sense.CanLocateTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }

                if(FacingTarget()){
                    TransitionState(DroneState.Seeking);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.Reacquiring)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Reacquiring].velocity;
                }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitReacquiringState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterChasingState(){
            if(_stateProperties.ContainsKey(DroneState.Chasing)){
                SetAudio(_stateProperties[DroneState.Chasing]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Chasing].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateChasingState(){
            while(enabled && _updateRate > 0){
                if(!TargetIsAlive()) TransitionState(DroneState.GoingHome);
                if(sense && !sense.CanLocateTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                if(CanAttackTarget()){
                    TransitionState(DroneState.Attacking);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.Chasing)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Chasing].velocity;
                }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitChasingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterSeekingState(){
            if(_stateProperties.ContainsKey(DroneState.Seeking)){
                SetAudio(_stateProperties[DroneState.Seeking]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Seeking].velocity;
            }

            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateSeekingState(){
            while(enabled && _updateRate > 0){
                if(!TargetIsAlive()) TransitionState(DroneState.GoingHome);
                if(sense && sense.CanLocateTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }


                if(_stateProperties.ContainsKey(DroneState.Seeking)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Seeking].velocity;
                }

                _numStateUpdates++;
                if(_numStateUpdates > 10/_updateRate){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                Vector3 pos = transform.position;
                if(sense && sense.CanSenseTarget() &&
                   CanSeeLocation(pos, target.position)){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                for(int i = 0; i < 50; i++){
                    _shouldFaceDirection = true;
                    _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 56f/(51f - i));
                    if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                }


                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitSeekingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterAttackingState(){
            if(_stateProperties.ContainsKey(DroneState.Attacking)){
                SetAudio(_stateProperties[DroneState.Attacking]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Attacking].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
            _shouldFaceDirection = false;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateAttackingState(){
            while(enabled && _updateRate > 0){
                if(!TargetIsAlive()) TransitionState(DroneState.GoingHome);

                if(!CanAttackTarget()){
                    TransitionState(DroneState.Chasing);
                    break;
                }

                if(sense && !sense.CanLocateTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.Attacking)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.Attacking].velocity;
                }

                _numStateUpdates++;

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitAttackingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }


        private void EnterGoingHomeState(){
            if(_stateProperties.ContainsKey(DroneState.GoingHome)){
                SetAudio(_stateProperties[DroneState.GoingHome]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.GoingHome].velocity;
            }

            _shouldFaceTarget = false;
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateGoingHomeState(){
            while(enabled && _updateRate > 0){
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.GoingHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.GoingHome].velocity;
                }

                _numStateUpdates++;

                //find a waypoint
                _currentWaypoint = FindWaypointNear(transform.position);
                if(_currentWaypoint){
                    Debug.Log($"First waypoint: {_currentWaypoint}");
                    TransitionState(DroneState.FollowingWaypointsHome);
                    break;
                }

                //wander until waypoint found
                Vector3 pos = transform.position;

                for(int i = 0; i < 50; i++){
                    _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 56f/(51f - i));
                    if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                }


                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitGoingHomeState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterFollowingWaypointsHomeState(){
            if(_stateProperties.ContainsKey(DroneState.FollowingWaypointsHome)){
                SetAudio(_stateProperties[DroneState.FollowingWaypointsHome]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.FollowingWaypointsHome]
                        .velocity;
            }

            _shouldFaceTarget = false;
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateFollowingWaypointsHomeState(){
            while(enabled && _updateRate > 0){
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.FollowingWaypointsHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.FollowingWaypointsHome]
                            .velocity;
                }

                _numStateUpdates++;

                if(!_currentWaypoint){
                    TransitionState(DroneState.GoingHome);
                    break;
                }

                Vector3 pos = transform.position;

                if(IsAtWaypoint(pos, _currentWaypoint.Position)){
                    if(CanSeeLocation(pos, home)){
                        TransitionState(DroneState.DirectlyHome);
                    }

                    _currentWaypoint = FindNextWaypointOnPath(transform.position, home);
                    Debug.Log($"Next waypoint: {_currentWaypoint}");
                    if(!_currentWaypoint){
                        TransitionState(DroneState.GoingHome);
                        break;
                    }
                }

                _directionToFace = (_currentWaypoint.Position - pos).normalized;
                if(!CanSeeLocation(pos, _currentWaypoint.Position))
                    for(int i = 0; i < 50; i++){
                        if(CanSeeLocation(pos, pos + _directionToFace*(51f - i)/5f)) break;
                        _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 5);
                    }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitFollowingWaypointsHomeState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void EnterDirectlyHomeState(){
            if(_stateProperties.ContainsKey(DroneState.DirectlyHome)){
                SetAudio(_stateProperties[DroneState.DirectlyHome]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.DirectlyHome]
                        .velocity;
            }

            _shouldFaceTarget = false;
            _shouldFaceDirection = true;
            _directionToFace = transform.forward;
            _numStateUpdates = 0;
        }

        private IEnumerator UpdateDirectlyHomeState(){
            while(enabled && _updateRate > 0){
                if(sense && sense.CanSenseTarget()){
                    TransitionState(DroneState.Reacquiring);
                    break;
                }

                if(_stateProperties.ContainsKey(DroneState.DirectlyHome)){
                    _rigidbody.velocity =
                        transform.forward*_stateProperties[DroneState.DirectlyHome].velocity;
                }

                Vector3 pos = transform.position;

                if(IsAtWaypoint(pos, home)){
                    TransitionState(DroneState.Guarding);
                    break;
                }

                _numStateUpdates++;

                _directionToFace = (home - pos).normalized;

                if(!CanSeeLocation(pos, home)){
                    for(int i = 0; i < 10; i++){
                        if(CanSeeLocation(pos, pos + _directionToFace*2)) break;
                        _directionToFace = Rng.Random.UnitNormal3(_directionToFace, 1);
                    }

                    if(_numStateUpdates > 20/_updateRate && !CanSeeLocation(pos, home)){
                        TransitionState(DroneState.GoingHome);
                        break;
                    }
                }

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitDirectlyHomeState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
            _shouldFaceDirection = false;
            StopUpdating(state);
        }

        private void StopAudio(){
            if(audioSourceComponent) audioSourceComponent.Stop();
        }

        private bool FacingTarget(){
            Vector3 position = transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            return Vector3.Angle(direction, transform.forward) <= attackHalfAngle;
        }

        private void FaceTarget(float deltaTime){
            Vector3 position = transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return;
            Vector3 direction = offset/distance;
            FaceDirection(direction, deltaTime);
        }

        private void FaceDirection(Vector3 direction, float deltaTime){
            Vector3 current = transform.forward;
            if(Mathf.Abs(Vector3.Dot(current, Vector3.up)) < .9f){
                transform.up = Vector3.MoveTowards(transform.up, Vector3.up,
                    deltaTime);
            }

            transform.forward = Vector3.MoveTowards(current, direction,
                deltaTime*2);
        }

        private void SetAudio(StateProperties afs){
            if(!audioSourceComponent) return;
            audioSourceComponent.clip = afs.audio;
            audioSourceComponent.volume = afs.volume;
            audioSourceComponent.pitch = afs.pitchMultiplier;
            audioSourceComponent.loop = true;
            audioSourceComponent.Play();
        }

        private bool TargetIsAlive(){
            if(!target){
                _targetHealth = null;
                return false;
            }

            if(!_targetHealth) _targetHealth = target.GetComponent<Health>();
            if(!_targetHealth) return false;
            return _targetHealth.Amount > 0;
        }

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

        private DroneWaypoint FindNextWaypointOnPath(Vector3 start, Vector3 end){
            DroneWaypoint lastWaypoint = FindWaypointNear(end);
            if(CanSeeLocation(start, lastWaypoint.Position)) return lastWaypoint;
            DroneWaypoint firstWaypoint = FindWaypointNear(start);
            if(firstWaypoint == lastWaypoint) return firstWaypoint;
            if(!IsAtWaypoint(start, firstWaypoint.Position)) return firstWaypoint;
            return MakeWaypointPath(firstWaypoint, lastWaypoint);
        }

        private DroneWaypoint MakeWaypointPath(DroneWaypoint firstWaypoint,
            DroneWaypoint lastWaypoint){
            foreach(DroneWaypoint wp in _waypoints){
                wp.PathfindingDistanceToGoal = Mathf.Infinity;
            }

            lastWaypoint.PathfindingDistanceToGoal = 0;
            Queue<DroneWaypoint> openSet = new Queue<DroneWaypoint>();
            openSet.Enqueue(lastWaypoint);
            while(openSet.Count > 0){
                DroneWaypoint w = openSet.Dequeue();
                PathfindingUpdateNeighbors(w, openSet);
            }

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

        private void PathfindingUpdateNeighbors(DroneWaypoint wp,
            Queue<DroneWaypoint> openSet){
            float l = wp.PathfindingDistanceToGoal + 1;
            foreach(DroneWaypoint n in wp.neighbors){
                if(n.PathfindingDistanceToGoal > l){
                    n.PathfindingDistanceToGoal = l;
                    if(!openSet.Contains(n)) openSet.Enqueue(n);
                }
            }
        }

        private bool CanSeeLocation(Vector3 location, Vector3 waypoint){
            Vector3 offset = waypoint - location;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            bool blocked = Physics.SphereCast(location, .5f, direction, out RaycastHit hit,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return !blocked || hit.collider.transform.root == transform;
        }

        private bool IsAtWaypoint(Vector3 location, Vector3 waypoint){
            return Vector3.Distance(location, waypoint) < .5f;
        }

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


        private IEnumerator RenderCoroutine(){
            if(!droneCam) yield break;
            yield return new WaitForSeconds(Rng.Random.NextFloat(0, cameraRenderInterval));
            while(enabled){
                yield return new WaitForSeconds(cameraRenderInterval);
                droneCam.Render();
            }
        }

        [Serializable]
        public class StateProperties {
            public DroneState state;
            public AudioClip audio;
            public float pitchMultiplier = 1;
            public float volume = .5f;
            public float deltaTime;
            public float velocity;
        }

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
    }
}