using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Timer))]
public class TimerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Timer timer = (Timer)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Add Time", GUILayout.Height(30)))
            {
                timer.AddTime(5f);
            }

            if (GUILayout.Button("- Subtract Time", GUILayout.Height(30)))
            {
                timer.SubtractTime(5f);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}