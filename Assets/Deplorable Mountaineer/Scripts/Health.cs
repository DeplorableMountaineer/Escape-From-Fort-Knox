using Deplorable_Mountaineer.Code_Library.Mover;
using Deplorable_Mountaineer.Drone;
using Deplorable_Mountaineer.UI;
using UnityEngine;

namespace Deplorable_Mountaineer {
    public class Health : MonoBehaviour {
        [SerializeField] private float startingValue = 100;
        [SerializeField] private float maxValue = 100;
        [SerializeField] private ValueBar healthBar;
        [SerializeField] public GameObject spawnOnDeath;

        private float _health;
        private Rigidbody _rigidbody;

        public float Amount {
            get => _health;
            set {
                float old = _health;
                _health = Mathf.Clamp(value, 0, maxValue);
                OnHealthUpdate(old, _health, maxValue);
            }
        }

        private void Awake(){
            _rigidbody = GetComponentInChildren<Rigidbody>();
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

            if(CompareTag("Player")) return;
            Sensing s = GetComponentInChildren<Sensing>();
            if(s){
                s.GivePain();
            }
        }

        public void AddImpulse(Vector3 force, ForceMode forceMode = ForceMode.VelocityChange){
            if(!_rigidbody) return;
            _rigidbody.AddForce(force, forceMode);
        }

        private void OnDeath(){
            if(CompareTag("Player")){
                GameEvents.Instance.OnPlayerDead();
                return;
            }
            else{
                Drone.Drone d = GetComponentInChildren<Drone.Drone>();
                if(spawnOnDeath){
                    Transform t = transform;
                    Instantiate(spawnOnDeath, t.position, t.rotation);
                }

                if(GetComponentInChildren<PhysicsBodyState>()){
                    transform.position = new Vector3(10000, 10000, 10000);
                }
                else if(d){
                    transform.position = new Vector3(10000, 10000, 10000);
                    d.state = Drone.Drone.DroneState.Dead;
                }
                else Destroy(gameObject, .2f);
            }
        }
    }
}