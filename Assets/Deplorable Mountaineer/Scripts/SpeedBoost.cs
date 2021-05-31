using UnityEngine;

namespace Deplorable_Mountaineer {
    public class SpeedBoost : MonoBehaviour {
        private void OnTriggerEnter(Collider other){
            if(!other.CompareTag("Player")) return;
            other.GetComponent<FirstPersonController>().SpeedBoost();
        }
    }
}