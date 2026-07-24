using UnityEditor;
using UnityEngine;

namespace Hermes
{
    [CustomEditor(typeof(ActivityManager))]
    public class ActivityManagerEditor : Editor
    {
        bool activitiesFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            var manager = (ActivityManager)target;

            if (!Application.isPlaying)
            {
                manager.RemoveNullActivitiesInternal();
                manager.RemoveDuplicateActivitiesInternal();
                DrawDefaultInspector();
                DrawStrayActivitiesInspector();
            }

            DrawRuntimeActivitiesStatusInspector();

            if (EditorGUI.EndChangeCheck() )
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        #region Stray Activities Inspector

        void DrawStrayActivitiesInspector()
        {
            if (Application.isPlaying)
            {
                return; // Only show this section in edit mode
            }

            int strayCount = FindStrayActivitiesCount();

            if (strayCount <= 0)
            {
                return; // No stray activities found, no need to show the section
            }

            EditorGUILayout.HelpBox($"Found {strayCount} stray activities in children that are not in the activities list.", MessageType.Warning);

            if (GUILayout.Button("Add Stray Activities to Manager"))
            {
                AddStrayActivities();
            }
        }

        int FindStrayActivitiesCount()
        {
            var manager = (ActivityManager)target;
            var children = manager.GetComponentsInChildren<Activity>();

            int count = 0;

            foreach (var child in children)
            {
                // Count activities that belong to this manager in the hierarchy but are NOT present in the internal list.
                if (IsDirectChild(child) && !manager.HasActivityInternal(child))
                {
                    count++;
                }
            } return count;
        }

        void AddStrayActivities()
        {
            var manager = (ActivityManager)target;
            var children = manager.GetComponentsInChildren<Activity>();

            Undo.RecordObject(target, "Add Stray Activities");

            foreach (var child in children)
            {
                // Add only activities that belong to this manager in the hierarchy and are not already in the list.
                if (IsDirectChild(child) && !manager.HasActivityInternal(child))
                {
                    manager.AddActivityInternal(child);
                }
            }
        }

        bool IsDirectChild(Activity activity)
        {
            var manager = (ActivityManager)target;
            var otherManager = activity.GetComponentInParent<ActivityManager>();

            return manager == otherManager;
        }

        #endregion

        void DrawRuntimeActivitiesStatusInspector()
        {
            var manager = (ActivityManager)target;

            EditorGUILayout.LabelField("Activity Status", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUI.indentLevel++;

                foreach (var activity in manager.activitiesListInternal)
                {
                    EditorGUILayout.LabelField(activity.GetType().Name);
                }

                EditorGUI.indentLevel--;

                return;
            }

            if (manager.lookup == null || manager.lookup.Count == 0)
            {
                EditorGUILayout.HelpBox("No activities registered.", MessageType.Info);
                return;
            }

            // Create a style that enables rich text
            GUIStyle richLabelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                normal = { textColor = Color.white }
            };

            EditorGUI.indentLevel++;

            foreach (var (type, activity) in manager.lookup)
            {
                string statusColor = manager.IsActive(activity) ? "#00ff00ff" : "#ccccccff";

                string mainText = $"<color={statusColor}>{type.Name}</color>";
                string tooltip = $"{activity.GetType().FullName}";

                EditorGUILayout.LabelField(new GUIContent(mainText, tooltip), richLabelStyle);
            }

            EditorGUI.indentLevel--;
        }
    }
}
