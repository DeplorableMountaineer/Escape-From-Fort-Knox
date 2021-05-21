#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class PrioritySteering : IMovement {
        public PrioritySteering(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }

        public SteeringOutput GetSteering(){
            SteeringOutput result = new SteeringOutput();

            foreach(SteeringGroup g in Self.priorityGroups){
                result = BlendedSteering.GetSteeringBlend(g.behaviorBlend, Self);
                if(result.Linear.HasValue && result.Linear.Value.magnitude >
                   Self.steeringParams.priorityEpsilon
                   || result.Eulers.HasValue && result.Eulers.Value.magnitude >
                   Self.steeringParams.priorityEpsilon
                   || result.Angular.HasValue && result.Angular.Value.magnitude >
                   Self.steeringParams.priorityEpsilon){
                    result += Self.Movements[Self.alwaysBehaviorForPriority.behavior]
                        .GetSteering()*Self.alwaysBehaviorForPriority.weight;
                    return result;
                }
            }

            result += Self.Movements[Self.alwaysBehaviorForPriority.behavior]
                .GetSteering()*Self.alwaysBehaviorForPriority.weight;
            return result;
        }
    }


    [Serializable]
    public class SteeringGroup {
        [SerializeField] public List<WeightedBehavior> behaviorBlend;
    }
}