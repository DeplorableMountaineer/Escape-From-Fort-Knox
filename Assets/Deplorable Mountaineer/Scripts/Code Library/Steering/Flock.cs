#region

using System.Collections;
using System.Collections.Generic;
using Deplorable_Mountaineer.EditorUtils.Attributes;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    /// <summary>
    ///     Component for parent game object of a flock of kinematic actors.
    /// </summary>
    public class Flock : MonoBehaviour, IEnumerable<SimpleKinematicComponent> {
        [Tooltip("How often to update center of gravity")] [SerializeField]
        private float updateRate = .05f;

        [Tooltip("Should mass be taken as volume of actor's sphere instead of as unit mass.")]
        [SerializeField]
        private bool weightByRadiusCubed = true;

        [Tooltip("read-only list of flock members; available when game is running")]
        [SerializeField]
        [ReadOnly]
        private List<SimpleKinematicComponent> flockMembers;

        private Vector3 _lastCenterOfMass;

        private float _lastUpdateTime;


        /// <summary>
        ///     Current center of mass of the flock
        /// </summary>
        [PublicAPI]
        public Vector3 CenterOfMass => GetCenterOfMass();

        private void Awake(){
            foreach(SimpleKinematicComponent c in
                GetComponentsInChildren<SimpleKinematicComponent>())
                AddToFlock(c);

            ComputeCenterOfMass();
        }

        /// <summary>
        ///     Collection of flock members
        /// </summary>
        /// <returns>Enumerator for flock members</returns>
        public IEnumerator<SimpleKinematicComponent> GetEnumerator(){
            foreach(SimpleKinematicComponent c in flockMembers)
                yield return c;
        }

        IEnumerator IEnumerable.GetEnumerator(){
            return GetEnumerator();
        }

        /// <summary>
        ///     Add a new member to the flock
        /// </summary>
        /// <param name="c">the actor</param>
        [PublicAPI]
        public void AddToFlock(SimpleKinematicComponent c){
            if(c.InFlock == this) return;
            if(c.InFlock != null) c.InFlock.RemoveFromFlock(c);
            flockMembers.Add(c);
            c.InFlock = this;
        }

        /// <summary>
        ///     Remove a member from the flock
        /// </summary>
        /// <param name="c">the actor</param>
        [PublicAPI]
        public void RemoveFromFlock(SimpleKinematicComponent c){
            flockMembers.Remove(c);
            c.InFlock = null;
        }

        private Vector3 GetCenterOfMass(){
            if(Time.time - _lastUpdateTime < updateRate) return _lastCenterOfMass;
            return ComputeCenterOfMass();
        }

        private Vector3 ComputeCenterOfMass(){
            _lastUpdateTime = Time.time;
            _lastCenterOfMass = Vector3.zero;
            float weight = 0;
            foreach(SimpleKinematicComponent c in flockMembers){
                if(!c.enabled) continue;
                float w = 1;
                if(weightByRadiusCubed) w = c.Radius*c.Radius*c.Radius;
                _lastCenterOfMass += c.Position*w;
                weight += w;
            }

            if(weight > Mathf.Epsilon) _lastCenterOfMass /= weight;
            return _lastCenterOfMass;
        }
    }
}