using System.Collections.Generic;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Mover {
    public class WaypointCircuit : MonoBehaviour {
        [SerializeField] public List<Vector3> localPositions = new List<Vector3>();
        [SerializeField] public Vector3 snapping = new Vector3(.25f, .25f, .25f);

        private int _currentWaypoint = 0;

        public int Count => localPositions.Count;

        public int CurrentWaypoint {
            get => GetWaypoint();
            set => SetWaypoint(value);
        }


        public void NextWaypoint(){
            CurrentWaypoint++;
        }

        public void SetWaypoint(int i){
            _currentWaypoint = ((i%localPositions.Count) + localPositions.Count)%
                               localPositions.Count;
        }

        public int GetWaypoint(){
            return _currentWaypoint;
        }

        public Vector3 GetPosition(){
            return localPositions.Count == 0
                ? transform.position
                : transform.TransformPoint(localPositions[_currentWaypoint]);
        }

        private void OnDrawGizmosSelected(){
            if(localPositions.Count < 2) return;
            Gizmos.color = Color.blue;
            for(int i = 0; i < localPositions.Count; i++){
                int j = (i + 1)%localPositions.Count;
                Gizmos.DrawLine(transform.TransformPoint(localPositions[i]),
                    transform.TransformPoint(localPositions[j]));
            }
        }
    }
}