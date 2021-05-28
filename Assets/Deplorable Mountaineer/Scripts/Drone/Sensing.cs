using UnityEngine;

namespace Deplorable_Mountaineer.Drone {
    public class Sensing : MonoBehaviour {
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float sightConeHalfAngle = 30;
        [SerializeField] private float sightConeDistance = 20;
        [SerializeField] private float hearingDistance = 5;

        private Transform _transform;
        private int _seenFrameChecked = -1;
        private int _heardFrameChecked = -1;
        private bool _seen = false;
        private bool _heard = false;

        private void OnValidate(){
            if(target || string.IsNullOrWhiteSpace(targetTag)) return;
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if(go) target = go.transform;
        }

        private void Awake(){
            _transform = transform;
        }

        public bool CanSenseTarget(){
            return CanSeeTarget();
        }

        public bool CanLocateTarget(){
            return CanSeeTarget() || CanHearTarget();
        }

        private bool CanHearTarget(){
            if(!target) return _heard = false;
            if(_heardFrameChecked == Time.frameCount) return _heard;
            _heardFrameChecked = Time.frameCount;
            Vector3 position = _transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            return distance <= hearingDistance;
        }

        private bool CanSeeTarget(){
            if(!target) return _seen = false;
            if(_seenFrameChecked == Time.frameCount) return _seen;
            _seenFrameChecked = Time.frameCount;
            Vector3 position = _transform.position;
            Vector3 targetPos = target.position;
            Vector3 offset = targetPos - position;
            float distance = offset.magnitude;
            if(distance > sightConeDistance) return _seen = false;
            if(distance < Mathf.Epsilon) return _seen = true;
            Vector3 direction = offset/distance;
            if(Vector3.Angle(direction, _transform.forward) > sightConeHalfAngle) return false;
            bool blocked = Physics.Raycast(position, direction, out RaycastHit hit,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if(!blocked) return _seen = true;
            Transform root = hit.collider.transform.root;
            return _seen = (root == transform || root == target.root);
        }
    }
}