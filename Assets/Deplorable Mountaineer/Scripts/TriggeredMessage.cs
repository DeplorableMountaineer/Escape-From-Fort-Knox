using UnityEngine;

namespace Deplorable_Mountaineer {
    public class TriggeredMessage : MonoBehaviour {
        public string triggerId;
        public GameEvents.MessageEvent MessageEvent;

        private void OnTriggerEnter(Collider other){
            if(!other.CompareTag("Player")) return;
            if(MessageEvent == null) return;
            if(MessageEvent.AudioMessage)
                GameEvents.Instance.Message(MessageEvent.AudioMessage);
            else GameEvents.Instance.Message(MessageEvent.TextMessage);
            if(string.IsNullOrWhiteSpace(MessageEvent.CancelTriggerId)){
                MessageEvent = null;
                return;
            }

            foreach(TriggeredMessage tm in FindObjectsOfType<TriggeredMessage>()){
                if(tm.triggerId == MessageEvent.CancelTriggerId) tm.MessageEvent = null;
            }

            MessageEvent = null;
        }
    }
}