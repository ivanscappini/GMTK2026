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
            if (Crouching)
            {
                characterController.height = Preset.CrouchHeight;
                characterController.center = Vector3.up * Preset.CrouchHeight * 0.5f;
            }
            else
            {
                characterController.height = Preset.StandHeight;
                characterController.center = Vector3.up * Preset.StandHeight * 0.5f;
            }
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

            float targetFOV = Preset.CameraNormalFOV;

            if (Sprinting)
            {
                float speedRatio = HorizontalSpeed / Preset.SprintSpeed;

                targetFOV = Mathf.Lerp(Preset.CameraNormalFOV, Preset.CameraSprintFOV, speedRatio);
            }


            fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, targetFOV, Preset.CameraFOVSmoothing * Time.deltaTime);


            //fpCamera.transform.localPosition = CurrentCameraPosition + CameraPositionOffset;
        }


        #endregion


    }
}
