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
        [SerializeField] internal List<Activity> activitiesListInternal = new();

        // Parallel list persisted in the inspector that stores whether each activity is enabled.
        [SerializeField] internal List<bool> activitiesEnabledInternal = new();

        internal Dictionary<System.Type, Activity> lookup;

        // runtime cache for enabled state keyed by activity Type
        internal Dictionary<System.Type, bool> enabledLookup;

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
            enabledLookup = new();

            // Ensure enabled list matches activities list length
            if (activitiesEnabledInternal == null)
                activitiesEnabledInternal = new();

            while (activitiesEnabledInternal.Count < activitiesListInternal.Count)
            {
                activitiesEnabledInternal.Add(true); // default enabled
            }

            if (activitiesEnabledInternal.Count > activitiesListInternal.Count)
            {
                activitiesEnabledInternal.RemoveRange(activitiesListInternal.Count, activitiesEnabledInternal.Count - activitiesListInternal.Count);
            }

            for (int i = 0; i < activitiesListInternal.Count; i++)
            {
                var activity = activitiesListInternal[i];
                if (activity == null)
                    continue;

                activity.Manager = this;
                lookup[activity.GetType()] = activity;

                bool enabled = true;
                if (i < activitiesEnabledInternal.Count)
                    enabled = activitiesEnabledInternal[i];

                enabledLookup[activity.GetType()] = enabled;
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

        #region Activity Enabled/Disabled API

        public bool IsEnabled(Activity activity)
        {
            if (activity == null)
                return false;

            return IsEnabled(activity.GetType());
        }

        public bool IsEnabled<T>() where T : Activity
        {
            return IsEnabled(typeof(T));
        }

        public bool IsEnabled(System.Type activityType)
        {
            if (activityType == null)
                return false;

            if (enabledLookup == null)
                return true;

            if (enabledLookup.TryGetValue(activityType, out bool enabled))
                return enabled;

            // default to true if unknown
            return true;
        }

        public void SetEnabled(Activity activity, bool enabled)
        {
            if (activity == null)
                return;

            SetEnabled(activity.GetType(), enabled);

            // also update serializable list if activity exists there
            int idx = activitiesListInternal.IndexOf(activity);
            if (idx >= 0)
            {
                if (activitiesEnabledInternal == null)
                    activitiesEnabledInternal = new();
                while (activitiesEnabledInternal.Count < activitiesListInternal.Count)
                    activitiesEnabledInternal.Add(true);
                activitiesEnabledInternal[idx] = enabled;
            }
        }

        public void SetEnabled<T>(bool enabled) where T : Activity
        {
            SetEnabled(typeof(T), enabled);
        }

        public void SetEnabled(System.Type activityType, bool enabled)
        {
            if (activityType == null)
                return;

            if (enabledLookup == null)
                enabledLookup = new();

            enabledLookup[activityType] = enabled;

            // If disabling an active activity, stop it immediately
            if (!enabled && lookup != null && lookup.TryGetValue(activityType, out var activity) && IsActive(activity))
            {
                ForceStop(activity);
            }
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

            // keep enabled list in sync
            if (activitiesEnabledInternal == null)
                activitiesEnabledInternal = new();
            activitiesEnabledInternal.Add(true);
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
                    if (activitiesEnabledInternal != null && activitiesEnabledInternal.Count > i)
                        activitiesEnabledInternal.RemoveAt(i);
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
                        // move enabled flag as well
                        if (activitiesEnabledInternal != null && activitiesEnabledInternal.Count > i)
                        {
                            activitiesEnabledInternal[writeIndex] = activitiesEnabledInternal[i];
                        }
                    }

                    writeIndex++;
                }
            }

            if (activitiesEnabledInternal != null && activitiesEnabledInternal.Count > writeIndex)
            {
                activitiesEnabledInternal.RemoveRange(writeIndex, activitiesEnabledInternal.Count - writeIndex);
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

            // Respect enabled state
            if (!IsEnabled(activity))
            {
                return false; // Activity disabled
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
