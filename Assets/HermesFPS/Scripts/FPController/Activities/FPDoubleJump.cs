using UnityEngine;

namespace Hermes
{
    // Double-jump as a toggleable activity so unlocking can be managed by ActivityManager
    public class FPDoubleJump : FPActivity
    {
        [SerializeField] bool hasDoubleJumped = false;

        protected override void Awake()
        {
            base.Awake();

            Controller.Landed.AddListener(OnLanded);
        }

        private void Start()
        {
            // Subscribe in Start (not Awake) so FPWallJump's Awake subscription runs first.
            // This ensures JumpConsumed is already set when we check it.
            Controller.TryJump += OnTryJump;
        }

        void OnTryJump()
        {
            // If another handler consumed the jump (wall-jump), clear and skip
            if (Controller.JumpConsumed)
            {
                Controller.JumpConsumed = false;
                return;
            }

            // Only proceed if ability is unlocked (enabled) and player is airborne
            if (Manager == null || !Manager.IsEnabled(this))
                return;

            if (Controller.Grounded)
                return;

            if (hasDoubleJumped)
                return;

            // perform double jump
            float baseJumpV = Mathf.Sqrt(Mathf.Max(0f, Preset.JumpHeight) * -2f * Physics.gravity.y * Preset.GravityScale);
            Controller.VerticalSpeed = baseJumpV;

            hasDoubleJumped = true;

            Controller.DoubleJumped?.Invoke();
        }

        public override void OnActivityStopped()
        {
            base.OnActivityStopped();

            // reset flag when activity is turned off
            hasDoubleJumped = false;
        }

        void OnLanded()
        {
            // reset on landing
            hasDoubleJumped = false;
        }
    }
}
