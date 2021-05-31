using System.Collections;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class PlayerGun : MonoBehaviour {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform beam;
        [SerializeField] private Collider character;
        [SerializeField] private float firingInterval = .5f;
        [SerializeField] private float range = 25;
        [SerializeField] private float damage = 25;
        [SerializeField] private AudioSource fireAudio;

        private bool _inUse;

        private void OnEnable(){
            GetComponent<Renderer>().enabled = true;
            if(beam) beam.GetComponentInChildren<Renderer>().enabled = false;
        }

        private void OnDisable(){
            GetComponent<Renderer>().enabled = false;
            _inUse = false;
            if(beam) beam.GetComponentInChildren<Renderer>().enabled = false;
        }

        private void Start(){
            enabled = false;
        }

        private void Update(){
            if(Input.GetButton("Fire")) StartCoroutine(MaybeFire());
        }

        private IEnumerator MaybeFire(){
            if(!enabled) yield break;
            if(_inUse) yield break;
            _inUse = true;
            float time = 0;
            Vector3 location = muzzle.position;
            Vector3 direction = muzzle.forward;
            if(beam) beam.GetComponentInChildren<Renderer>().enabled = true;
            if(fireAudio) fireAudio.Play();
            while(time < .1f){
                yield return null;
                time += Time.deltaTime;

                bool blocked = Physics.Raycast(location, direction, out RaycastHit hit,
                    range, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if(blocked && hit.collider != character) ApplyDamage(hit);
                float len = blocked ? hit.distance : range;
                beam.transform.localScale = new Vector3(1, 1, len*4);
            }

            if(beam) beam.GetComponentInChildren<Renderer>().enabled = false;
            yield return new WaitForSeconds(firingInterval - .1f);
            _inUse = false;
        }

        private void ApplyDamage(RaycastHit hitInfo){
            Health health = hitInfo.collider.GetComponentInParent<Health>();
            if(!health) return;
            health.Amount -= damage/.1f*Time.deltaTime;
            health.AddImpulse(10*muzzle.forward);
        }
    }
}