using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;

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
    }
}
