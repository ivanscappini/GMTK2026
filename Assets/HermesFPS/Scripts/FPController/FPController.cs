using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;

namespace Hermes
{
    [RequireComponent(typeof(CharacterController))]

    public class FPController : MonoBehaviour
    {
        public FPControllerPreset Preset;
        
        float MaxSpeed
        {
            get
            {
                // Character is always sprinting now.
                return Preset.SprintSpeed;
            }
        }

        public bool Sprinting
        {
            get
            {
                // Always sprinting by design.
                return true;
            }
        }

        [SerializeField] float currentPitch = 0f;

        public float CurrentPitch
        {
            get => currentPitch;

            set
            {
                currentPitch = Mathf.Clamp(value, -Preset.PitchLimit, Preset.PitchLimit);
            }
        }

        [Header("Camera Parameters")]
        // public Vector3 CurrentCameraPosition {get; private set;} = new Vector3(0f, 1.6f, 0f);

        [Space(10)]
        public Vector3 CameraPositionOffset = Vector3.zero;
        public Quaternion CameraRotationOffset = Quaternion.identity;

        [Header("Physics Parameters")]

        public float VerticalVelocity = 0f;

        public bool LookEnabled = true;
        public bool MovementEnabled = true;

        public Vector3 CurrentVelocity { get; private set; }
        public float CurrentSpeed { get; private set; }

        private bool wasGrounded = false;

        public bool Grounded => characterController.isGrounded;

        [Header("Input")]
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public bool SprintInput;

        [Tooltip("When enabled, sprint will toggle on/off with a press. When disabled, sprint is hold-to-run.")]
        public bool SprintToggleMode = false;

        [Header("Components")]
        [SerializeField] CinemachineCamera fpCamera;
        [SerializeField] CharacterController characterController;

        public CharacterController CharacterController => characterController;

        public CinemachineCamera Camera => fpCamera;

        public Transform CameraTransform => fpCamera.transform;

        [Header("Activities")]
        public FPSprint Sprint;

        [Header("Events")]
        public UnityEvent Landed;
        public UnityEvent Jumped;
        public UnityEvent DoubleJumped;

        public UnityAction TryJump;
        public UnityAction RequestCameraOffset;

        Vector2 smoothedLookInput = Vector2.zero;
        Vector2 lookInputVelocity = Vector2.zero;

        #region Unity Methods
        void OnValidate()
        {
            if(characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            UpdateCameraOffset();

            MoveUpdate();
            LookUpdate();
            CameraUpdate();

            // camera height / character collider: crouch removed, always standing height
            Vector3 targetCameraPosition = Vector3.up * 1.6f; // default camera height

            characterController.height = 2f;
            characterController.center = Vector3.up * 1f; // keep a consistent hitbox

            CurrentCameraPosition = Vector3.Lerp(CurrentCameraPosition, targetCameraPosition, 7f * Time.deltaTime);

            if (!wasGrounded && Grounded)
            {
                Landed?.Invoke();
            }

            wasGrounded = Grounded;
        }

        # endregion

        #region Controller Methods

        void MoveUpdate()
        {
            if (MovementEnabled == false)
            {
                VerticalVelocity = 0f;
                CurrentVelocity = Vector3.zero;
                return;
            }

            Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x; //y input is forward/backward (W/S), x input is left/right (A/D)
            motion.y = 0f;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f)
            {
                CurrentVelocity = Vector3.Lerp(CurrentVelocity, motion * MaxSpeed, Preset.Acceleration * Time.deltaTime);
            }
            else
            {
                CurrentVelocity = Vector3.Lerp(CurrentVelocity, Vector3.zero, Preset.Acceleration * Time.deltaTime);
            }

            if (Grounded && VerticalVelocity <= 0.01f)
            {
                VerticalVelocity = -3f; //downward force to keep the player grounded
            }
            else
            { 
                VerticalVelocity += Physics.gravity.y * Preset.GravityScale * Time.deltaTime; //simple gravity implementation
            }

            Vector3 fullVelocity = new Vector3(CurrentVelocity.x, VerticalVelocity, CurrentVelocity.z);

            CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && VerticalVelocity > 0.01f)
            {
                VerticalVelocity = 0f; //stop upward velocity if hitting ceiling
            }

            //updating speed
            CurrentSpeed = CurrentVelocity.magnitude;
        }

        void LookUpdate()
        {
            if (LookEnabled == false)
            {
                return;
            }

            // Consume look input once per frame to avoid reusing stale mouse delta.
            Vector2 rawInput = LookInput;
            LookInput = Vector2.zero;

            Vector2 targetInput = new Vector2(rawInput.x * Preset.LookSensitivity.x, rawInput.y * Preset.LookSensitivity.y);

            if (Preset.LookSmoothing > 0f)
            {
                smoothedLookInput = Vector2.SmoothDamp(smoothedLookInput, targetInput, ref lookInputVelocity, Preset.LookSmoothing);
            }
            else
            {
                smoothedLookInput = targetInput;
                lookInputVelocity = Vector2.zero;
            }

            //looking up and down
            CurrentPitch -= smoothedLookInput.y; //invert y input for more natural mouse look feel

            fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f) * CameraRotationOffset;

            //looking left and right
            transform.Rotate(Vector3.up * smoothedLookInput.x);
        }

        void UpdateCameraOffset()
        {
            CameraPositionOffset = Vector3.zero;
            CameraRotationOffset = Quaternion.identity;

            RequestCameraOffset?.Invoke();
        }

        void CameraUpdate()
        {
            float targetFOV = Preset.CameraNormalFOV;

            if (Sprinting)
            {
                float speedRatio = CurrentSpeed / Preset.SprintSpeed;

                targetFOV = Mathf.Lerp(Preset.CameraNormalFOV, Preset.CameraSprintFOV, speedRatio);
            }

            fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, targetFOV, Preset.CameraFOVSmoothing * Time.deltaTime);

            // NOTE: Do NOT override camera world position here.
            // The camera is driven by FPCameraController (it follows the model's head CameraTarget).
            // Removing assignment to localPosition prevents fighting with FPCameraController.
            //
            // fpCamera.transform.localPosition = CurrentCameraPosition + CameraPositionOffset;
        }

        #endregion

        // keep the previous public camera position property for compatibility
        public Vector3 CurrentCameraPosition { get; private set; } = new Vector3(0f, 1.6f, 0f);
    }
}
