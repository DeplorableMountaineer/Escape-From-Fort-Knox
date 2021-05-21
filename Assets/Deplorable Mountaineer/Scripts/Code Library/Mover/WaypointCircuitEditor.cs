using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Mover {
    [CustomEditor(typeof(WaypointCircuit)), CanEditMultipleObjects]
    public class WaypointCircuitEditor : Editor {
        public override void OnInspectorGUI(){
            base.OnInspectorGUI();
            WaypointCircuit circuit = (WaypointCircuit) target;

            if(GUILayout.Button("Add Waypoint")){
                Undo.RecordObject(circuit, "Added waypoint");

                Vector3 pos = Vector3.zero;
                if(circuit.localPositions.Count > 0){
                    pos = circuit.localPositions[circuit.localPositions.Count - 1] +
                          new Vector3(.5f, 0, .5f);
                }

                circuit.localPositions.Add(pos);
            }

            if(circuit.localPositions.Count > 0){
                if(GUILayout.Button("Snap All Waypoints")){
                    Undo.RecordObject(circuit, "Snapped all waypoints");
                    for(int i = 0; i < circuit.localPositions.Count; i++){
                        SnapWaypoint(circuit, i);
                    }
                }
            }
        }

        public void OnSceneGUI(){
            WaypointCircuit circuit = (WaypointCircuit) target;
            Transform t = circuit.transform;
            Undo.RecordObject(circuit, "Moved waypoint");

            for(int i = 0; i < circuit.localPositions.Count; i++){
                circuit.localPositions[i] =
                    t.InverseTransformPoint(Handles.PositionHandle(
                        t.TransformPoint(circuit.localPositions[i]),
                        quaternion.identity));
                Handles.Label(t.InverseTransformPoint(circuit.localPositions[i]),
                    $"Waypoint {i}");
            }
        }

        private static void SnapWaypoint(WaypointCircuit circuit, int i){
            Vector3 pos = circuit.localPositions[i];
            if(circuit.snapping.x > Mathf.Epsilon){
                pos.x = Mathf.Round(pos.x/circuit.snapping.x)*circuit.snapping.x;
                pos.y = Mathf.Round(pos.y/circuit.snapping.y)*circuit.snapping.y;
                pos.z = Mathf.Round(pos.z/circuit.snapping.z)*circuit.snapping.z;
            }

            circuit.localPositions[i] = pos;
        }
    }
}