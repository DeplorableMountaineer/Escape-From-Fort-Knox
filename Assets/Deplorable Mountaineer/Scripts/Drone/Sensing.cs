using UnityEngine;

namespace Deplorable_Mountaineer.Drone {
    public class Sensing : MonoBehaviour {
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float sightConeHalfAngle = 30;
        [SerializeField] private float sightConeDistance = 10;

        private Transform _transform;
        private int _frameChecked = -1;
        private bool _seen = false;

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
            return CanSeeTarget();
        }

        private bool CanSeeTarget(){
            if(!target) return _seen = false;
            if(_frameChecked == Time.frameCount) return _seen;
            _frameChecked = Time.frameCount;
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