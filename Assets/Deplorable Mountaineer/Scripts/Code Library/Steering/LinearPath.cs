#region

using System.Collections.Generic;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class LinearPath : PathBase {
        [SerializeField] private List<Vector3> points;
        [SerializeField] private bool loop = true;

        [Space] [Space] [Header("Visualization")] [SerializeField]
        private Color lineColor = Color.cyan;

        [SerializeField] private string iconPath =
            "Assets/Deplorable Mountaineer/Sprites/Icons/Pushpin_Raised.png";

        [Space] [Space] [Header("Computed Data")] [SerializeField] [ReadOnly]
        private List<Vector3> directionToNext;

        [SerializeField] [ReadOnly] private List<float> distanceToNext;
        [SerializeField] [ReadOnly] private List<float> paramOfThis;
        [SerializeField] [ReadOnly] private float totalLength;

        private void OnDrawGizmosSelected(){
            if(points == null) return;
            for(int i = 0; i < points.Count; i++){
                Vector3 point = points[i];
                Gizmos.DrawIcon(point, iconPath);
                Gizmos.color = lineColor;
                if(i > 0) Gizmos.DrawLine(points[i - 1], point);
                else if(loop && points.Count > 2)
                    Gizmos.DrawLine(points[points.Count - 1], point);
            }
        }

        private void OnValidate(){
            directionToNext.Clear();
            distanceToNext.Clear();
            paramOfThis.Clear();
            totalLength = 0;
            for(int i = 0; i < points.Count; i++)
                if(i < points.Count - 1){
                    Vector3 offset = points[i + 1] - points[i];
                    float distance = offset.magnitude;
                    directionToNext.Add(offset/distance);
                    distanceToNext.Add(distance);
                    paramOfThis.Add(totalLength);
                    totalLength += distance;
                }
                else{
                    Vector3 offset = points[0] - points[i];
                    float distance = offset.magnitude;
                    directionToNext.Add(offset/distance);
                    distanceToNext.Add(distance);
                    paramOfThis.Add(totalLength);
                    totalLength += distance;
                }
        }

        public override float GetParam(Vector3 position, float lastParam){
            if(points.Count < 2) return 0;
            int index =
                GetClosestSegment(position, lastParam, out Vector3 projection);
            float dist = Vector3.Distance(points[index], projection);
            float param = paramOfThis[index] + dist;
            return param;
        }

        public override Vector3 GetPosition(float param){
            return GetPosition(param, out _, out _);
        }

        private int GetClosestSegment(Vector3 position, float lastParam,
            out Vector3 projection){
            float bestDistance = Mathf.Infinity;
            int bestIndex = 0;
            projection = Vector3.zero;
            int startIndex = points.Count - 1;
            for(int index = 0; index < points.Count - 1; index++){
                if(paramOfThis[index + 1] < lastParam) continue;
                startIndex = index;
                break;
            }

            for(int i = 0; i < 3; i++){
                int index = (startIndex + i)%points.Count;
                float distance =
                    DistanceToSegment(position, index, out Vector3 currentProjection);
                if(distance >= bestDistance) continue;
                bestDistance = distance;
                bestIndex = index;
                projection = currentProjection;
            }

            return bestIndex;
        }

        private float DistanceToSegment(Vector3 position, int index, out Vector3 projection){
            Vector3 lineDir = directionToNext[index];
            projection = points[index] +
                         Vector3.Dot(position - points[index], lineDir)*lineDir;
            return Vector3.Distance(position, projection);
        }

        private Vector3 GetPosition(float param, out int index, out float paramPastIndex){
            if(points.Count == 0){
                index = 0;
                paramPastIndex = 0;
                return transform.position;
            }

            float t = loop
                ? Mathf.Repeat(param, totalLength)
                : Mathf.Clamp(param, 0, totalLength);

            for(int i = 1; i < points.Count; i++)
                if(paramOfThis[i] > t){
                    index = i - 1;
                    paramPastIndex = t - paramOfThis[index];
                    return paramPastIndex*directionToNext[index] + points[index];
                }

            index = points.Count - 1;
            paramPastIndex = t - paramOfThis[index];
            return paramPastIndex*directionToNext[index] + points[index];
        }
    }
}