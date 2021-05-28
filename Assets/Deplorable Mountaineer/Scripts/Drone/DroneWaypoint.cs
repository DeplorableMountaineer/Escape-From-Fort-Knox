using System.Collections.Generic;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deplorable_Mountaineer.Drone {
    public class DroneWaypoint : MonoBehaviour {
        [FormerlySerializedAs("_neighbors")] [SerializeField, ReadOnly]
        public List<DroneWaypoint> neighbors = new List<DroneWaypoint>();

        public Vector3 Position => transform.position;

        public float PathfindingDistanceToGoal { get; set; }

        private void Reset(){
            AutoPopulate();
        }

        private void OnValidate(){
            AutoPopulate();
        }

        private void OnDrawGizmosSelected(){
            AutoPopulate();
            foreach(DroneWaypoint wp in neighbors){
                Gizmos.color = wp.neighbors.Contains(this)
                    ? new Color(0, 1, 0, .5f)
                    : new Color(1, 1, 0, 1);
                Gizmos.DrawLine(transform.position, wp.transform.position);
            }
        }

        private void Awake(){
            AutoPopulate();
        }

        private void AutoPopulate(){
            neighbors.Clear();

            Vector3 location = transform.position;
            foreach(DroneWaypoint wp in FindObjectsOfType<DroneWaypoint>()){
                if(wp == this) continue;
                Vector3 direction = wp.transform.position - location;
                float distance = direction.magnitude;
                if(distance < Mathf.Epsilon) continue;
                direction /= distance;
                bool blocked = Physics.SphereCast(location, .5f, direction, out RaycastHit hit,
                    distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if(blocked){
                    continue;
                }

                neighbors.Add(wp);
            }
        }
    }
}