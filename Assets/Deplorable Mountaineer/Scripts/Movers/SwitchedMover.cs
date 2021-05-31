using Deplorable_Mountaineer.Switches;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deplorable_Mountaineer.Movers {
    public class SwitchedMover : MonoBehaviour {
        [SerializeField] private Transform endPosition;
        [SerializeField] private float speed = 1;
        [FormerlySerializedAs("audio")] [SerializeField] private AudioSource audioSource;

        private bool _activated = false;
        private Vector3 _endPosition;
        private Vector3 _startPosition;

        public bool Activated {
            get => _activated;
            set {
                if(!_activated && value){
                    transform.position = _endPosition;
                    FindObjectOfType<Trigger>().numActivations = 0;
                }
                else if(_activated && !value){
                    transform.position = _startPosition;
                    audioSource.Stop();
                }

                _activated = value;
            }
        }

        private void Awake(){
            _endPosition = endPosition.position;
            _startPosition = transform.position;
        }

        private void FixedUpdate(){
            if(!Activated) return;
            Vector3 newPosition = Vector3.MoveTowards(transform.position, _endPosition,
                speed*Time.fixedDeltaTime);
            transform.position = newPosition;
            if(Vector3.Distance(newPosition, _endPosition) < speed*Time.fixedDeltaTime){
                enabled = false;
                audioSource.Stop();
            }
        }

        public void OnActivate(){
            if(enabled) audioSource.Play();
            _activated = true; //do not use property, which is instant-open
        }
    }
}