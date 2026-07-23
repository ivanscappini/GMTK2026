using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Events;
using System;

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
                if (Activity.IsActive(Crouch))
                    return Preset.CrouchSpeed;
                if (Activity.IsActive(Sprint))
                    return Preset.SprintSpeed;
                return Preset.WalkSpeed;
            }
        }

        public bool Sprinting
        {
            get
            {
                return Activity.IsActive(Sprint);
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
        public Vector3 CurrentCameraPosition { get; private set; } = new Vector3(0f, 1.6f, 0f);

        [Space(10)]
        public Vector3 CameraPositionOffset = Vector3.zero;
        public Quaternion CameraRotationOffset = Quaternion.identity;

        [Header("Physics Parameters")]

        public float VerticalVelocity = 0f;

        public Vector3 CurrentVelocity { get; private set; }
        public float CurrentSpeed { get; private set; }

        private bool wasGrounded = false;

        public bool Grounded => characterController.isGrounded;

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

        [Header("Activities")]
        public FPSprint Sprint;
        public FPCrouch Crouch;

        [Header("Events")]
        public UnityEvent Landed;
        public UnityEvent Jumped;
        public UnityEvent DoubleJumped;

        public event Action OnMoving;
        public event Action OnStationary;

        public UnityAction TryJump;
        public UnityAction RequestCameraOffset;

        [Header("Looking Parameters")]
        [Range(0.01f, 1f)]
        public Vector2 LookSensitivity = new Vector2(0.1f, 0.1f);

        public static FPController Instance { get; private set; }

        #region Unity Methods
        void OnValidate()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Update()
        {
            UpdateCameraOffset();

            MoveUpdate();
            LookUpdate();
            CameraUpdate();

            //check if player is crouching and adjust camera height

            Vector3 targetCameraPosition = Vector3.up * 1.6f; //default camera height

            if (Activity.IsActive(Crouch))
            {
                targetCameraPosition = Vector3.up * 0.9f; //crouch camera height

                characterController.height = 1f;
                characterController.center = Vector3.up * 0.5f; //adjust character hitbox when crouching
            }
            else
            {
                characterController.height = 2f;
                characterController.center = Vector3.up * 1f; //reset character hitbox when not crouching
            }

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
            Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x; //y input is forward/backward (W/S), x input is left/right (A/D)
            motion.y = 0f;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f)
            {
                CurrentVelocity = Vector3.Lerp(CurrentVelocity, motion * MaxSpeed, Preset.Acceleration * Time.deltaTime);
                OnMoving?.Invoke();
            }
            else
            {
                CurrentVelocity = Vector3.Lerp(CurrentVelocity, Vector3.zero, Preset.Acceleration * Time.deltaTime);
                OnStationary?.Invoke();
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
            Vector2 input = new Vector2(LookInput.x * Preset.LookSensitivity.x, LookInput.y * Preset.LookSensitivity.y);

            //looking up and down
            CurrentPitch -= input.y; //invert y input for more natural mouse look feel

            fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f) * CameraRotationOffset;

            //looking left and right
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
                float speedRatio = CurrentSpeed / Preset.SprintSpeed;

                targetFOV = Mathf.Lerp(Preset.CameraNormalFOV, Preset.CameraSprintFOV, speedRatio);
            }

            fpCamera.Lens.FieldOfView = Mathf.Lerp(fpCamera.Lens.FieldOfView, targetFOV, Preset.CameraFOVSmoothing * Time.deltaTime);

            fpCamera.transform.localPosition = CurrentCameraPosition + CameraPositionOffset;
        }

        #endregion

    }
}
