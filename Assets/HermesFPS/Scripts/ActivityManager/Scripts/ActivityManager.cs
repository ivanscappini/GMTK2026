using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Hermes
{
    public class ActivityManager : MonoBehaviour
    {
        /// <summary>
        /// This array is only used in the inspector to feed the lookup directory
        /// </summary>
        [SerializeField] internal List<Activity> activitiesListInternal;

        internal Dictionary<System.Type, Activity> lookup;

        private HashSet<Activity> activeActivities = new();

        [Header("Events")]
        public UnityEvent<Activity> ActivityStarted;
        public UnityEvent<Activity> ActivityStopped;

        #region Unity Methods

        private void Awake()
        {
            PopulateLookupDictionary();
        }

        #endregion

        #region Utility Methods

        void PopulateLookupDictionary()
        {
            lookup = new();

            foreach (var activity in activitiesListInternal)
            {
                activity.Manager = this;
                lookup[activity.GetType()] = activity;
            }
        }

        public T GetActivity<T>() where T : Activity
        {
            if (lookup.TryGetValue(typeof(T), out var activity))
            {
                return (T)activity;
            }

            return null;
        }

        public bool IsActive<T>() where T : Activity
        {
            if (lookup.TryGetValue(typeof(T), out var activity))
            {
                return IsActive(activity);
            }

            return false;
        }

        public bool IsActive(Activity activity)
        {
            return activeActivities.Contains(activity);
        }

        #endregion

        #region Activity Management Methods (Internal List)
        internal bool HasActivityInternal(Activity activity)
        {
            return activitiesListInternal.Contains(activity);
        }

        internal void AddActivityInternal(Activity activity)
        {
            if (Application.isPlaying)
            {
                Debug.Log("Can't add activities at runtime. Use the inspector to add activities.");
                return;
            }

            if (HasActivityInternal(activity))
            {
                Debug.LogWarning($"Activity {activity.name} already exists in the list.");
                return;
            }

            activitiesListInternal.Add(activity);
        }

        internal void RemoveNullActivitiesInternal()
        {
            if (Application.isPlaying)
            {
                Debug.Log("Can't remove activities at runtime. Use the inspector to remove activities.");
                return;
            }

            for (int i = activitiesListInternal.Count - 1; i >= 0; i--)
            {
                if (activitiesListInternal[i] == null)
                {
                    activitiesListInternal.RemoveAt(i);
                }
            }
        }

        internal void RemoveDuplicateActivitiesInternal()
        {
            if (Application.isPlaying)
            {
                Debug.Log("Can't edit activities list at runtime. Use the inspector to remove activities.");
                return;
            }

            if (activitiesListInternal.Count <= 1)
            {
                return; // No duplicates possible
            }

            var set = new HashSet<Activity>();
            int writeIndex = 0;

            for (int i =0; i < activitiesListInternal.Count; i++)
            {
                if (set.Add(activitiesListInternal[i]))
                {
                    if (writeIndex != i)
                    {
                        activitiesListInternal[writeIndex] = activitiesListInternal[i];
                    }

                    writeIndex++;
                }
            }

            activitiesListInternal.RemoveRange(writeIndex, activitiesListInternal.Count - writeIndex);
        }

        #endregion

        #region Starting Activities

        public bool CanStart(Activity activity)
        {
            if (IsActive(activity))
            {
                return false; // Already active
            }

            if (activity.IsAnyBlockingActivityActive())
            {
                return false; // Blocked by another activity
            }

            return activity.CanStartActivity();
        }

        public bool CanStart<T>() where T : Activity
        {
            return CanStart(GetActivity<T>());
        }

        public bool TryStart(Activity activity)
        {
            if (CanStart(activity))
            {
                ForceStart(activity);
                return true;
            }
            
            return false;
        }

        public bool TryStart<T>() where T : Activity
        {
            return TryStart(GetActivity<T>());
        }

        public void ForceStart(Activity activity)
        {
            if (IsActive(activity))
            {
                return; // Already active
            }

            activeActivities.Add(activity);
            activity.OnActivityStarted();

            ActivityStarted.Invoke(activity);

            // interrupt others
            foreach (var interruptedActivity in activity.InterruptList)
            {
                if (interruptedActivity != null && IsActive(interruptedActivity))
                {
                    ForceStop(interruptedActivity);
                }
            }
        }

        public void ForceStart<T>() where T : Activity
        {
            ForceStart(GetActivity<T>());
        }

        #endregion

        #region Stopping Activities

        public bool CanStop(Activity activity)
        {
            if (!IsActive(activity))
            {
                return false; // Already inactive
            }

            return activity.CanStopActivity();
        }

        public bool CanStop<T>() where T : Activity
        {
            return CanStop(GetActivity<T>());
        }

        public bool TryStop(Activity activity)
        {
            if (CanStop(activity))
            {
                ForceStop(activity);
                return true;
            }
            return false;
        }

        public bool TryStop<T>() where T : Activity
        {
            return TryStop(GetActivity<T>());
        }

        public void ForceStop(Activity activity)
        {
            if (!IsActive(activity))
            {
                return; // Already inactive
            }

            activeActivities.Remove(activity);
            activity.OnActivityStopped();

            ActivityStopped?.Invoke(activity);
        }

        public void ForceStop<T>() where T : Activity
        {
            ForceStop(GetActivity<T>());
        }

        #endregion

        #region Toggling Activities

        public bool TryToggle(Activity activity)
        {
            if (IsActive(activity))
            {
                return TryStop(activity);
            }
            else
            {
                return TryStart(activity);
            }
        }

        public bool TryToggle<T>() where T : Activity
        {
            return TryToggle(GetActivity<T>());
        }

        #endregion
    }
}
