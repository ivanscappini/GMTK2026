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


        private float jumpFinishTime = 0f;
        private float doubleJumpFinishTime = 0f;


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

            try
            {
                RightHandRig.weight = 0f;
                LeftHandRig.weight = 0f;
            }
            catch { }


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
                    animator.SetBool("Jump", false);
                    animator.SetBool("Fall", false);
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
        }

        #endregion


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

            animator.SetFloat("LedgeClimbType", (float)ledgeClimb.LedgeClimbType);
            animator.SetFloat("LedgeClimbSpeed", speedMultiplier);
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

        void UpdateAnimator()
        {
            Vector3 velocity = controller.HorizontalVelocity;

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float strafeSpeed = Vector3.Dot(velocity, transform.right);

            animator.SetFloat("Speed", forwardSpeed);
            animator.SetFloat("StrafeSpeed", strafeSpeed);

            animator.SetBool("Crouch", controller.Crouching);


            animator.SetBool("Jump", IsJumping());
            animator.SetBool("DoubleJump", IsDoubleJumping());
            animator.SetBool("Fall", IsFalling());
            animator.SetBool("LedgeClimb", IsLedgeClimbing());
        }

        #endregion



    }
}