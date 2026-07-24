using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Hermes {
    public abstract class Activity : MonoBehaviour
    {
        public bool Active => Manager.IsActive(this);

        public ActivityManager Manager { get; internal set; }

        [Tooltip("Activities that will be stopped when this activity starts")]
        [SerializeField, HideInInspector] List<Activity> interruptList = new();

        public IReadOnlyList<Activity> InterruptList => interruptList.AsReadOnly();

        [Tooltip("This activity cannot start if any of the activities in this list are active")]
        [SerializeField, HideInInspector] List<Activity> blockedByList = new();

        public IReadOnlyList<Activity> BlockedByList => blockedByList.AsReadOnly();

        #region InterruptList Management

        public bool InInterruptList(Activity other)
        {
            return interruptList.Contains(other);
        }

        public void AddToInterruptList(Activity activity)
        {
            if (!interruptList.Contains(activity))
            {
                interruptList.Add(activity);
            }
        }

        public void RemoveFromInterruptList(Activity activity)
        {
            if (interruptList.Contains(activity))
            {
                interruptList.Remove(activity);
            }
        }

        #endregion

        #region BlockedByList Management

        public bool IsAnyBlockingActivityActive()
        {
            foreach (var activity in blockedByList)
            {
                if (activity.Active) return true;
            }
            return false;
        }

        public bool InBlockedByList(Activity other)
        {
            return blockedByList.Contains(other);
        }

        public void AddToBlockedByList(Activity activity)
        {
            if (!blockedByList.Contains(activity))
            {
                blockedByList.Add(activity);
            }
        }

        public void RemoveFromBlockedByList(Activity activity)
        {
            if (blockedByList.Contains(activity))
            {
                blockedByList.Remove(activity);
            }
        }

        #endregion

        #region Virtual Methods
        public virtual bool CanStartActivity()
        {
            return true;
        }

        public virtual bool CanStopActivity()
        {
            return true;
        }

        public virtual void OnActivityStarted()
        {
            // Override in derived classes to implement custom behavior when the activity starts.
        }

        public virtual void OnActivityStopped()
        {
            // Override in derived classes to implement custom behavior when the activity stops.
        }

        #endregion

    }
}
