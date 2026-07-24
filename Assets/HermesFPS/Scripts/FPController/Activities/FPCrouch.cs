using UnityEngine;

namespace Hermes
{
    public class FPCrouch : FPActivity
    {
        protected override void Awake()
        {
            base.Awake();

            Controller.Crouch = this;
        }

        public override bool CanStartActivity()
        {
            if (Activity.IsActive(Controller.Sprint))
            {
                return false;
            }

            return true;
        }

        public override bool CanStopActivity()
        {
            var cc = Controller.CharacterController;
            float standingHeight = 2f; // Your standing collider height
            float crouchedHeight = cc.height; // Current collider height when crouched

            // Calculate the position at the top of the crouched collider
            Vector3 crouchedTop = Controller.transform.position + cc.center + Vector3.up * (crouchedHeight / 2f);

            // Calculate how much higher the standing collider would be
            float heightDifference = standingHeight - crouchedHeight;

            if (heightDifference <= 0f)
                return true; // Already at or above standing height

            // Raycast from the top of the crouched collider upwards
            if (Physics.Raycast(crouchedTop, Vector3.up, heightDifference, Preset.ObstacleLayerMask))
            {
                return false;
            }

            return true;
        }
    }
}