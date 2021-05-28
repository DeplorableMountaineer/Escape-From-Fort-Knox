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

        [FormerlySerializedAs("audioForStates")] [SerializeField]
        private StateProperties[] stateProperties;

        [SerializeField] private AudioSource audioSourceComponent;
        [SerializeField, ReadOnly] private Vector3 home;

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

        private void OnValidate(){
            home = transform.position;
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
        }

        private void OnDisable(){
            _updateRate = 1000;
            StopUpdating(state);
            ExitState(state, DroneState.Disabled);
        }

        private void FixedUpdate(){
            if(_shouldFaceTarget) FaceTarget(Time.deltaTime);
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
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    _stateUpdateCoroutines[droneState] =
                        StartCoroutine(UpdateReacquiringState());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(droneState), droneState,
                        null);
            }
        }

        private void StopUpdating(DroneState droneState){
            Debug.Log($"{this} no longer updating {droneState}");
            if(_stateUpdateCoroutines.ContainsKey(droneState))
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
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    EnterReacquiringState();
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
                    break;
                case DroneState.Disabled:
                    break;
                case DroneState.Dead:
                    break;
                case DroneState.Reacquiring:
                    ExitReacquiringState(next);
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
        }

        private IEnumerator UpdateGuardingState(){
            while(enabled && _updateRate > 0){
                if(TargetIsAlive()){
                    if(sense && sense.CanSenseTarget()){
                        TransitionState(DroneState.Reacquiring);
                        break;
                    }
                }

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
            _shouldFaceTarget = false;
        }

        private void EnterReacquiringState(){
            if(_stateProperties.ContainsKey(DroneState.Reacquiring)){
                SetAudio(_stateProperties[DroneState.Reacquiring]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Reacquiring].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
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
        }

        private void EnterChasingState(){
            if(_stateProperties.ContainsKey(DroneState.Chasing)){
                SetAudio(_stateProperties[DroneState.Chasing]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Chasing].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
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
        }

        private void EnterSeekingState(){
            if(_stateProperties.ContainsKey(DroneState.Seeking)){
                SetAudio(_stateProperties[DroneState.Seeking]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Seeking].velocity;
            }

            _shouldFaceTarget = false;
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

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitSeekingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
        }

        private void EnterAttackingState(){
            if(_stateProperties.ContainsKey(DroneState.Attacking)){
                SetAudio(_stateProperties[DroneState.Attacking]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.Attacking].velocity;
            }

            _shouldFaceTarget = TargetIsAlive();
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

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitAttackingState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
        }


        private void EnterGoingHomeState(){
            if(_stateProperties.ContainsKey(DroneState.GoingHome)){
                SetAudio(_stateProperties[DroneState.GoingHome]);
                _rigidbody.velocity =
                    transform.forward*_stateProperties[DroneState.GoingHome].velocity;
            }

            _shouldFaceTarget = false;
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

                yield return new WaitForSeconds(_updateRate);
            }
        }

        private void ExitGoingHomeState(DroneState nextState){
            if(!_stateProperties.ContainsKey(nextState))
                StopAudio();
            _shouldFaceTarget = false;
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
            transform.forward = Vector3.MoveTowards(current, direction,
                deltaTime);
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
            if(CanSeeWaypoint(start, lastWaypoint.Position)) return lastWaypoint;
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

        private bool CanSeeWaypoint(Vector3 location, Vector3 waypoint){
            Vector3 offset = waypoint - location;
            float distance = offset.magnitude;
            if(distance < Mathf.Epsilon) return true;
            Vector3 direction = offset/distance;
            bool blocked = Physics.SphereCast(location, .5f, direction, out RaycastHit hit,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return !blocked || hit.collider.transform.root == transform;
        }

        private bool IsAtWaypoint(Vector3 location, Vector3 waypoint){
            return Vector3.Distance(location, waypoint) < .1f;
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
                    bestStraightLine = wp;
                    bestStraightLineDistance = distance;
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
            Reacquiring
        }
    }
}