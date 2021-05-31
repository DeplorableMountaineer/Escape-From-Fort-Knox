using System.Collections;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class ExplosionDamage : MonoBehaviour {
        [SerializeField] private float innerRadius = 1;
        [SerializeField] private float outerRadius = 3;
        [SerializeField] private float maxDamage = 100;
        [SerializeField] private float nonPlayerMultiplier = 5;
        [SerializeField] private float damageDelay = .2f;

        private void Awake(){
            Vector3 position = transform.position;
            foreach(Health h in FindObjectsOfType<Health>()){
                Vector3 hPos = h.transform.position;
                Vector3 direction = hPos - position;
                float distance = direction.magnitude;
                if(distance < Mathf.Epsilon || distance > outerRadius) continue;
                direction /= distance;
                bool blocked = Physics.Raycast(position, direction, out RaycastHit hit,
                    distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if(blocked && hit.collider.GetComponentInParent<Health>() != h) continue;
                float amount = (outerRadius -
                                Mathf.Clamp(distance, innerRadius, outerRadius))/
                    (outerRadius - innerRadius)*maxDamage;
                if(!h.CompareTag("Player")) amount *= nonPlayerMultiplier;
                StartCoroutine(DoDamage(damageDelay, amount, h, direction));
            }
        }

        private IEnumerator DoDamage(float delay, float amount, Health health,
            Vector3 direction){
            health.AddImpulse(10*direction);
            yield return new WaitForSeconds(delay);
            health.Amount -= amount;
        }
    }
}