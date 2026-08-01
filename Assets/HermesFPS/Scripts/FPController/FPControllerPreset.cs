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
        public float SlideHeight = 0.5f;


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

        // Air control tuning
        [Header("Air Control")]
        [Tooltip("Multiplier applied to acceleration while in air (0..1). Lower = weaker air control.")]
        [Range(0f, 1f)] public float AirAccelerationMultiplier = 0.5f;

        [Tooltip("Multiplier applied to deceleration while in air (0..1). Lower = momentum preserved longer in air.")]
        [Range(0f, 1f)] public float AirDecelerationMultiplier = 0.08f;

        [Tooltip("Multiplier applied to strafe acceleration while in air.")]
        [Range(0f, 1f)] public float AirStrafeAccelerationMultiplier = 0.5f;

        [Tooltip("Multiplier applied to strafe max speed while in air.")]
        [Range(0f, 1f)] public float AirStrafeSpeedMultiplier = 1f;


        [Header("Jumping Parameters")]
        [Tooltip("This is how high the character can jump.")]
        public float JumpHeight = 2f;

        [Tooltip("Extra horizontal speed added on jump (m/s)")]
        public float JumpHorizontalBoost = 1.5f;

        public bool CanDoubleJump = true;
        public float CoyoteTime = 0.3f;

        [Header("Slide Parameters")]
        [Header("Tuning")]
        public float BoostAmount = 4f;
        public float Friction = 1.2f;
        public float MinStartSpeed = 6f;
        public float StopSpeedThreshold = 3f;
        public float MaxSlideDuration = 1.6f;
        public float MinSlideTime = 0.25f;

        [Header("Slide Pushout")]
        [Tooltip("Maximum time (seconds) to push the player forward when attempting to cancel a slide while blocked by a ceiling")]
        public float SlidePushOutMaxDuration = 2.0f;

        [Tooltip("Minimum forward speed (m/s) to use when pushing the player out from under a ceiling")]
        public float SlidePushOutMinSpeed = 3.0f;

        [Header("Behaviour")]
        [Tooltip("Time (seconds) to blend movement out when slide is cancelled so animation can transition smoothly.")]
        [Min(0f)]
        public float SlideExitBlendTime = 0.5f;

        [Tooltip("Fraction of forward momentum preserved when cancelling the slide (0..1). 1 = keep full speed, 0 = hard stop.")]
        [Range(0f, 1f)]
        public float SlideCancelMomentum = 0.75f;

        [Tooltip("Cooldown (seconds) after finishing a slide before another slide can start")]
        [Min(0f)]
        public float SlideCooldown = 0.25f;

        [Tooltip("Maximum degrees per second the slide direction adjusts toward the camera look direction (0 = locked direction)")]
        public float SlideSteerRate = 45f;

        [Tooltip("Acceleration (m/s²) applied by slope steepness. Positive = faster downhill, slower uphill.")]
        public float SlideSlopeAcceleration = 15f;

        [Tooltip("Camera Y offset during slide (negative = lower). Drives how far the camera drops from standing height.")]
        public float SlideCameraDropOffset = -0.85f;

        [Header("Slide Boost")]
        [Tooltip("Perfect window opens when speed drops below this ratio of initial slide speed (0..1). e.g. 0.4 = below 40%")]
        [Range(0f, 1f)]
        public float SlidePerfectJumpStart = 0.4f;

        [Tooltip("Perfect window closes when speed drops below this ratio (0..1). e.g. 0.15 = below 15%")]
        [Range(0f, 1f)]
        public float SlidePerfectJumpEnd = 0.15f;

        [Tooltip("Speed multiplier applied to exit velocity when jumping in the perfect window")]
        [Min(1f)]
        public float SlidePerfectJumpBoost = 1.6f;

        [Tooltip("Extra vertical velocity added on a perfect jump for a bigger leap")]
        public float SlidePerfectJumpVerticalBoost = 3f;


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

        [Header("Wall Jump Parameters")]
        [Tooltip("How far from a wall the player can register a wall jump")]
        public float WallCheckDistance = 1.0f;

        [Tooltip("Horizontal impulse applied away from the wall")]
        public float WallAwayVelocity = 6f;

        [Tooltip("Multiplier applied to base jump vertical velocity for wall jumps")]
        public float WallUpVelocityMultiplier = 1f;

        [Tooltip("Additional small push in camera forward direction when wall jumping")]
        public float WallCameraPush = 2f;

        [Tooltip("Max vertical difference (meters) between player chest and wall hit to consider a valid wall")]
        public float WallMaxVerticalOffset = 1.2f;

        [Tooltip("Weight applied to camera distance when choosing which hit to use")]
        [Range(0f, 1f)] public float WallCameraDistanceWeight = 0.25f;

        [Tooltip("Cooldown after a wall jump before another can be performed")]
        public float WallJumpCooldown = 0.25f;

        [Tooltip("Minimum horizontal speed after a wall jump to avoid feeling stuck")]
        public float WallMinSpeed = 3f;

        [Tooltip("Weight of away direction when mixing impulse direction (0..1)")]
        [Range(0f, 1f)] public float WallAwayWeight = 0.85f;

        [Tooltip("Weight of camera forward when mixing impulse direction (0..1)")]
        [Range(0f, 1f)] public float WallCamWeight = 0.15f;

        [Header("Wall Run Parameters")]
        [Tooltip("Speed along the wall while wall running")]
        public float WallRunSpeed = 10f;

        [Tooltip("Maximum duration of a wall run in seconds")]
        public float WallRunMaxDuration = 2.0f;

        [Tooltip("If set, only colliders with this tag will allow wall running. Empty = any wall allowed.")]
        public string WallRunAllowedTag = "";

        [Tooltip("Blend speed used when applying wall-run horizontal velocity (higher = snappier)")]
        public float WallRunBlend = 10f;

        [Tooltip("Delay after kick-off before wall-run can reattach")]
        public float WallRunReattachDelay = 0.25f;

        [Tooltip("Minimum alignment (0..1) between velocity and wall to start a run. Prevents sticking when hitting walls head-on.")]
        [Range(0f, 1f)] public float WallRunMinAlignment = 0.3f;

        [Tooltip("Gravity multiplier during wall run (0 = float, 1 = full gravity). Controls the gliding feel.")]
        [Range(0f, 1f)] public float WallRunGravityScale = 0.1f;

        [Tooltip("Upward speed (m/s) applied during wall run. Positive = lift, 0 = neutral float, negative = sink.")]
        public float WallRunLiftSpeed = 2f;

        [Tooltip("Upward velocity (m/s) applied when kicking off a wall. 0 = no vertical boost.")]
        public float WallRunKickOffVerticalSpeed = 5f;

        [Tooltip("Camera Z-axis tilt angle (degrees) while wall running")]
        public float WallRunCameraTilt = 12f;

        [Tooltip("Speed (degrees/sec) at which camera tilt blends in and out")]
        public float WallRunCameraTiltSpeed = 80f;

        [Tooltip("Minimum dot product between old and new wall normals. Below this the wall run ends (corner detection).")]
        [Range(0f, 1f)] public float WallRunMinNormalDot = 0.5f;

    }
}

