using UnityEngine;

namespace Deplorable_Mountaineer {
    public class PlayerGunPickup : MonoBehaviour {
        private void OnEnable(){
            GetComponent<Renderer>().enabled = true;
        }

        private void OnDisable(){
            GetComponent<Renderer>().enabled = false;
        }

        private void OnTriggerEnter(Collider other){
            if(!enabled) return;
            if(other.CompareTag("Player")){
                PlayerGun g = other.GetComponentInChildren<PlayerGun>();
                g.enabled = true;
                GameEvents.Instance.Message(GameEvents.Instance.AudioMessages["Action"]);
                enabled = false;
            }
        }
    }
}