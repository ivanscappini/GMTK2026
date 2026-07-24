using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;

namespace Hermes
{
    [RequireComponent(typeof(CharacterController))]

    public partial class FPController : MonoBehaviour
    {
        public FPControllerPreset Preset;

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

        [Header("Control Parameters")]
        public bool LookEnabled = true;
        public bool MovementEnabled = true;

        public float VerticalSpeed
        {
            get => Movement.VerticalSpeed;
            set { Movement.VerticalSpeed = value; }
        }
        public Vector3 HorizontalVelocity => Movement.HorizontalVelocity;
        public float HorizontalSpeed => Movement.HorizontalSpeed;

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
        public FPMovement Movement;
        public FPSprint Sprint;
        public FPCrouch Crouch;


        [Header("Events")]
        public UnityEvent Landed;
        public UnityEvent Jumped;
        public UnityEvent DoubleJumped;

        public UnityAction TryJump;

        public UnityAction<bool> MovementToggled;
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

            LookUpdate();
            CameraUpdate();

            // camera height / character collider: crouch removed, always standing height
            Vector3 targetCameraPosition = Vector3.up * 1.6f; // default camera height

            if (Activity.IsActive(Crouch))
            {
                characterController.height = Preset.CrouchHeight;
                characterController.center = Vector3.up * Preset.CrouchHeight * 0.5f;
            }
            else
            {
                characterController.height = Preset.StandHeight;
                characterController.center = Vector3.up *  Preset.StandHeight * 0.5f; // keep a consistent hitbox
            }

            CurrentCameraPosition = Vector3.Lerp(CurrentCameraPosition, targetCameraPosition, 7f * Time.deltaTime);

            UpdateGroundCheck();
        }

        # endregion

        #region Controller Methods

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
                float speedRatio = HorizontalSpeed / Preset.SprintSpeed;

                targetFOV = Mathf.Lerp(Preset.CameraNormalFOV, Preset.CameraSprintFOV, speedRatio);
            }

            fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, targetFOV, Preset.CameraFOVSmoothing * Time.deltaTime);
        }

        #endregion

        // keep the previous public camera position property for compatibility
        public Vector3 CurrentCameraPosition { get; private set; } = new Vector3(0f, 1.6f, 0f);
    }
}
