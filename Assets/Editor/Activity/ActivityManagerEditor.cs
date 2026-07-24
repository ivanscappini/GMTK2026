using UnityEditor;
using UnityEngine;

namespace Hermes
{
    [CustomEditor(typeof(ActivityManager))]
    public class ActivityManagerEditor : Editor
    {
        bool activitiesFoldout = true;
        bool enableFlagsFoldout = true;

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
                DrawActivityEnableFlagsInspector(manager);
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

        #region Activity Enable Flags Inspector

        void DrawActivityEnableFlagsInspector(ActivityManager manager)
        {
            if (manager.activitiesListInternal == null || manager.activitiesListInternal.Count == 0)
                return;

            EditorGUILayout.Space();
            enableFlagsFoldout = EditorGUILayout.Foldout(enableFlagsFoldout, "Activity Enable Flags", true);
            if (!enableFlagsFoldout)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            // Ensure the enabled list exists and is synced visually with activities list length
            if (manager.activitiesEnabledInternal == null)
                manager.activitiesEnabledInternal = new System.Collections.Generic.List<bool>();

            while (manager.activitiesEnabledInternal.Count < manager.activitiesListInternal.Count)
                manager.activitiesEnabledInternal.Add(true);

            for (int i = 0; i < manager.activitiesListInternal.Count; i++)
            {
                var activity = manager.activitiesListInternal[i];
                if (activity == null)
                    continue;

                bool current = true;
                if (i < manager.activitiesEnabledInternal.Count)
                    current = manager.activitiesEnabledInternal[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(activity.GetType().Name, GUILayout.MaxWidth(180));
                bool next = EditorGUILayout.ToggleLeft("Enabled", current, GUILayout.Width(80));
                EditorGUILayout.ObjectField(activity, typeof(Activity), true);
                EditorGUILayout.EndHorizontal();

                if (next != current)
                {
                    Undo.RecordObject(target, "Toggle Activity Enabled");
                    manager.activitiesEnabledInternal[i] = next;
                    manager.SetEnabled(activity, next);
                    EditorUtility.SetDirty(target);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            EditorGUI.indentLevel--;
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
                    if (activity == null)
                        continue;
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
