using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hermes
{
    [RequireComponent(typeof(FPController))]
    public class Player : MonoBehaviour
    {
        [SerializeField] bool AlwaysRun = false;


        [Header("Components")]
        [SerializeField] FPController FPController;
        [SerializeField, Min(0f)] float JumpTapThreshold = 0.12f;

        float lastJumpPressTime = -999f;

        // runtime hook to the Input System action for crouch (if PlayerInput is present)
        InputAction crouchAction;


        public void SetAlwaysRun(bool alwaysRun)
        {
            AlwaysRun = alwaysRun;

            // set sprint input according to new state so activities see correct intent immediately
            FPController.SprintInput = alwaysRun;
        }


        #region Input Handling

        void OnEnable()
        {
            // If a PlayerInput component exists, subscribe to the Crouch action performed/canceled events.
            var pi = GetComponent<PlayerInput>();
            if (pi != null && pi.actions != null)
            {
                // InputActionAsset doesn't have TryGetAction in all versions; use FindAction which is available.
                var action = pi.actions.FindAction("Crouch", true);
                if (action != null)
                {
                    crouchAction = action;
                    crouchAction.performed += OnCrouchPerformed;
                    crouchAction.canceled += OnCrouchCanceled;
                }
                else
                {
                    Debug.LogWarning("[Player] PlayerInput found but action 'Crouch' not present in asset.");
                }
            }
        }

        void OnDisable()
        {
            if (crouchAction != null)
            {
                crouchAction.performed -= OnCrouchPerformed;
                crouchAction.canceled -= OnCrouchCanceled;
                crouchAction = null;
            }
        }


        void OnMove(InputValue value)
        {
            FPController.MoveInput = value.Get<Vector2>();
        }

        void OnLook(InputValue value)
        {
            FPController.LookInput = value.Get<Vector2>();
        }

        void OnSprint(InputValue value)
        {
            if (AlwaysRun)
            {
                return;
            }

            FPController.SprintInput = value.isPressed;

            if (!FPController.MovementEnabled)
            {
                FPController.SprintInput = false;
            }
        }


        ActivityManager ActivityManager => FPController.ActivityManager;


        void OnJump(InputValue value)
        {
            if (!FPController.MovementEnabled)
            {
                return;
            }
            // Press or release handling: track press time and call TryJump on release if it was a tap.
            if (value.isPressed)
            {
                // store press time
                lastJumpPressTime = Time.time;

                FPController.JumpInput = true;

                // stand when trying to jump while crouched
                if (FPController.Crouching)
                {
                    ActivityManager.TryStop<FPCrouch>();
                    return;
                }

                // Try primary jump immediately if possible
                if (!ActivityManager.IsActive<FPJump>())
                {
                    bool started = ActivityManager.TryStart<FPJump>();
                    if (started)
                    {
                        return;
                    }
                }

                // Airborne and no ground jump: fire TryJump on press for double jump.
                // Skip if near a wall — FPWallRun handles the hold/tap detection there.
                if (!FPController.Grounded && !IsNearRunnableWall())
                {
                    FPController.LastTryJumpFrame = Time.frameCount;
                    FPController.TryJump?.Invoke();
                }
            }
            else
            {
                // release: clear hold state
                FPController.JumpInput = false;

                float held = Time.time - lastJumpPressTime;

                bool shouldTryJump = false;

                if (held <= JumpTapThreshold)
                {
                    shouldTryJump = true;
                }
                else if (!FPController.Grounded && IsNearRunnableWall() && !FPController.WallRunning)
                {
                    // Wall run isn't active — either disabled or didn't attach.
                    // Use a generous tap threshold so wall jump can still trigger.
                    var wallRun = ActivityManager.GetActivity<FPWallRun>();
                    bool wallRunAvailable = wallRun != null && wallRun.enabled && ActivityManager.IsEnabled<FPWallRun>();
                    if (!wallRunAvailable && held <= 0.4f)
                        shouldTryJump = true;
                }

                if (shouldTryJump && Time.frameCount != FPController.LastTryJumpFrame)
                {
                    FPController.LastTryJumpFrame = Time.frameCount;
                    FPController.TryJump?.Invoke();
                }
            }
        }

        bool IsNearRunnableWall()
        {
            Vector3 origin = FPController.transform.position + Vector3.up * 1f;
            Transform t = FPController.transform;
            float sphereRadius = 0.15f;
            float dist = FPController.Preset.WallCheckDistance;
            string tag = FPController.Preset.WallRunAllowedTag;

            Vector3[] dirs = new Vector3[]
            {
                t.right, -t.right,
                (t.forward + t.right).normalized,
                (t.forward - t.right).normalized,
                (-t.forward + t.right).normalized,
                (-t.forward - t.right).normalized,
                t.forward, -t.forward,
            };

            foreach (var dir in dirs)
            {
                if (Physics.SphereCast(origin, sphereRadius, dir, out var hit, dist - sphereRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (IsRunnableWallSurface(hit, t, tag))
                        return true;
                }

                var rayHits = Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
                foreach (var rh in rayHits)
                {
                    if (IsRunnableWallSurface(rh, t, tag))
                        return true;
                }
            }

            return false;
        }

        static bool IsRunnableWallSurface(RaycastHit h, Transform self, string requiredTag)
        {
            if (h.collider == null) return false;
            if (h.collider.transform.IsChildOf(self) || h.collider.gameObject == self.gameObject)
                return false;
            if (h.distance < 0.01f) return false;
            float upDot = Mathf.Abs(Vector3.Dot(h.normal.normalized, Vector3.up));
            if (upDot > 0.3f) return false;
            if (!string.IsNullOrEmpty(requiredTag) && !h.collider.gameObject.CompareTag(requiredTag))
                return false;
            return true;
        }

        // kept for compatibility with message-based input; if we're using the PlayerInput subscription
        // we skip handling here to avoid duplicate processing.
        void OnCrouch(InputValue value)
        {
            if (crouchAction != null)
            {
                // using explicit action callbacks; ignore message-based invocation to prevent duplicates
                return;
            }

            HandleCrouch(value.isPressed);
        }

        // InputAction callbacks (if PlayerInput exists)
        void OnCrouchPerformed(InputAction.CallbackContext ctx)
        {
            HandleCrouch(ctx.ReadValueAsButton());
        }

        void OnCrouchCanceled(InputAction.CallbackContext ctx)
        {
            HandleCrouch(false);
        }

        // unified handler for press/release logic and debug logging
        void HandleCrouch(bool pressed)
        {
            // Ignore input entirely if movement disabled
            if (!FPController.MovementEnabled)
            {
                return;
            }

            // store current raw crouch input on controller so activities can read hold state reliably
            FPController.CrouchInput = pressed;

            // (debug logs removed)

            // detect sprinting (either input or active)
            bool isSprinting = FPController.SprintInput || FPController.Sprinting;

            // Press: start slide if sprinting, otherwise toggle crouch
            if (pressed)
            {
                if (isSprinting)
                {
                    if (FPController.Grounded)
                    {
                        // start slide while held
                        if (!ActivityManager.TryStart<FPSlide>())
                        {
                            // fallback to crouch if slide couldn't start
                            ActivityManager.TryToggle<FPCrouch>();
                        }
                    }
                    else
                    {
                        // queue slide to start on landing
                        var slide = ActivityManager.GetActivity<FPSlide>();
                        if (slide != null)
                        {
                            slide.QueueSlide();
                        }
                        else
                        {
                            Debug.LogWarning("[Player] FPSlide activity not found in ActivityManager (was it added to the inspector list?).");
                        }
                    }

                    return;
                }

                // default crouch toggle when not sprinting
                ActivityManager.TryToggle<FPCrouch>();
                return;
            }

            // Release: stop slide if active, cancel any queued slide
            if (!pressed)
            {
                if (ActivityManager.IsActive<FPSlide>())
                {
                    ActivityManager.TryStop<FPSlide>();
                }
                else
                {
                    var slide = ActivityManager.GetActivity<FPSlide>();
                    if (slide != null)
                    {
                        slide.CancelQueued();
                    }
                }
            }
        }

        #endregion


        #region Unity Methods

        void OnValidate()
        {
            if (FPController == null) FPController = GetComponent<FPController>();
        }

        void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            //Time.timeScale = 0.1f;


            if (AlwaysRun)
            {
                FPController.SprintInput = true;
            }
        }

        #endregion
    }
}

