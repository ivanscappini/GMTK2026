using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class FPJump : FPActivity
    {
        [SerializeField] bool hasDoubleJumped = false;

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
            hasDoubleJumped = false;
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
            if (Active && Preset.CanDoubleJump && !hasDoubleJumped)
            {
                Jump();
                hasDoubleJumped = true;

                Controller.DoubleJumped?.Invoke();
            }
        }

        void Jump()
        {
            Controller.VerticalSpeed = Mathf.Sqrt(Preset.JumpHeight * -2f * Physics.gravity.y * Preset.GravityScale);
        }

    }
}

