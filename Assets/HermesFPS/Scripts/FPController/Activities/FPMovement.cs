using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
                // Use crouch/walk/sprint speeds
                if (Activity.IsActive(Controller.Crouch))
                    return Preset.CrouchSpeed;
                if (Activity.IsActive(Controller.Sprint))
                    return Preset.SprintSpeed;
                return Preset.WalkSpeed;
            }
        }

        public Vector3 Velocity = Vector3.zero;

        public float VerticalSpeed
        {
            get => Velocity.y;

            set {
                Velocity.y = value;
            }
        }

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
            Controller.MovementToggled.Invoke(true);
        }

        protected override void OnActivityStopped()
        {
            Controller.MovementToggled.Invoke(false);

            VerticalSpeed = 0f;
            HorizontalVelocity = Vector3.zero;
        }

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            base.Awake();

            Controller.Movement = this;
            Debug.Log($"[FPMovement] Awake on '{name}' - Movement assigned to Controller");
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

        void MoveUpdate()
        {
            Debug.Log($"[FPMovement] MoveUpdate - MoveInput={MoveInput} HorizontalSpeed={HorizontalSpeed:F2}");
            Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x; //y input is forward/backward (W/S), x input is left/right (A/D)
            motion.y = 0f;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f)
            {
                HorizontalVelocity = Vector3.Lerp(HorizontalVelocity, motion * MaxSpeed, Preset.Acceleration * Time.deltaTime);
            }
            else
            {
                HorizontalVelocity = Vector3.Lerp(HorizontalVelocity, Vector3.zero, Preset.Acceleration * Time.deltaTime);
            }

            if (Grounded && VerticalSpeed <= 0.01f)
            {
                VerticalSpeed = -3f; //downward force to keep the player grounded
            }
            else
            {
                VerticalSpeed += Physics.gravity.y * Preset.GravityScale * Time.deltaTime; //simple gravity implementation
            }

            Vector3 fullVelocity = new Vector3(HorizontalVelocity.x, VerticalSpeed, HorizontalVelocity.z);

            CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && VerticalSpeed > 0.01f)
            {
                VerticalSpeed = 0f; //stop upward velocity if hitting ceiling
            }

            //updating speed
            HorizontalSpeed = HorizontalVelocity.magnitude;
        }



    }
}
