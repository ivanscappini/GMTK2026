using UnityEngine;

namespace Hermes {
    public class FPSprint : FPActivity
    {
        protected override void Awake()
        {
            base.Awake();

            Controller.Sprint = this;
        }

        public override bool CanStartActivity()
        {
            bool crouch = Activity.IsActive(Controller.Crouch);

            return !crouch;
        }

        private void Update()
        {
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
