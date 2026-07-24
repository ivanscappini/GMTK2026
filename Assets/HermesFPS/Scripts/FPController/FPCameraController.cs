using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    public class FPCameraController : MonoBehaviour
    {
        [SerializeField] Transform CameraTarget;

        FPController controller;

        [SerializeField] float smoothTime = 0.1f;

        private Vector3 smoothDampVelocity;


        private void Awake()
        {
            controller = GetComponentInParent<FPController>();

            if (controller == null)
            {
                Debug.LogError("FPController not found in hierarchy.");
            }
        }


        private void Update()
        {
            Vector3 targetPosition = CameraTarget.position + controller.CameraPositionOffset;

            controller.CameraTransform.position = Vector3.SmoothDamp(
                controller.CameraTransform.position,
                targetPosition,
                ref smoothDampVelocity,
                smoothTime);

        }

    }
}

