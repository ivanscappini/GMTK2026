using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Hermes
{
    [RequireComponent(typeof(FPController))]
    public class Player : MonoBehaviour
    {

        [Header("Components")]
        [SerializeField] FPController FPController;

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
            FPController.SprintInput = value.isPressed;
        }

        void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                FPController.TryJump?.Invoke();
            }
        }

        void OnCrouch(InputValue value)
        {
            if (value.isPressed)
            {
                Activity.TryToggle(FPController.Crouch);
            }
        }

        #endregion

        #region Unity Methods

        private void OnValidate()
        {
            if (FPController == null)
            {
                FPController = GetComponent<FPController>();
            }
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        #endregion
    }
}
