using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueBoxController))]
public class DialogueControllerEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DialogueBoxController dbc = (DialogueBoxController)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Show Dialogue Box", GUILayout.Height(30)))
            {
                // provide sample title/description for the debug call
                dbc.ShowDialogueBox("DEBUG: Ability Unlocked", "This is a debug description shown from the editor.");
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
