using UnityEngine;

namespace Hermes {
    public class Activity : MonoBehaviour
    {
        public virtual bool Active { get; protected set; } = false;

        #region Static Methods

        public static bool IsActive(Activity activity)
        {
            return activity != null && activity.Active;
        }

        public static bool TryStart(Activity activity)
        {
            return activity != null && activity.TryStartActivity();
        }

        public static bool TryStop(Activity activity)
        {
            return activity != null && activity.TryStopActivity();
        }

        public static bool TryToggle(Activity activity)
        {
            return activity != null && activity.TryToggleActivity();
        }

        #endregion

        public virtual bool CanStartActivity()
        {
            return true;
        }

        protected virtual void OnActivityStarted()
        {
            // Override in derived classes to implement custom behavior when the activity starts.
        }

        private void StartActivity()
        {
            Active = true;
            OnActivityStarted();
        }

        public virtual bool CanStopActivity()
        {
            return true;
        }

        protected virtual void OnActivityStopped()
        {
            // Override in derived classes to implement custom behavior when the activity stops.
        }

        private void StopActivity()
        {
            Active = false;
            OnActivityStopped();
        }

        public bool TryStartActivity()
        {
            if (!CanStartActivity())
            {
                return false;
            }

            StartActivity();
            return true;
        }

        public bool TryStopActivity()
        {
            if (!CanStopActivity())
            {
                return false;
            }

            StopActivity();
            return true;
        }

        public bool TryToggleActivity()
        {
            if (Active)
            {
                return TryStopActivity();
            }

            return TryStartActivity();
        }


    }
}
