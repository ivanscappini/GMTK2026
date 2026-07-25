using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class FPJump : FPActivity
    {
        [SerializeField] float coyoteTimer = 0f;


        public override bool CanStartActivity()
        {
            return coyoteTimer <= Preset.CoyoteTime && !HasObstaclesAbove();
        }

        public override void OnActivityStarted()
        {
            Jump();
            Controller.Jumped?.Invoke();
        }

        public override void OnActivityStopped()
        {
            base.OnActivityStopped();
        }


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


        bool HasObstaclesAbove()
        {
            Ray ray = new Ray(Controller.CameraTransform.position, Vector3.up);

            float raycastDistance = 0.5f;

            if (Physics.Raycast(ray, raycastDistance, Preset.ObstacleLayerMask))
            {
                return true;
            }

            return false;
        }


        void OnLanded()
        {
            Manager.ForceStop(this);
        }


        void OnTryJump()
        {
            // If another activity (e.g. wall-jump) consumed this jump, clear the flag and skip.
            if (Controller.JumpConsumed)
            {
                Controller.JumpConsumed = false;
                return;
            }

            // Try primary jump (coyote)
            if (coyoteTimer <= Preset.CoyoteTime && !HasObstaclesAbove())
            {
                Jump();

                Controller.Jumped?.Invoke();
                return;
            }

            // otherwise, don't handle double jump here; FPDoubleJump activity will handle it when enabled
        }

        void Jump()
        {
            Controller.VerticalSpeed = Mathf.Sqrt(Preset.JumpHeight * -2f * Physics.gravity.y * Preset.GravityScale);
        }

    }
}

