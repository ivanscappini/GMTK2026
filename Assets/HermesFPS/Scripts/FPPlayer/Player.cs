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



        public void SetAlwaysRun(bool alwaysRun)
        {
            AlwaysRun = alwaysRun;

            FPController.SprintInput = false;
        }


        #region Input Handling

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

            if (value.isPressed)
            {
                // stand when trying to jump while crouched
                if (FPController.Crouching)
                {
                    ActivityManager.TryStop<FPCrouch>();
                    return;
                }

                // jumping
                if (!ActivityManager.IsActive<FPJump>())
                {
                    ActivityManager.TryStart<FPJump>();
                    return;
                }

                FPController.TryJump?.Invoke();
            }
        }

        void OnCrouch(InputValue value)
        {
            if (value.isPressed && FPController.MovementEnabled)
            {
                ActivityManager.TryToggle<FPCrouch>();
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

