using UnityEngine;

namespace Hermes
{
    // blocked by: crouch
    public class FPSprint : FPActivity
    {
        private void Update()
        {
            if (Controller.SprintInput && Controller.HorizontalSpeed > 0.1f)
            {
                Manager.TryStart(this);
            }

            if (Controller.SprintInput == false || Controller.HorizontalSpeed <= 0.1f)
            {
                Manager.TryStop(this);
            }
        }
    }
}