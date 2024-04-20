using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

public class DevSettingsWindow : EditorWindow
{
    
    private DevSettings devSettings;
    private Editor editor;
    private Vector2 scrollPos;

    [MenuItem("Window/Developer Settings")]
    public static void ShowWindow() 
    {
        GetWindow<DevSettingsWindow>("Developer Settings");
    }

    private void OnGUI() 
    {
        devSettings = EditorGUILayout.ObjectField("Developer settings script:", devSettings, typeof(DevSettings), false) as DevSettings;
        
        if(devSettings == null) {
            GUILayout.Label("No DevSettings script selected.");
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if(editor == null || editor.target != devSettings)
            editor = Editor.CreateEditor(devSettings);
        
        editor.OnInspectorGUI();

        EditorGUILayout.EndScrollView();

    }

    private void OnDisable() 
    {
        if(editor != null)
            DestroyImmediate(editor);
    }

}
