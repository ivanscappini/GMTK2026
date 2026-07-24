using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Hermes {
    public class FPMovement : FPActivity
    {

        Vector2 MoveInput => Controller.MoveInput;

        bool Grounded => Controller.Grounded;

        CharacterController characterController => Controller.CharacterController;

        float MaxSpeed
        {
            get
            {
                if (Activity.IsActive(Controller.Crouch))
                    return Preset.CrouchSpeed;
                if (Activity.IsActive(Controller.Sprint))
                    return Preset.SprintSpeed;
                else return Preset.WalkSpeed;
            }
        }

        private Vector3 velocity = Vector3.zero;

        public Vector3 Velocity => velocity;

        public float VerticalSpeed
        {
            get => Velocity.y;

            set {
                velocity.y = value;
            }
        }

        Vector3 forwardVelocity;
        Vector3 rightVelocity;

        public Vector3 HorizontalVelocity
        {
            get; private set;
        }

        public float HorizontalSpeed
        {
            get; private set;
        }

        #region Activity Override Methods

        protected override void OnActivityStarted()
        {
            // Controller.MovementToggled.Invoke(true);
        }

        protected override void OnActivityStopped()
        {
            // Controller.MovementToggled.Invoke(false);

            VerticalSpeed = 0f;
            HorizontalVelocity = Vector3.zero;
        }

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            base.Awake();

            Controller.Movement = this;
        }

        private void Update()
        {
            if (!Active && Controller.MovementEnabled)
            {
                TryStart(this);
            }
            else if (Active && !Controller.MovementEnabled)
            {
                TryStop(this);
            }

            if (Active)
            {
                MoveUpdate();
            }
        }

        #endregion


        void ComputeForwardVelocity()
        {
            Vector3 motion = transform.forward * MoveInput.y;
            motion.y = 0f;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f)
            {
                forwardVelocity = Vector3.MoveTowards(forwardVelocity, motion * MaxSpeed, Preset.Acceleration * Time.deltaTime);
            }
            if (motion.sqrMagnitude < 0.01f || Vector3.Dot(forwardVelocity, motion) < 0f)
            {
                // decelerating to a stop when no input is given
                forwardVelocity = Vector3.MoveTowards(forwardVelocity, Vector3.zero, Preset.Deceleration * Time.deltaTime);
            }

            Vector3 sidewaysVelocity = transform.right * Vector3.Dot(transform.right, forwardVelocity);

            forwardVelocity -= sidewaysVelocity;
        }

        void ComputeRightVelocity()
        {
            Vector3 motion = transform.forward * MoveInput.y;
            motion.y = 0f;
            motion.Normalize();

            float maxSpeed;
            if (Activity.IsActive(Controller.Crouch))
            {
                maxSpeed = Preset.MaxStrafeCrouchSpeed;
            }
            else if (Activity.IsActive(Controller.Sprint))
            {
                maxSpeed = Preset.MaxStrafeSprintSpeed;
            }
            else
            {
                maxSpeed = Preset.MaxStrafeWalkSpeed;
            }

            if (motion.sqrMagnitude >= 0.01f)
            {
                rightVelocity = Vector3.MoveTowards(rightVelocity, motion * maxSpeed, Preset.StrafeAcceleration * Time.deltaTime);
            }
            if (motion.sqrMagnitude < 0.01f || Vector3.Dot(rightVelocity, motion) < 0f)
            {
                // decelerating to a stop when no input is given
                rightVelocity = Vector3.MoveTowards(rightVelocity, Vector3.zero, Preset.StrafeAcceleration * Time.deltaTime);
            }

            Vector3 fwdVelocity = transform.forward * Vector3.Dot(transform.forward, rightVelocity);

            forwardVelocity -= fwdVelocity;
        }

        void MoveUpdate()
        {
            ComputeForwardVelocity();
            ComputeRightVelocity();

            HorizontalVelocity = forwardVelocity + rightVelocity;

            if (Grounded && VerticalSpeed <= 0.01f)
            {
                VerticalSpeed = -3f; //downward force to keep the player grounded
            }
            else
            {
                VerticalSpeed += Physics.gravity.y * Preset.GravityScale * Time.deltaTime; //simple gravity implementation
            }

            velocity = new Vector3(HorizontalVelocity.x, VerticalSpeed, HorizontalVelocity.z);

            CollisionFlags flags = characterController.Move(Velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && VerticalSpeed > 0.01f)
            {
                VerticalSpeed = 0f; //stop upward velocity if hitting ceiling
            }

            //updating speed
            HorizontalSpeed = HorizontalVelocity.magnitude;
        }



    }
}
