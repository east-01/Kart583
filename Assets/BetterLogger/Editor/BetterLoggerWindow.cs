using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public class BetterLoggerWindow : EditorWindow
{

    private BLog betterLogInstance;
    private Vector2 scrollPos;

    private Dictionary<LogChannel, LogChannelData> data = new();

    private void Awake() 
    {
        LoadData();
    }

    [MenuItem("Window/Better Logger")]
    public static void ShowWindow() 
    {
        GetWindow<BetterLoggerWindow>("Better Logger");
        LoadData();
    }

    private void OnGUI() 
    {
        betterLogInstance = EditorGUILayout.ObjectField("Better logger script:", betterLogInstance, typeof(BLog), false) as BLog;

        if(betterLogInstance == null) {
            GUILayout.Label("No BLog script selected.");
            return;
        }

        GUILayout.Space(5);

        bool save = false;
        bool initialVerbosity = betterLogInstance.GetVerbosity();
        betterLogInstance.SetVerbosity(EditorGUILayout.IntSlider("Verbosity:", betterLogInstance.GetVerbosity(), 0, BLog.MAX_VERBOSITY));
        if(betterLogInstance.GetVerbosity() != initialVerbosity)
            save = true;

        GUILayout.Space(5);

        CreateHeader("Channels:");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach(LogChannel channel in Enum.GetValues(typeof(LogChannel))) 
        {
            if(channel == LogChannel.Default)
                continue;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(FormatEnum(channel.ToString()), new GUILayoutOption[] {GUILayout.Width(125)});

            LogChannelData data = betterLogInstance.HasData(channel) ? betterLogInstance.GetData(channel) : LogChannelData.DefaultData;

            bool initialEnable = data.enable;
            data.enable = EditorGUILayout.Toggle(data.enable);
            if(data.enable != initialEnable)
                save = true;

            Color initialColor = data.color;
            data.color = EditorGUILayout.ColorField(data.color, new GUILayoutOption[] {GUILayout.Width(100)});
            if(data.color != initialColor)
                save = true;

            betterLogInstance.SetData(channel, data);

            EditorGUILayout.EndHorizontal();            
        }

        EditorGUILayout.EndScrollView();

        if(save)
            SaveData();
    }

    private void SaveData() 
    {
        string json = JSonUtility.ToJson(betterLogInstance);
    }

    private void LoadData() 
    {
        Debug.Log("Loading data");
    }

    public static string FormatEnum(string enumString)
    {
        string formattedString = enumString.Replace("_", " ");
        formattedString = Regex.Replace(formattedString, @"\b(\w)", m => m.Value.ToUpper());
        return formattedString;
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