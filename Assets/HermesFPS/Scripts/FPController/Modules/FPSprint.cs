using UnityEngine;

namespace Hermes {
    public class FPSprint : FPActivity
    {
        protected override void Awake()
        {
            base.Awake();

            Controller.Sprint = this;
        }

        private void Update()
        {
            // If sprint toggle mode is enabled we should not rely on Controller.SprintInput (hold).
            // Toggle mode: Player calls Activity.TryToggle(this) to start/stop the activity.
            if (Controller.SprintToggleMode)
            {
                // Still stop sprint automatically if player stops moving.
                if (Controller.CurrentSpeed <= 0.1f)
                {
                    TryStopActivity();
                }

                // Do not perform hold-based start/stop checks.
                return;
            }

            // Default hold-to-sprint behavior
            if (Controller.SprintInput == true && Controller.CurrentSpeed > 0.1f)
            {
                TryStartActivity();
            }

            if (Controller.SprintInput == false || Controller.CurrentSpeed <= 0.1f)
            {
                TryStopActivity();
            }
        }


    }
}
