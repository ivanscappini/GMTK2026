using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Hermes
{
    // blocked by Sprint, Ledge Climb

    public class FPCrouch : FPActivity
    {

        public override bool CanStartActivity()
        {
            if (!Controller.Grounded)
            {
                return false;
            }

            return true;
        }

        public override bool CanStopActivity()
        {
            // crouch can't be stopped if we have obstacles above us

            Ray ray = new Ray(Controller.CameraTransform.position, Vector3.up);

            float standingHeight = 2f;
            float raycastDistance = standingHeight - Controller.CameraTransform.localPosition.y;

            raycastDistance = Mathf.Max(0f, raycastDistance);

            if (Physics.Raycast(ray, raycastDistance, Preset.ObstacleLayerMask))
            {
                return false;
            }

            return true;
        }

    }
}