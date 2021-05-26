using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Deplorable_Mountaineer.Code_Library.Mover {
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class PhysicsBodyState : MonoBehaviour, IObjectState {
        [SerializeField] private StateData stateData = new StateData();

        public StateData State {
            get => stateData;
            set => stateData = value;
        }

        private void Reset(){
#if UNITY_EDITOR
            stateData.id = GUID.Generate().ToString();
#endif
        }

        private void OnValidate(){
#if UNITY_EDITOR
            if(string.IsNullOrWhiteSpace(stateData.id))
                stateData.id = GUID.Generate().ToString();
#endif
        }

        public void GetState(){
            Rigidbody rb = GetComponent<Rigidbody>();
            stateData.velocity = rb.velocity;
            stateData.position = rb.position;
            stateData.eulerAngles = rb.rotation.eulerAngles;
        }

        public void SetState(){
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.velocity = stateData.velocity;
            rb.position = stateData.position;
            transform.eulerAngles = stateData.eulerAngles;
        }

        [Serializable]
        public class StateData {
            public Vector3 velocity;
            public Vector3 position;
            public Vector3 eulerAngles;
            public string id;
        }
    }
}