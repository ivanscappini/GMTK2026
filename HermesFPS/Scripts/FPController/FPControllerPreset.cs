using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Hermes
{
    [CreateAssetMenu(menuName = "HermesFPS/FPContollerPreset")]
    public class FPControllerPreset : ScriptableObject
    {
        [Header("Movement Parameters")]
        public float Acceleration = 20f;

        public float WalkSpeed = 5f;
        public float SprintSpeed = 10f;
        public float CrouchSpeed = 2.5f;

        [Header("Jump Parameters")]
        [Tooltip("This is how high the player can jump.")]
        public float JumpHeight = 2f;

        public bool CanDoubleJump = true;
        public float CoyoteTime = 0.15f; //how long after leaving a platform the player can still jump

        [Header("Looking Parameters")]
        public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);
        [Min(0f)] public float LookSmoothing = 0.02f;

        public float PitchLimit = 85f; //how much the player can look up and down

        [Header("Camera Parameters")]
        public float CameraNormalFOV = 60f;
        public float CameraSprintFOV = 67.5f;
        public float CameraFOVSmoothing = 5f;

        [Header("Head Bobbing")]
        public HeadBobPreset HeadBobWalk;
        public HeadBobPreset HeadBobSprint;
        public HeadBobPreset HeadBobCrouch;

        [Header("Physics Parameters")]
        public float GravityScale = 2f;
        public LayerMask ObstacleLayerMask = Physics.DefaultRaycastLayers; //layers that will block the player from standing up when crouching

        [Header("Sound Parameters")]
        public float FootstepWalkRate = 0.5f; // normal
        public float FootstepSprintRate = 0.3f; // fast
        public float FootstepCrouchRate = 0.7f; // slow

        [Space(15)]
        public float FootstepWalkVolume = 0.8f;
        public float FootstepSprintVolume = 1.0f;
        public float FootstepCrouchVolume = 0.6f;

        [Space(15)]
        public float JumpSoundVolume = 0.4f;
        public float MaxLandSoundVolume = 0.7f;
    }
}
