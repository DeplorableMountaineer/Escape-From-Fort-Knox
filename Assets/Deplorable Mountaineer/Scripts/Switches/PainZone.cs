using UnityEngine;

namespace Deplorable_Mountaineer.Switches {
    public class PainZone : MonoBehaviour {
        [SerializeField] private float amountPerSecond = 50;

        private void OnTriggerStay(Collider other){
            Health h = other.GetComponent<Health>();
            if(!h) return;
            h.Amount -= amountPerSecond*Time.fixedDeltaTime;
        }
    }
}
