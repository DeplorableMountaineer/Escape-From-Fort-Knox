using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Deplorable_Mountaineer {
    public class GameOver : MonoBehaviour {
       
        
        private IEnumerator OnTriggerEnter(Collider other){
            if(!other.CompareTag("Player")) yield break;
            yield return new WaitForSeconds(2);
            GameEvents.Instance.Message("Game Over!");
            FindObjectOfType<CharacterController>().enabled = false;
            FindObjectOfType<FirstPersonController>().enabled = false;
            FindObjectOfType<PlayerGun>().enabled = false;
            while(enabled){
                yield return null;
                if(Input.anyKeyDown){
                    GameSaver.Instance.volumeBar.gameObject.SetActive(true);
                    GameSaver.Instance.restarting = true; 
                    SceneManager.LoadScene(0);
                }
            }
        }
    }
}