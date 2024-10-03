using System;
using System.Collections.Generic;
using AClockworkBerry;
using EMullen.Core;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SocialPlatforms;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(TransitionManager))]
public class CoreManager : MonoBehaviour
{

    /* On-component references */
    public static CoreManager Instance;
    public static AudioManager AudioManager { get { return Instance.audioManager; } }
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance.gameplayManagerDelegate; } }
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }

    /* Child component references */
    public static OptionsMenuController OptionsMenuController => Instance.optionsMenuController;
    public static EventSystem EventSystem => Instance.eventSystem;
    public static InputSystemUIInputModule InputSystemUIInputModule => Instance.inputSystemUIInputModule;
    public static InputActionAsset UIInputActionAsset => Instance.uiInputActionAsset;

    /* Atlas prefab access*/
    public static LevelAtlas LevelAtlas { get { return Instance.atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public static KartAtlas KartAtlas { get { return Instance.atlasesPrefab.GetComponent<KartAtlas>(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.atlasesPrefab.GetComponent<ItemAtlas>(); } }
    public static AudioAtlas AudioAtlas { get { return Instance.atlasesPrefab.GetComponent<AudioAtlas>(); } }
    public static AudioClipPackage AudioClipPackage(AudioFile file) { return Instance.atlasesPrefab.GetComponent<AudioAtlas>().clips[(int)file]; }
    public static AudioClip AudioClip(AudioFile file) { return AudioClipPackage(file).audioClip; }

    [Header("Prefabs"), SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject sceneControllerPrefab;
    [SerializeField] private GameObject netSceneControllerPrefab;
    [SerializeField] private GameObject playerObjectManagerPrefab;
    [SerializeField] private GameObject screenLoggerPrefab;
    [SerializeField] private GameObject atlasesPrefab;

    [Header("Settings")] public bool isMultiplayer;
    [SerializeField] private int playerLimit = 8;

    private AudioManager audioManager;
    private GameplayManagerDelegate gameplayManagerDelegate;
    private TransitionManager transitionManager;
    private EventSystem eventSystem;
    private InputSystemUIInputModule inputSystemUIInputModule;
    [SerializeField] private InputActionAsset uiInputActionAsset;

    [SerializeField] private OptionsMenuController optionsMenuController;

    private bool notifiedOfRelease = false;

    private void Awake() 
    {
        if(Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if(!notifiedOfRelease && !DevSettings.IsDevelopment()) {
            Debug.Log($"<color=aqua>Running release build {DevSettings.GetVersionString()}</color>");
            notifiedOfRelease = true;
        }

        audioManager = GetComponent<AudioManager>();
        gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        transitionManager = GetComponent<TransitionManager>();
        eventSystem = GetComponentInChildren<EventSystem>();
        inputSystemUIInputModule = GetComponentInChildren<InputSystemUIInputModule>();
    }

    private void Start() 
    {
        optionsMenuController.gameObject.SetActive(true);
        optionsMenuController.Close();
        optionsMenuController.LoadOptions();
    }

    private void OnDestroy() 
    {
        optionsMenuController.SaveOptions();
    }

    /// <summary>
    /// Check if the running instance is a server instance. More reliable than InstanceFinder because 
    ///   it will handle cases where a NetworkManager doesn't exist.
    /// </summary>
    public static bool IsServerOnly { get {
        if(NetworkManager.Instances.Count == 0) return false;
        return InstanceFinder.IsServerStarted && !InstanceFinder.IsHostStarted;
    } }
    public static bool IsMultiplayer { 
        get { return Instance.isMultiplayer; } 
        set { Instance.isMultiplayer = value;}
    }
    public static bool IsLocal { 
        get { return !IsMultiplayer; }
        set { IsMultiplayer = !value;}
    }

    public int PlayerLimit { get {
        if(DevSettings.Settings.OverridePlayerLimit)
            return DevSettings.Settings.PlayerLimit;
        else
            return playerLimit;
    } }
}
