using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class  FPFullBodyAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] FPController controller;
        [SerializeField] Animator animator;

        private float jumpFinish = 0f;
        private float doubleJumpFinish = 0f;

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

        private void Start()
        {
            controller.Jumped.AddListener(() =>
            {
                jumpFinish = Time.time + 0.9f;
            });

            controller.DoubleJumped.AddListener(() =>
            {
                doubleJumpFinish = Time.time + 0.9f;
            });

            controller.Landed.AddListener(() =>
            {

            });
            
        }

        private void Update()
        {
            UpdateAnimator();
        }

        #endregion

        #region Animator Methods
        bool IsFalling()
        {
            if (controller.GroundDistance <= 2f || IsJumping() || IsDoubleJumping())
            {
                return false;
            }

            return controller.VerticalSpeed <= -0.1f;
        }

        bool IsJumping()
        {
            if (IsDoubleJumping())
            {
                return false;
            }

            if (controller.Grounded && controller.VerticalSpeed <= 0.1f)
            {
                return false;
            }

            return Time.time <= jumpFinish;
        }
        bool IsDoubleJumping()
        {
            if (controller.Grounded && controller.VerticalSpeed <= 0.1f)
            {
                return false;
            }

            return Time.time <= doubleJumpFinish;
        }

        private void UpdateAnimator()
        {
            Vector3 velocity = controller.HorizontalVelocity;

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float strafeSpeed = Vector3.Dot(velocity, transform.right);

            animator.SetFloat("Speed", forwardSpeed);
            animator.SetFloat("StrafeSpeed", strafeSpeed);

            animator.SetBool("Jump", IsJumping());
            animator.SetBool("DoubleJump", IsDoubleJumping());
            animator.SetBool("Fall", IsFalling());
        }

        #endregion

    }
}
