using Deplorable_Mountaineer.UI;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class Health : MonoBehaviour {
        [SerializeField] private float startingValue = 100;
        [SerializeField] private float maxValue = 100;
        [SerializeField] private ValueBar healthBar;

        private float _health;

        public float Amount {
            get => _health;
            set {
                float old = _health;
                _health = Mathf.Clamp(value, 0, maxValue);
                OnHealthUpdate(old, _health, maxValue);
            }
        }

        private void Start(){
            Amount = startingValue;
        }

        private void OnHealthUpdate(float old, float health, float max){
            if(healthBar) healthBar.Amount = health/max;
            if(health < Mathf.Epsilon){
                _health = 0;
                OnDeath();
            }
        }

        private void OnDeath(){
            Debug.Log("Player is dead.");
            Debug.Break();
        }
    }
}