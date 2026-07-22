using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class FPJump : FPControllerModule
    {
        [SerializeField] bool hasDoubleJumped = false;

        [SerializeField] float coyoteTimer = 0f;

        private void Start()
        {
            Controller.TryJump += OnTryJump;

            Controller.Landed.AddListener(OnLanded);
        }

        private void Update()
        {
            if (Controller.Grounded)
            {
                coyoteTimer = 0f;
            }
            else
            {
                coyoteTimer += Time.deltaTime;
            }
        }

        void OnLanded()
        {
            hasDoubleJumped = false;
        }

        void OnTryJump()
        {
            // jumping
            if(coyoteTimer <= Preset.CoyoteTime)
            {
                Jump();

                Controller.Jumped?.Invoke();

                return;
            }

            // double jumping
            if (Preset.CanDoubleJump == false)
            {
                return;
            }

            if (!hasDoubleJumped)
            {
                Jump();
                hasDoubleJumped = true;

                Controller.Jumped?.Invoke();
            }

        }

        void Jump()
        {
            Controller.VerticalVelocity = Mathf.Sqrt(Preset.JumpHeight * -2f * (Physics.gravity.y * Preset.GravityScale)); //v = sqrt(h * -2 * g)
        }

    }
}
