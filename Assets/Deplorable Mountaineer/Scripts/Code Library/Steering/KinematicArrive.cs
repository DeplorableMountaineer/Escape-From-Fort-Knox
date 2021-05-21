﻿#region

using System;
using UnityEngine;

#endregion

namespace Deplorable_Mountaineer.Code_Library.Steering {
    public class KinematicArrive : IMovement {
        private IKinematic _target;

        public KinematicArrive(Kinematic self){
            Self = self;
        }

        public Kinematic Self { get; set; }
        public IKinematic OverrideTarget { get; set; }


        public SteeringOutput GetSteering(){
            _target = OverrideTarget ?? Self.steeringTarget;

            Self.EulerRotation = Vector3.zero;
            Self.AngularVelocity = Vector3.zero;
            Self.Velocity = _target.Position - Self.Position;
            if(Self.steeringParams.keepUpright){
                Vector3 e = Self.EulerAngles;
                e.z = 0;
                e.x = 0;
                Self.EulerAngles = e;
            }

            if(Self.Velocity.magnitude < Self.steeringParams.acceptanceRadius){
                Self.Velocity = Vector3.zero;
                return default;
            }

            Self.Velocity /= Self.steeringParams.timeToTarget;
            Self.Velocity =
                Vector3.ClampMagnitude(Self.Velocity, Self.steeringParams.maxSpeed);

            switch(Self.steeringParams.kinematicFacing){
                case Facing.FaceVelocity:
                    Self.Look(Self.Velocity, Self.Up);
                    break;
                case Facing.FaceTarget:
                    Self.Look(_target.Position - Self.Position, Self.Up);
                    break;
                case Facing.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }
    }
}