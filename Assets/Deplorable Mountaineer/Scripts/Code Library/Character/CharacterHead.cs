using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    public class CharacterHead : MonoBehaviour {
        private float _pitch;
        private Transform _transform;

        private void Awake(){
            _transform = transform;
        }

        private void OnEnable(){
            _pitch = _transform.localEulerAngles.x;
        }

        public void AddPitch(float amount){
            _pitch += amount;
            _pitch = Mathf.Clamp(_pitch, -89.9f, 89.9f);
        }

        private void FixedUpdate(){
            _transform.localEulerAngles = new Vector3(-_pitch, 0, 0);
        }
    }
}