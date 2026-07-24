using Hermes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hermes
{
    [CreateAssetMenu(menuName = "Hermes/FPControllerPreset")]
    public class FPControllerPreset : ScriptableObject
    {
        [Header("Character Dimensions")]
        public float StandHeight = 1.75f;
        public float CrouchHeight = 1.5f;
        public float ProneHeight = 0.5f;


        [Header("Movement Parameters")]
        public float Acceleration = 20f;
        public float Deceleration = 30f;

        [Space(20f)]
        public float StrafeAcceleration = 20f;
        public float StrafeSprintSpeed = 4f;
        public float StrafeWalkSpeed = 3f;
        public float StrafeCrouchSpeed = 1.5f;

        [Space(15)]
        public float CrouchSpeed = 2f;
        public float WalkSpeed = 3.5f;
        public float SprintSpeed = 10f;


        [Header("Jumping Parameters")]
        [Tooltip("This is how high the character can jump.")]
        public float JumpHeight = 2f;

        public bool CanDoubleJump = true;
        public float CoyoteTime = 0.3f;

        [Tooltip("The Landed event is only triggered when vertical velocity is above minus this value.")]
        [Min(0.001f)]
        public float LandVelocityThreshold = 5f;


        [Header("Looking Parameters")]
        public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);

        public float PitchLimit = 85f;

        [Header("Camera Parameters")]
        public float CameraNormalFOV = 60f;
        public float CameraSprintFOV = 67.5f;
        public float CameraFOVSmoothing = 5f;

        [Header("Head Bobbing")]
        public HeadBobPreset HeadBobWalk;
        public HeadBobPreset HeadBobCrouch;
        public HeadBobPreset HeadBobSprint;


        [Header("Physics Parameters")]
        public float GravityScale = 2f;
        public LayerMask ObstacleLayerMask = Physics.DefaultRaycastLayers;

        public GroundSensorPreset GroundSensor;

        [Header("Sounds & Footsteps Parameters")]
        [Min(0.01f)] public float FootstepWalkInterval = 0.55f;
        [Min(0.01f)] public float FootstepCrouchInterval = 0.4f;
        [Min(0.01f)] public float FootstepSprintInterval = 0.35f;

        [Space(15)]
        [Min(0.01f)] public float FootstepWalkVolume = 0.1f;
        [Min(0.01f)] public float FootstepCrouchVolume = 0.06f;
        [Min(0.01f)] public float FootstepSprintVolume = 0.3f;

        [Space(15)]
        public float MaxLandSoundVolume = 0.7f;
        public float JumpSoundVolume = 0.5f;


        [Header("Animation Lengths (In Seconds)")]
        public float AnimationLengthClimb = 1.3f;
        public float AnimationLengthVault = 1.3f;
        public float AnimationLengthStep = 1.3f;

    }
}

