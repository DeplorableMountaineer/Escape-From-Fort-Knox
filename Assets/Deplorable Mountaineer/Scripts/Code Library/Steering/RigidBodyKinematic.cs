using System.Collections.Generic;
using UnityEngine;

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class RigidBodyKinematic : SimpleKinematicComponent {
        [SerializeField] public SimpleKinematicComponent steeringTarget;
        [SerializeField] public PathBase path;
        [SerializeField] public float pathOffset = 1;
        [SerializeField] public SteeringParams steeringParams;
        [SerializeField] public List<SteeringGroup> priorityGroups;
        [SerializeField] public WeightedBehavior alwaysBehaviorForPriority;

    }
}