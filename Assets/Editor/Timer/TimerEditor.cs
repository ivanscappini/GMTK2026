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
            
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("(x2) Double Speed", GUILayout.Height(30)))
            {
                timer.SetTimerSpeed(2f);
            }
            
            if (GUILayout.Button("(x1) Normal Speed", GUILayout.Height(30)))
            {
                timer.SetTimerSpeed(1f);
            }

            if (GUILayout.Button("(x0.5) Half Speed", GUILayout.Height(30)))
            {
                timer.SetTimerSpeed(.5f);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}