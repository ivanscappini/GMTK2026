using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class  FPFullBodyAnimator : MonoBehaviour
    {
        [SerializeField] FPController controller;
        [SerializeField] Animator animator;

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
                animator.SetBool("Jump", true);
            });

            controller.Landed.AddListener(() =>
            {
                animator.SetBool("Jump", false);
            });

            controller.DoubleJumped.AddListener(() =>
            {
                animator.SetTrigger("DoubleJump");
            });
        }

        private void Update()
        {
            Vector3 velocity = controller.CurrentVelocity;

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float strafeSpeed = Vector3.Dot(velocity, transform.right);

            animator.SetFloat("Speed", forwardSpeed);
            animator.SetFloat("StrafeSpeed", strafeSpeed);

            animator.SetBool("Fall", controller.VerticalVelocity <= -0.1f && !controller.Grounded);
        }
    }
}
