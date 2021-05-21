using System.Collections.Generic;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class PlatformObjectCarrier : MonoBehaviour {
        private readonly Dictionary<Collider, PlatformRiderObject> _objects =
            new Dictionary<Collider, PlatformRiderObject>();

        private void OnTriggerEnter(Collider other){
            if(!_objects.ContainsKey(other)){
                _objects[other] = other.GetComponent<PlatformRiderObject>();
            }

            if(_objects[other]) other.transform.SetParent(transform, true);
        }

        private void OnTriggerExit(Collider other){
            if(!_objects.ContainsKey(other)) return;
            if(_objects[other]) other.transform.SetParent(null, true);
            _objects.Remove(other);
        }
    }
}