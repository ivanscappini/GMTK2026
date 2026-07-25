using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Hermes
{
    public class FPFullBodyAnimator : MonoBehaviour
    {
        [Header("Hand Control")]
        public bool LeftHandClimbEnabled = true;
        public bool RightHandClimbEnabled = true;


        private Vector3 LeftHandTargetPos;
        private Vector3 RightHandTargetPos;


        [Header("Rigs")]
        [SerializeField] Rig RightHandRig;
        [SerializeField] Transform RightHandTarget;

        [Space(20)]
        [SerializeField] Rig LeftHandRig;
        [SerializeField] Transform LeftHandTarget;

        [Space(20)]
        [SerializeField] float HandSpacing = 0.3f;
        [SerializeField] float MaxArmReach = 0.8f;
        //[SerializeField] float MaxCamLedgeDistance = 0.9f;

        [Space(20)]
        [SerializeField] Vector3 RightHandTargetRotationOffset = new Vector3(90f, 0f, 0f);
        [SerializeField] Vector3 LeftHandTargetRotationOffset = new Vector3(90f, 0f, 0f);

        [Space(20)]
        [SerializeField] Transform LeftShoulderBone;
        [SerializeField] Transform RightShoulderBone;


        [Header("Components")]
        [SerializeField] FPController controller;
        [SerializeField] Animator animator;

        [Header("Lower Body Alignment")]
        [Tooltip("When enabled, rotate the character root to face wall-forward while wall-running/jumping, keeping the camera world rotation unchanged.")]
        [SerializeField] bool AlignLowerBodyDuringWallActions = true;

        [SerializeField]
        [Min(0f)]
        float LowerBodyAlignSpeed = 10f;

        [Header("Wall Alignment")]
        [Tooltip("If true, use wall normal/tangent from active activities to orient lower body and mirror animations when needed.")]
        [SerializeField] bool UseWallSurfaceAlignment = true;

        // optional blend time to smoothly align orientation/flip flags
        [SerializeField, Min(0f)] float WallAlignmentBlend = 0.12f;

        // runtime
        Quaternion targetBodyRotation = Quaternion.identity;
        float wallAlignTimer = 0f;
        bool lastWallFlip = false;


        private float jumpFinishTime = 0f;
        private float doubleJumpFinishTime = 0f;

        // remember previous slide state to avoid reapplying every frame
        bool lastSlideState = false;

        // warning to avoid spamming console if Slide trigger missing
        bool warnedNoSlideParam = false;
        bool warnedNoSlideActiveParam = false;
        bool warnedNoSlideCancelParam = false;
        bool warnedNoCrouchParam = false;
        bool warnedNoJumpParam = false;
        bool warnedNoDoubleJumpParam = false;
        bool warnedNoFallParam = false;
        bool warnedNoLedgeClimbParam = false;
        bool warnedNoLedgeClimbTypeParam = false;
        bool warnedNoLedgeClimbSpeedParam = false;
        bool warnedNoSpeedParam = false;
        bool warnedNoStrafeParam = false;
        bool warnedNoWallRunParam = false;
        bool warnedNoWallRunActiveParam = false;
        bool warnedNoWallRunCancelParam = false;
        bool warnedNoWallJumpParam = false;
        bool lastWallRunState = false;
        bool warnedNoWallAlignParam = false;
        bool warnedNoWallMirrorParam = false;


        #region Unity Methods

        private void OnValidate()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<FPController>();
            }

            if (animator == null)
            {
                animator = GetComponentInParent<Animator>();
            }
        }


        public float LeftArmReachDistance
        {
            get
            {
                Vector3 shoulderPos = controller.CameraTransform.position - controller.transform.right * HandSpacing / 2f;

                if (LeftShoulderBone != null)
                {
                    shoulderPos = LeftShoulderBone.position;
                }

                return Vector3.Distance(shoulderPos, LeftHandTargetPos);
            }
        }


        public float RightArmReachDistance
        {
            get
            {
                Vector3 shoulderPos = controller.CameraTransform.position + controller.transform.right * HandSpacing / 2f;

                if (RightShoulderBone != null)
                {
                    shoulderPos = RightShoulderBone.position;
                }

                return Vector3.Distance(shoulderPos, RightHandTargetPos);
            }
        }



        private void Start()
        {
            // listening when the ledge climb activity has started.
            controller.ActivityManager.ActivityStarted.AddListener((activity) =>
            {
                if (activity.GetType() == typeof(FPLedgeClimb))
                {
                    OnLedgeClimbStarted();
                }
            });

            // listen for wall jump start to trigger animation and set alignment
            controller.ActivityManager.ActivityStarted.AddListener((activity) =>
            {
                if (activity is FPWallJump wj)
                {
                    // set alignment/mirror based on last wall normal
                    if (UseWallSurfaceAlignment)
                    {
                        Vector3 normal = wj.LastWallNormal;
                        if (normal.sqrMagnitude > 0.0001f)
                        {
                            Vector3 wallForward = Vector3.Cross(normal, Vector3.up).normalized;
                            if (Vector3.Dot(wallForward, controller.CameraTransform.forward) < 0f) wallForward = -wallForward;

                            float angle = Vector3.SignedAngle(controller.transform.forward, wallForward, Vector3.up);
                            SetAnimatorFloat("WallAlignAngle", angle, ref warnedNoWallAlignParam);

                            bool mirror = Vector3.Dot(controller.transform.right, normal) > 0f;
                            SetAnimatorBool("WallMirror", mirror, ref warnedNoWallMirrorParam);
                        }
                    }

                    // trigger WallJump animation
                    if (AnimatorHasParameter("WallJump"))
                    {
                        animator.SetTrigger("WallJump");
                    }
                    else if (!warnedNoWallJumpParam)
                    {
                        Debug.LogWarning("[FPFullBodyAnimator] Animator missing Trigger 'WallJump'.");
                        warnedNoWallJumpParam = true;
                    }
                }
            });

            try
            {
                RightHandRig.weight = 0f;
                LeftHandRig.weight = 0f;
            }
            catch { }

            // basic sanity checks to help debug missing wiring
            if (animator == null)
            {
                Debug.LogWarning("[FPFullBodyAnimator] Animator reference is null. Assign an Animator in inspector.");
            }

            if (controller == null)
            {
                Debug.LogError("[FPFullBodyAnimator] Controller reference is null. Ensure component is on same hierarchy as FPController.");
            }
            else if (controller.ActivityManager == null)
            {
                Debug.LogWarning("[FPFullBodyAnimator] Controller.ActivityManager is null. Activity events won't be received.");
            }


            controller.Jumped.AddListener(() =>
            {
                jumpFinishTime = Time.time + 0.9f;
            });


            controller.DoubleJumped.AddListener(() =>
            {
                doubleJumpFinishTime = Time.time + 0.9f;
            });


            controller.Landed.AddListener(() =>
            {
            });





            controller.MovementToggled += (movementEnabled) =>
            {
                if (!movementEnabled)
                {
                    SetAnimatorBool("Jump", false, ref warnedNoJumpParam);
                    SetAnimatorBool("Fall", false, ref warnedNoFallParam);
                }
            };
        }


        private void Update()
        {
            UpdateAnimator();

            try
            {
                UpdateHandRigs();
            }
            catch { }

            try
            {
                UpdateLowerBodyAlignment();
            }
            catch { }
        }

        #endregion

        void UpdateLowerBodyAlignment()
        {
            if (!AlignLowerBodyDuringWallActions) return;

            bool wallRunActive = controller.ActivityManager != null && controller.ActivityManager.IsActive<FPWallRun>();
            var wallJumpActivity = controller.ActivityManager?.GetActivity<FPWallJump>();
            bool wallJumpActive = wallJumpActivity != null && wallJumpActivity.IsWallJumpActive;

            if (!wallRunActive && !wallJumpActive)
            {
                wallAlignTimer = 0f;
                return;
            }

            // During wall RUN, FPWallRun handles yaw clamping on the controller transform directly.
            // Only rotate the body here for wall JUMP (instantaneous, no ongoing yaw control).
            if (wallJumpActive && !wallRunActive)
            {
                Vector3 horiz = controller.ForwardVelocity;
                horiz = Vector3.ProjectOnPlane(horiz, Vector3.up);
                if (horiz.sqrMagnitude >= 0.001f)
                {
                    Vector3 bodyForward = horiz.normalized;
                    Transform body = controller.transform;
                    Quaternion targetRot = Quaternion.LookRotation(bodyForward, Vector3.up);

                    Transform cam = controller.CameraTransform;
                    Quaternion camWorldRot = cam.rotation;

                    wallAlignTimer = Mathf.Min(wallAlignTimer + Time.deltaTime, WallAlignmentBlend);
                    float t = WallAlignmentBlend > 0f ? Mathf.SmoothStep(0f, 1f, wallAlignTimer / WallAlignmentBlend) : 1f;
                    body.rotation = Quaternion.Slerp(body.rotation, targetRot, Mathf.Clamp01(t * LowerBodyAlignSpeed * Time.deltaTime));

                    cam.rotation = camWorldRot;
                }
            }

            // Set animator params for alignment angle and mirror during wall run
            if (UseWallSurfaceAlignment && wallRunActive)
            {
                var wract = controller.ActivityManager?.GetActivity<FPWallRun>();
                if (wract != null && wract.WallNormal.sqrMagnitude > 0.0001f)
                {
                    Vector3 normal = wract.WallNormal;
                    Vector3 wf = Vector3.Cross(normal, Vector3.up).normalized;
                    if (Vector3.Dot(wf, controller.CameraTransform.forward) < 0f) wf = -wf;

                    float alignAngle = Vector3.SignedAngle(controller.transform.forward, wf, Vector3.up);
                    SetAnimatorFloat("WallAlignAngle", alignAngle, ref warnedNoWallAlignParam);

                    bool mirror = Vector3.Dot(controller.transform.right, normal) > 0f;
                    SetAnimatorBool("WallMirror", mirror, ref warnedNoWallMirrorParam);
                }
            }

            // Set alignment params for wall jump based on stored normal
            if (UseWallSurfaceAlignment && wallJumpActive && wallJumpActivity != null)
            {
                Vector3 normal = wallJumpActivity.LastWallNormal;
                if (normal.sqrMagnitude > 0.0001f)
                {
                    Vector3 wf = Vector3.Cross(normal, Vector3.up).normalized;
                    if (Vector3.Dot(wf, controller.CameraTransform.forward) < 0f) wf = -wf;

                    float alignAngle = Vector3.SignedAngle(controller.transform.forward, wf, Vector3.up);
                    SetAnimatorFloat("WallAlignAngle", alignAngle, ref warnedNoWallAlignParam);

                    bool mirror = Vector3.Dot(controller.transform.right, normal) > 0f;
                    SetAnimatorBool("WallMirror", mirror, ref warnedNoWallMirrorParam);
                }
            }
        }


        void OnLedgeClimbStarted()
        {
            var ledgeClimb = controller.ActivityManager.GetActivity<FPLedgeClimb>();

            Vector3 ledgeTop = ledgeClimb.LedgeTop + Vector3.up * 0.1f;

            ledgeTop -= controller.transform.forward * ledgeClimb.WallCheckDistance / 2f;

            LeftHandTargetPos = ledgeTop - transform.right * HandSpacing / 2f;
            RightHandTargetPos = ledgeTop + transform.right * HandSpacing / 2f;

            float speedMultiplier = 1f;

            if (ledgeClimb.LedgeClimbType == ELedgeClimbType.Climb)
            {
                speedMultiplier = controller.Preset.AnimationLengthClimb / ledgeClimb.ClimbDuration;
            }
            else if (ledgeClimb.LedgeClimbType == ELedgeClimbType.Vault)
            {
                speedMultiplier = controller.Preset.AnimationLengthVault / ledgeClimb.ClimbDuration;
            }
            else
            {
                speedMultiplier = controller.Preset.AnimationLengthStep / ledgeClimb.ClimbDuration;
            }

            SetAnimatorFloat("LedgeClimbType", (float)ledgeClimb.LedgeClimbType, ref warnedNoLedgeClimbTypeParam);
            SetAnimatorFloat("LedgeClimbSpeed", speedMultiplier, ref warnedNoLedgeClimbSpeedParam);
        }


        #region Rigging Methods


        void LerpLeftHandRigWeight(float weight)
        {
            LeftHandRig.weight = Mathf.Lerp(LeftHandRig.weight, weight, 15f * Time.deltaTime);
        }

        void LerpRightHandRigWeight(float weight)
        {
            RightHandRig.weight = Mathf.Lerp(RightHandRig.weight, weight, 15f * Time.deltaTime);
        }


        void LerpHandRigWeights(float targetWeight)
        {
            LerpLeftHandRigWeight(targetWeight);
            LerpRightHandRigWeight(targetWeight);
        }


        void UpdateLeftHand()
        {
            //Debug.Log($"Left hand reach: {LeftArmReachDistance}");

            if (LeftArmReachDistance > MaxArmReach || !LeftHandClimbEnabled)
            {
                LerpLeftHandRigWeight(0f);
                return;
            }


            LerpLeftHandRigWeight(1f);
        }


        void UpdateRightHand()
        {
            var ledgeClimb = controller.ActivityManager.GetActivity<FPLedgeClimb>();

            bool vaulting = ledgeClimb.LedgeClimbType == ELedgeClimbType.Vault;

            if (RightArmReachDistance > MaxArmReach || !RightHandClimbEnabled || vaulting)
            {
                LerpRightHandRigWeight(0f);
                return;
            }


            LerpRightHandRigWeight(1f);
        }


        void UpdateHandRigs()
        {
            if (!controller.LedgeClimbing)
            {
                LerpHandRigWeights(0f);

                return;
            }

            LeftHandTarget.position = LeftHandTargetPos;
            RightHandTarget.position = RightHandTargetPos;


            UpdateLeftHand();
            UpdateRightHand();

            LeftHandTarget.localRotation = Quaternion.Euler(LeftHandTargetRotationOffset);
            RightHandTarget.localRotation = Quaternion.Euler(RightHandTargetRotationOffset);
        }

        #endregion


        #region Animator Methods

        void SetAnimatorBool(string name, bool value, ref bool warnedFlag)
        {
            if (animator == null) return;
            if (AnimatorHasParameter(name))
            {
                animator.SetBool(name, value);
            }
            else if (!warnedFlag)
            {
                Debug.LogWarning($"[FPFullBodyAnimator] Animator missing Bool '{name}'.");
                warnedFlag = true;
            }
        }

        void SetAnimatorFloat(string name, float value, ref bool warnedFlag)
        {
            if (animator == null) return;
            if (AnimatorHasParameter(name))
            {
                animator.SetFloat(name, value);
            }
            else if (!warnedFlag)
            {
                Debug.LogWarning($"[FPFullBodyAnimator] Animator missing Float '{name}'.");
                warnedFlag = true;
            }
        }


        bool IsFalling()
        {
            if (controller.GroundDistance <= 2f || IsJumping() || IsDoubleJumping() || IsLedgeClimbing())
            {
                return false;
            }

            return controller.VerticalSpeed <= -0.1f;
        }

        bool IsJumping()
        {
            if (IsLedgeClimbing() || IsDoubleJumping())
            {
                return false;
            }


            if (controller.Grounded && controller.VerticalSpeed <= 0.1f)
            {
                return false;
            }

            return Time.time <= jumpFinishTime;
        }


        bool IsDoubleJumping()
        {
            if (IsLedgeClimbing())
            {
                return false;
            }

            if (controller.Grounded && controller.VerticalSpeed <= 0.1f)
            {
                return false;
            }

            return Time.time <= doubleJumpFinishTime;
        }

        bool IsLedgeClimbing()
        {
            return controller.LedgeClimbing;
        }

        bool IsSliding()
        {
            return controller.ActivityManager.IsActive<FPSlide>();
        }

        bool AnimatorHasParameter(string paramName)
        {
            if (animator == null) return false;
            foreach (var p in animator.parameters)
            {
                if (p.name == paramName) return true;
            }
            return false;
        }

        void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            Vector3 velocity = controller.HorizontalVelocity;

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float strafeSpeed = Vector3.Dot(velocity, transform.right);
            SetAnimatorFloat("Speed", forwardSpeed, ref warnedNoSpeedParam);
            SetAnimatorFloat("StrafeSpeed", strafeSpeed, ref warnedNoStrafeParam);

            // Only query activity state if the ActivityManager has the activity enabled to avoid warnings when activities are disabled
            bool crouchEnabled = controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPCrouch));
            SetAnimatorBool("Crouch", crouchEnabled && controller.Crouching, ref warnedNoCrouchParam);


            // Only set Jump/DoubleJump/Fall if the corresponding activities are enabled to avoid warnings
            bool jumpEnabled = controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPJump));
            bool doubleJumpEnabled = controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPDoubleJump));

            SetAnimatorBool("Jump", jumpEnabled && IsJumping(), ref warnedNoJumpParam);
            SetAnimatorBool("DoubleJump", doubleJumpEnabled && IsDoubleJumping(), ref warnedNoDoubleJumpParam);
            SetAnimatorBool("Fall", (jumpEnabled || doubleJumpEnabled || (controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPLedgeClimb)))) && IsFalling(), ref warnedNoFallParam);

            bool ledgeEnabled = controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPLedgeClimb));
            SetAnimatorBool("LedgeClimb", ledgeEnabled && IsLedgeClimbing(), ref warnedNoLedgeClimbParam);

            // Only consider slide state if the FPSlide activity is enabled in the ActivityManager.
            bool sliding = false;
            if (controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPSlide)))
            {
                sliding = IsSliding();
            }

            // Wall run state: similar rising-edge trigger and active flag as slide
            bool wallRunActive = false;
            if (controller.ActivityManager != null && controller.ActivityManager.IsEnabled(typeof(FPWallRun)))
            {
                wallRunActive = controller.ActivityManager.IsActive<FPWallRun>();
            }

            if (wallRunActive && !lastWallRunState)
            {
                // entering wall run
                if (AnimatorHasParameter("WallRun"))
                {
                    animator.SetTrigger("WallRun");
                }
                else if (!warnedNoWallRunParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Trigger 'WallRun'.");
                    warnedNoWallRunParam = true;
                }

                if (AnimatorHasParameter("WallRunning"))
                {
                    animator.SetBool("WallRunning", true);
                }
                else if (!warnedNoWallRunActiveParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Bool 'WallRunning'.");
                    warnedNoWallRunActiveParam = true;
                }
            }

            if (!wallRunActive && lastWallRunState)
            {
                // exiting wall run
                if (AnimatorHasParameter("WallRunning"))
                {
                    animator.SetBool("WallRunning", false);
                }

                if (AnimatorHasParameter("WallRunCancel"))
                {
                    animator.SetTrigger("WallRunCancel");
                }
                else if (!warnedNoWallRunCancelParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Trigger 'WallRunCancel'. Add it to interrupt wall-run.");
                    warnedNoWallRunCancelParam = true;
                }
            }

            lastWallRunState = wallRunActive;

            // Use a trigger when slide becomes active (rising edge) so one-shot animations don't re-enter.
            if (sliding && !lastSlideState)
            {
                if (AnimatorHasParameter("Slide"))
                {
                    animator.SetTrigger("Slide");
                }
                else if (!warnedNoSlideParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Trigger 'Slide'.");
                    warnedNoSlideParam = true;
                }

                // set persistent flag telling Animator we're in slide state (prevents other transitions)
                if (AnimatorHasParameter("SlideActive"))
                {
                    animator.SetBool("SlideActive", true);
                }
                else if (!warnedNoSlideActiveParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Bool 'SlideActive'.");
                    warnedNoSlideActiveParam = true;
                }
            }

            // When slide ends, clear persistent flag so Animator can resume normal transitions
            if (!sliding && lastSlideState)
            {
                if (AnimatorHasParameter("SlideActive"))
                {
                    animator.SetBool("SlideActive", false);
                }

                // signal cancellation to Animator so one-shot slide can be interrupted
                if (AnimatorHasParameter("SlideCancel"))
                {
                    animator.SetTrigger("SlideCancel");
                }
                else if (!warnedNoSlideCancelParam)
                {
                    Debug.LogWarning("[FPFullBodyAnimator] Animator missing Trigger 'SlideCancel'. Add it to interrupt slide.");
                    warnedNoSlideCancelParam = true;
                }
            }

            lastSlideState = sliding;
        }

        #endregion



    }
}