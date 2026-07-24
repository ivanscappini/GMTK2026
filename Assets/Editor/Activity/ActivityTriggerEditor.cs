using UnityEditor;
using UnityEngine;
using Hermes;

namespace HermesEditor
{
    [CustomEditor(typeof(ActivityTrigger))]
    public class ActivityTriggerEditor : Editor
    {
        private bool activitiesFoldout = true;

        private SerializedProperty activityProperty;

        private void OnEnable()
        {
            activityProperty =
                serializedObject.FindProperty("activity");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            ActivityManager manager =
                FindFirstObjectByType<ActivityManager>();

            if (manager == null)
            {
                EditorGUILayout.HelpBox(
                    "No ActivityManager found in the scene.",
                    MessageType.Warning
                );

                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawActivities(manager);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActivities(ActivityManager manager)
        {
            if (manager.activitiesListInternal == null ||
                manager.activitiesListInternal.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The ActivityManager has no activities.",
                    MessageType.Info
                );

                return;
            }

            EditorGUILayout.Space();

            activitiesFoldout = EditorGUILayout.Foldout(
                activitiesFoldout,
                "Activity To Enable",
                true
            );

            if (!activitiesFoldout)
                return;

            EditorGUI.indentLevel++;

            Activity selectedActivity =
                activityProperty.objectReferenceValue as Activity;

            foreach (Activity activity in
                     manager.activitiesListInternal)
            {
                if (activity == null)
                    continue;

                bool isSelected =
                    selectedActivity == activity;

                bool nextSelected =
                    EditorGUILayout.ToggleLeft(
                        activity.GetType().Name,
                        isSelected
                    );

                if (nextSelected && !isSelected)
                {
                    activityProperty.objectReferenceValue =
                        activity;
                }
                else if (!nextSelected && isSelected)
                {
                    activityProperty.objectReferenceValue =
                        null;
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}