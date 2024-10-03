using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

public class DevSettingsWindow : EditorWindow
{
    
    /* Editor properties */
    private Vector2 scrollPos;

    [MenuItem("Window/Developer Settings")]
    public static void ShowWindow() 
    {
        GetWindow<DevSettingsWindow>("Developer Settings");
    }

    private void OnGUI() 
    {
        DevSettings settings = DevSettings.Settings;
        string initialSerialization = DevSettings.Serialize(); // See postSerialization at the end of method for explanation

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(10);

        CreateBigHeader("Version");

        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(5);
        settings.major = EditorGUILayout.IntField(settings.major, new GUILayoutOption[] {GUILayout.Width(25)});
        EditorGUILayout.LabelField(".", new GUILayoutOption[] {GUILayout.Width(7)});
        settings.minor = EditorGUILayout.IntField(settings.minor, new GUILayoutOption[] {GUILayout.Width(25)});
        EditorGUILayout.LabelField(".", new GUILayoutOption[] {GUILayout.Width(7)});
        settings.revision = EditorGUILayout.IntField(settings.revision, new GUILayoutOption[] {GUILayout.Width(25)});
        GUILayout.Space(10);
        settings.releaseType = (ReleaseType)EditorGUILayout.EnumPopup(settings.releaseType, new GUILayoutOption[] {GUILayout.Width(120)});

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        CreateBigHeader("Developer Settings");

        settings.Enable = EditorGUILayout.Toggle("Enable", settings.Enable);

        GUILayout.Space(5);

        if(settings.Enable) {
            CreateHeader("General");
            settings.LoadMode = (LoadMode) EditorGUILayout.EnumPopup("Load mode", settings.LoadMode);

            settings.OverridePlayerLimit = EditorGUILayout.Toggle("Override player limit", settings.OverridePlayerLimit);
            if(settings.OverridePlayerLimit) {
                EditorGUI.indentLevel++;
                settings.PlayerLimit = EditorGUILayout.IntSlider("Player limit", settings.PlayerLimit, 1, 8);
                EditorGUI.indentLevel--;
            }

            settings.OverrideMapPick = EditorGUILayout.Toggle("Override map pick", settings.OverrideMapPick);
            if(settings.OverrideMapPick) {
                EditorGUI.indentLevel++;
                settings.Map = (KartLevel)EditorGUILayout.EnumPopup("Map", settings.Map);
                EditorGUI.indentLevel--;
            }

            settings.EnableWarpPoint = EditorGUILayout.Toggle("Enable warp point", settings.EnableWarpPoint);
            if(settings.EnableWarpPoint) {
                EditorGUI.indentLevel++;
                settings.WarpPosition = EditorGUILayout.Vector3Field("Warp position", settings.WarpPosition);
                CreateNote("Use LCtrl+N to save and LCtrl+M to load. Only works in local instances.");
                EditorGUI.indentLevel--;                
            }

            GUILayout.Space(5);

            CreateHeader("Networking");
            settings.HaveStandalonePlayerRunAsServer = EditorGUILayout.Toggle("Player as server", settings.HaveStandalonePlayerRunAsServer);
            CreateNote("When a standalone player is built, it will automatically run as a server.");
            settings.ManualLobbyPlayerWaitSwitch = EditorGUILayout.Toggle("Manual lobby switch", settings.ManualLobbyPlayerWaitSwitch);
            CreateNote($"When enabled the gamelobby will wait until {KartLobby.FORCE_MAP_PICK_KEY} is pressed to pick map.");

            GUILayout.Space(10);

            CreateHeader("Race Settings");
            settings.OverrideRaceProgressAtStart = EditorGUILayout.Toggle("Override race progress", settings.OverrideRaceProgressAtStart);
            CreateNote("Instead of putting the player at the start, this will spawn the player at x race progress.");
            CreateNote("BE CAREFUL: This will automatically disable the starting countdown.");
            if(settings.OverrideRaceProgressAtStart) {
                EditorGUI.indentLevel++;
                settings.RaceProgress = EditorGUILayout.Slider("Race progress", settings.RaceProgress, 0, 1);
                EditorGUI.indentLevel--;
            }

            settings.OverrideLapCount = EditorGUILayout.Toggle("Override lap count", settings.OverrideLapCount);
            if(settings.OverrideLapCount) {
                EditorGUI.indentLevel++;
                settings.LapCount = EditorGUILayout.IntSlider("Lap count", settings.LapCount, 1, 20);
                EditorGUI.indentLevel--;
            }

            settings.OverrideBots = EditorGUILayout.Toggle("Override bots", settings.OverrideBots);
            if(settings.OverrideBots) {
                EditorGUI.indentLevel++;
                settings.Bots = EditorGUILayout.Toggle("Spawn bots", settings.Bots);
                EditorGUI.indentLevel--;            
            }
        }

        EditorGUILayout.EndScrollView();

        // Compare the initial serialization with the post serialization to see if values changed
        string postSerialization = DevSettings.Serialize();
        if(!initialSerialization.Equals(postSerialization))
            DevSettings.SaveSettings(postSerialization);
    }

    private void CreateBigHeader(string text) 
    {
        GUILayout.Label($"<b><color=white>{text}</color></b>", BigHeaderStyle);
    }

    private void CreateHeader(string text) 
    {
        GUILayout.Label($"<b><color=white>{text}</color></b>", HeaderStyle);
    }

    private void CreateNote(string text) 
    {
        GUILayout.Label($"<i><color=#a7abb0>{text}</color></i>", NoteStyle);
    }

    public GUIStyle BigHeaderStyle { get {
        return new() {
            richText = true,
            margin = new RectOffset(3, 10, 0, 10),
            fontSize = 15
        };
    } }

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
