using UnityEngine;
using UnityEditor;
using System.Drawing.Printing;
using Unity.VisualScripting;

[CustomEditor(typeof(DevSettings))]
public class DevSettingsEditor : Editor
{

    private DevSettings devSettings;
    private SerializedObject serializedTargetDevSettings;

    private SerializedProperty masterEnable;
    private SerializedProperty loadMode;
    private SerializedProperty overridePlayerLimit;
    private SerializedProperty overrideMapPick;
    private SerializedProperty haveStandalonePlayerRunAsServer;
    private SerializedProperty manualLobbyPlayerWaitSwitch;
    private SerializedProperty overrideRaceProgressAtStart;
    private SerializedProperty overrideLapCount;
    private SerializedProperty overrideBots;

    private void OnEnable() 
    {
        InitializeProperties();
    }

    public override void OnInspectorGUI() 
    {
        InitializeProperties();

        GUILayout.Space(10);

        masterEnable.boolValue = EditorGUILayout.Toggle("Enable", masterEnable.boolValue);

        GUILayout.Space(5);

        if(!masterEnable.boolValue) {
            serializedTargetDevSettings.ApplyModifiedProperties();
            return;
        }

        CreateHeader("General");
        EditorGUILayout.PropertyField(loadMode, new GUIContent("Load mode"));

        overridePlayerLimit.boolValue = EditorGUILayout.Toggle("Override player limit", overridePlayerLimit.boolValue);
        if(overridePlayerLimit.boolValue) {
            EditorGUI.indentLevel++;
            devSettings.playerLimit = EditorGUILayout.IntSlider("Player limit", devSettings.playerLimit, 1, 8);
            EditorGUI.indentLevel--;
        }

        overrideMapPick.boolValue = EditorGUILayout.Toggle("Override map pick", overrideMapPick.boolValue);
        if(overrideMapPick.boolValue) {
            EditorGUI.indentLevel++;
            devSettings.map = (KartLevel)EditorGUILayout.EnumPopup("Map", devSettings.map);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(5);

        CreateHeader("Networking");
        haveStandalonePlayerRunAsServer.boolValue = EditorGUILayout.Toggle("Player as server", haveStandalonePlayerRunAsServer.boolValue);
        CreateNote("When a standalone player is built, it will automatically run as a server.");
        manualLobbyPlayerWaitSwitch.boolValue = EditorGUILayout.Toggle("Manual lobby switch", manualLobbyPlayerWaitSwitch.boolValue);
        CreateNote($"When enabled the gamelobby will wait until {GameLobby.FORCE_MAP_PICK_KEY} is pressed to pick map.");

        GUILayout.Space(10);

        CreateHeader("Race Settings");
        overrideRaceProgressAtStart.boolValue = EditorGUILayout.Toggle("Override race progress", overrideRaceProgressAtStart.boolValue);
        CreateNote("Instead of putting the player at the start, this will spawn the player at x race progress.");
        CreateNote("BE CAREFUL: This will automatically disable the starting countdown.");
        if(overrideRaceProgressAtStart.boolValue) {
            EditorGUI.indentLevel++;
            devSettings.raceProgress = EditorGUILayout.Slider("Race progress", devSettings.raceProgress, 0, 1);
            EditorGUI.indentLevel--;
        }

        overrideLapCount.boolValue = EditorGUILayout.Toggle("Override lap count", overrideLapCount.boolValue);
        if(overrideLapCount.boolValue) {
            EditorGUI.indentLevel++;
            devSettings.lapCount = EditorGUILayout.IntSlider("Lap count", devSettings.lapCount, 1, 20);
            EditorGUI.indentLevel--;
        }

        overrideBots.boolValue = EditorGUILayout.Toggle("Override bots", overrideBots.boolValue);
        if(overrideBots.boolValue) {
            EditorGUI.indentLevel++;
            devSettings.bots = EditorGUILayout.Toggle("Spawn bots", devSettings.bots);
            EditorGUI.indentLevel--;            
        }

        serializedTargetDevSettings.ApplyModifiedProperties();
    }

    private void InitializeProperties() 
    {
        devSettings = (DevSettings) target;
        serializedTargetDevSettings = new(target);

        masterEnable = serializedTargetDevSettings.FindProperty("masterEnable");
        loadMode = serializedTargetDevSettings.FindProperty("loadMode");
        overridePlayerLimit = serializedTargetDevSettings.FindProperty("overridePlayerLimit");
        overrideMapPick = serializedTargetDevSettings.FindProperty("overrideMapPick");
        haveStandalonePlayerRunAsServer = serializedTargetDevSettings.FindProperty("haveStandalonePlayerRunAsServer");
        manualLobbyPlayerWaitSwitch = serializedTargetDevSettings.FindProperty("manualLobbyPlayerWaitSwitch");
        overrideRaceProgressAtStart = serializedTargetDevSettings.FindProperty("overrideRaceProgressAtStart");
        overrideLapCount = serializedTargetDevSettings.FindProperty("overrideLapCount");
        overrideBots = serializedTargetDevSettings.FindProperty("overrideBots");
    }

    private void CreateHeader(string text) 
    {
        GUILayout.Label($"<b><color=white>{text}</color></b>", HeaderStyle);
    }

    private void CreateNote(string text) 
    {
        GUILayout.Label($"<i><color=#a7abb0>{text}</color></i>", NoteStyle);
    }

    public GUIStyle HeaderStyle { get {
        return new() {
            richText = true,
            margin = new RectOffset(3, 0, 0, 0)
        };
    } }

    public GUIStyle NoteStyle { get {
        return new() {
            richText = true,
            margin = new RectOffset(5, 0, 0, 0)
        };
    } }

}
