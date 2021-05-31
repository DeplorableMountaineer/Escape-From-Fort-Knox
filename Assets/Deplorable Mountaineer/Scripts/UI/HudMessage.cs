using TMPro;
using UnityEngine;

namespace Deplorable_Mountaineer.UI {
    [RequireComponent(typeof(TMP_Text))]
    public class HudMessage : MonoBehaviour {
        [SerializeField] private float fadeTime = 5;
        private TMP_Text _text;
        private float _alpha = 1;

        private void Awake(){
            _text = GetComponent<TMP_Text>();
        }

        private void Update(){
            if(_alpha <= 0) return;
            _alpha -= Time.deltaTime/fadeTime;
            if(_alpha <= 0){
                _text.alpha = 0;
                return;
            }

            _text.alpha = _alpha;
        }

        public void TextMessage(string text){
            _text.text = text;
            _alpha = 1;
        }

        public static void Message(string text){
            FindObjectOfType<HudMessage>().TextMessage(text);
        }

        public static void Message(AudioClip clip){
            AudioSource a = FindObjectOfType<HudMessage>().GetComponent<AudioSource>();
            a.clip = clip;
            a.Play();
        }
    }
}