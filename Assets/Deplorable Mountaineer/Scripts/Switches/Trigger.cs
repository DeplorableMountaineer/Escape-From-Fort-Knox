using UnityEngine;
using UnityEngine.Events;

namespace Deplorable_Mountaineer.Switches {
    public class Trigger : MonoBehaviour {
        [SerializeField] private UnityEvent onActivate;
        [SerializeField] private int numActivations = 1;
        [SerializeField] private AudioSource activationSound;

        private void OnTriggerEnter(Collider other){
            if(!other.CompareTag("Player")) return;
            if(numActivations <= 0) return;
            numActivations--;
            onActivate?.Invoke();
            if(activationSound) activationSound.Play();
        }
    }
}