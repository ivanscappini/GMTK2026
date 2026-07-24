using UnityEngine;
using UnityEditor;

namespace Hermes
{
    [CustomEditor(typeof(Activity), true)]
    public class ActivityEditor : Editor
    {
        bool showInterrupts = false;
        bool showBlockedBy = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            var activity = (Activity)target;

            if (activity.Manager == null)
            {
                activity.Manager = activity.GetComponentInParent<ActivityManager>();
            }

            if (activity.Manager == null)
            {
                EditorGUILayout.HelpBox("Activity Manager not found in parents.", MessageType.Warning);
            }
            else
            {
                if (!activity.Manager.HasActivityInternal(activity))
                {
                    EditorGUILayout.HelpBox("Activity not added to the Activity Manager.", MessageType.Warning);

                    if (GUILayout.Button("Add Activity to Manager"))
                    {
                        activity.Manager.AddActivityInternal(activity);
                    }
                }

                DrawInterruptsGUI();

                EditorGUILayout.Space(10f);

                DrawBlockedByGUI();

                EditorGUILayout.Space(10f);
            }

            EditorGUILayout.Space(20f);

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        void DrawInterruptsGUI()
        {
            var activity = (Activity)target;
            var manager = activity.Manager;

            int count = activity.InterruptList.Count;

            showInterrupts = EditorGUILayout.Foldout(showInterrupts, $"Interrupts ({count})", true);

            if (showInterrupts)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("These activities will be forced to stop as soon as the current activity starts.", MessageType.Info);

                foreach (var other in manager.activitiesListInternal)
                {
                    if (other == activity || other == null)
                    {
                        continue;
                    }

                    bool inInterruptList = activity.InInterruptList(other);
                    bool wasInInterruptList = inInterruptList;

                    var guiContent = new GUIContent(other.GetType().Name, other.GetType().FullName);

                    inInterruptList = EditorGUILayout.Toggle(guiContent, inInterruptList);

                    if (!wasInInterruptList && inInterruptList)
                    {
                        activity.AddToInterruptList(other);
                    }
                    else if (wasInInterruptList && !inInterruptList)
                    {
                        activity.RemoveFromInterruptList(other);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        void DrawBlockedByGUI()
        {
            var activity = (Activity)target;
            var manager = activity.Manager;

            int count = activity.BlockedByList.Count;

            showBlockedBy = EditorGUILayout.Foldout(showBlockedBy, $"Blocked By ({count})", true);

            if (showBlockedBy)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("These activities will be forced to stop as soon as the current activity starts.", MessageType.Info);

                foreach (var other in manager.activitiesListInternal)
                {
                    if (other == activity || other == null)
                    {
                        continue;
                    }

                    bool inBlockedByList = activity.InBlockedByList(other);
                    bool wasInBlockedByList = inBlockedByList;

                    var guiContent = new GUIContent(other.GetType().Name, other.GetType().FullName);

                    inBlockedByList= EditorGUILayout.Toggle(guiContent, inBlockedByList);

                    if (!wasInBlockedByList && inBlockedByList)
                    {
                        activity.AddToBlockedByList(other);
                    }
                    else if (wasInBlockedByList && !inBlockedByList)
                    {
                        activity.RemoveFromBlockedByList(other);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
