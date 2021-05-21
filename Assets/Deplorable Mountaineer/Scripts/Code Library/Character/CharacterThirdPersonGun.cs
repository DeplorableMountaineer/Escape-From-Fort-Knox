using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Character {
    public class CharacterThirdPersonGun : MonoBehaviour {
        [SerializeField] private CharacterHead head;

        private void OnValidate(){
            if(head == null){
                head = transform.root.GetComponentInChildren<CharacterHead>();
            }
        }

        private void LateUpdate(){
            transform.localRotation = head.transform.localRotation;
        }
    }
}