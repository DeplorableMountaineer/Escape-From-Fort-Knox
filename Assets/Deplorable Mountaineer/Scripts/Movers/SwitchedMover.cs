using UnityEngine;

namespace Deplorable_Mountaineer.Movers {
    public class SwitchedMover : MonoBehaviour {
        [SerializeField] private Transform endPosition;
        [SerializeField] private float speed = 1;
        [SerializeField] private new AudioSource audio;

        private bool _activated = false;
        private Vector3 _endPosition;

        private void Awake(){
            _endPosition = endPosition.position;
        }

        private void FixedUpdate(){
            if(!_activated) return;
            Vector3 newPosition = Vector3.MoveTowards(transform.position, _endPosition,
                speed*Time.fixedDeltaTime);
            transform.position = newPosition;
            if(Vector3.Distance(newPosition, _endPosition) < speed*Time.fixedDeltaTime){
                enabled = false;
                audio.Stop();
            }
        }

        public void OnActivate(){
            if(enabled) audio.Play();
            _activated = true;
        }
    }
}