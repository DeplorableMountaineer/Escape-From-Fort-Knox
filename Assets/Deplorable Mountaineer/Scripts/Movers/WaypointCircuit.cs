using System.Collections.Generic;
using UnityEngine;

namespace Deplorable_Mountaineer.Movers {
    public class WaypointCircuit : MonoBehaviour {
        [SerializeField] private List<Waypoint> waypoints;

        [SerializeField] private string iconPath =
            "Assets/Deplorable Mountaineer/Sprites/Icons/Pushpin_Raised.png";

        [SerializeField] private string iconPathEmpty =
            "Assets/Deplorable Mountaineer/Sprites/Icons/Path.png";

        private int _currentWaypoint = 0;

        private void OnDrawGizmosSelected(){
            bool drawn = false;
            foreach(Waypoint wp in waypoints){
                drawn = true;
                Vector3 position = wp.transform.position;
                Gizmos.DrawIcon(position, iconPath, true);
            }

            if(!drawn) Gizmos.DrawIcon(transform.position, iconPathEmpty, true);
        }

        public Waypoint GetNearestWaypoint(Vector3 position){
            Waypoint nearest = null;
            float distance = 0;
            int j = 0;
            for(int i = 0; i < waypoints.Count; i++){
                j = (_currentWaypoint + i)%waypoints.Count;
                float d = Vector3.Distance(position, waypoints[j].transform.position);
                if(nearest != null && d >= distance) continue;
                nearest = waypoints[j];
                distance = d;
            }

            _currentWaypoint = j;
            return nearest;
        }

        public Waypoint GetNextWaypoint(Waypoint current){
            if(current == null) return null;
            for(int i = 0; i < waypoints.Count; i++){
                int j = (_currentWaypoint + i)%waypoints.Count;
                if(current != waypoints[j]) continue;
                j = (j + 1)%waypoints.Count;
                _currentWaypoint = j;
                return waypoints[j];
            }

            return GetNearestWaypoint(current.transform.position);
        }
    }
}