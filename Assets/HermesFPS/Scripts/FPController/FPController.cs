using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;

namespace Hermes
{
    [RequireComponent(typeof(CharacterController), typeof(ActivityManager))]
    public partial class FPController : MonoBehaviour
    {
        public FPControllerPreset Preset;


        #region Accessors

        public bool Crouching => ActivityManager.IsActive<FPCrouch>();

        public bool Sprinting => ActivityManager.IsActive<FPSprint>();

        public bool LedgeClimbing => ActivityManager.IsActive<FPLedgeClimb>();

        public bool Sliding => ActivityManager.IsActive<FPSlide>();

        public bool WallRunning => ActivityManager.IsActive<FPWallRun>();

        #endregion




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


        public Vector3 ForwardVelocity
        {
            get => Movement.ForwardVelocity;
            set => Movement.SetForwardVelocity(value);
        }


        [Header("Input")]
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public bool SprintInput;
        // added: store current crouch input state so activities can query hold/release reliably
        public bool CrouchInput;
        // added: store current jump input hold state
        public bool JumpInput;


        [Header("Components")]
        [SerializeField] CinemachineCamera fpCamera;
        [SerializeField] CharacterController characterController;

        public CharacterController CharacterController => characterController;

        public CinemachineCamera Camera => fpCamera;

        public Transform CameraTransform => fpCamera.transform;

        public ActivityManager ActivityManager;

        public FPMovement Movement;

        [Header("Events")]
        public UnityEvent Landed;
        public UnityEvent Jumped;
        public UnityEvent DoubleJumped;


        public UnityAction TryJump;
        // When true, the next TryJump should be consumed by a special jump (e.g. wall-jump)
        public bool JumpConsumed { get; set; } = false;

        // Prevents TryJump from being invoked more than once per frame
        public int LastTryJumpFrame { get; set; } = -1;

        // Invoked when movement was enabled or disabled
        public UnityAction<bool> MovementToggled;
        public UnityAction RequestCameraOffset;


        #region Unity Methods

        private void OnDrawGizmos()
        {
            TryDrawGroundSensorGizmos();
        }


        void OnValidate()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (ActivityManager == null)
            {
                ActivityManager = GetComponent<ActivityManager>();
            }
        }


        void Update()
        {
            UpdateCameraOffset();

            LookUpdate();
            CameraUpdate();

            UpdateCharacterHeight();
            UpdateGroundCheck();
        }

        #endregion



        #region Controller Methods

        void UpdateCharacterHeight()
        {
            float targetHeight;
            if (Sliding)
                targetHeight = Preset.SlideHeight;
            else if (Crouching)
                targetHeight = Preset.CrouchHeight;
            else
                targetHeight = Preset.StandHeight;

            if (targetHeight > characterController.height + 0.01f)
            {
                float growAmount = targetHeight - characterController.height;
                Vector3 castOrigin = transform.position + Vector3.up * characterController.height;

                if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, growAmount + 0.05f, Preset.ObstacleLayerMask))
                {
                    float clearance = Mathf.Max(0f, hit.distance - 0.05f);
                    targetHeight = Mathf.Max(characterController.height + clearance, Preset.SlideHeight);
                }

                characterController.height = Mathf.MoveTowards(characterController.height, targetHeight, 10f * Time.deltaTime);
            }
            else
            {
                characterController.height = targetHeight;
            }

            characterController.center = Vector3.up * characterController.height * 0.5f;
        }

		
		void LookUpdate()
        {
            if (LookEnabled == false)
            {
                return;
            }

            Vector2 input = new Vector2(LookInput.x * Preset.LookSensitivity.x, LookInput.y * Preset.LookSensitivity.y);

            // looking up and down
            CurrentPitch -= input.y;

            fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f) * CameraRotationOffset;


            // looking left and right
            transform.Rotate(Vector3.up * input.x);
        }


        void UpdateCameraOffset()
        {
            CameraPositionOffset = Vector3.zero;
            CameraRotationOffset = Quaternion.identity;

            RequestCameraOffset?.Invoke();
        }


        void CameraUpdate()
        {
            float speedRatio = Mathf.Clamp01(
                (HorizontalSpeed - Preset.WalkSpeed) / (Preset.SprintSpeed - Preset.WalkSpeed));
            float targetFOV = Mathf.Lerp(Preset.CameraNormalFOV, Preset.CameraSprintFOV, speedRatio);

            fpCamera.Lens.FieldOfView = Mathf.Lerp(
                fpCamera.Lens.FieldOfView, targetFOV, Preset.CameraFOVSmoothing * Time.deltaTime);
        }


        #endregion


    }
}
