using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(TransitionManager))]
public class CoreManager : MonoBehaviour
{

    /* On-component references */
    public static CoreManager Instance;
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance.gameplayManagerDelegate; } }
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }

    /* Atlas prefab access*/
    public static LevelAtlas LevelAtlas { get { return Instance.atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public static KartAtlas KartAtlas { get { return Instance.atlasesPrefab.GetComponent<KartAtlas>(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.atlasesPrefab.GetComponent<ItemAtlas>(); } }
    public static AudioAtlas AudioAtlas { get { return Instance.atlasesPrefab.GetComponent<AudioAtlas>(); } }
    public static AudioClipPackage AudioClipPackage(AudioFile file) { return Instance.atlasesPrefab.GetComponent<AudioAtlas>().clips[(int)file]; }
    public static AudioClip AudioClip(AudioFile file) { return AudioClipPackage(file).audioClip; }

    [SerializeField] private GameObject atlasesPrefab;

    [Header("Settings")] public bool isMultiplayer;
    [SerializeField] private int playerLimit = 8;

    private GameplayManagerDelegate gameplayManagerDelegate;
    private TransitionManager transitionManager;

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

        gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        transitionManager = GetComponent<TransitionManager>();
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
