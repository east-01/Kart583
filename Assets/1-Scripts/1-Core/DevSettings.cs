using System.Collections;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

public class DevSettings : MonoBehaviour
{

    [SerializeField] private GameObject atlasesPrefab;

    [Space]
    [SerializeField] private bool masterEnable;

    [Header("General")]
    [SerializeField] private bool overridePlayerLimit = false;
    public int playerLimit = -1;

    [Space]
    public LoadMode loadMode = LoadMode.NONE;

    [SerializeField] private bool overrideMapPick = false;
    public KartLevel map = KartLevel.TEST_TRACK;

    [Header("Networking settings")]
    public bool haveStandalonePlayerRunAsServer = false;
    [SerializeField] private bool manualLobbyPlayerWaitSwitch = false;


    [Header("Race Settings Overrides")]
    [SerializeField] private bool overrideRaceProgressAtStart = false;
    public float raceProgress = 0;

    [SerializeField] private bool overrideLapCount = false;
    public int lapCount = -1;
    [SerializeField] private bool overrideBots = false;
    public bool bots = false;

    /* Public boolean accessors */
    public bool Enable { get { return masterEnable && GameVersion.IsDevelopment; } }
    public bool ManualLobbyPlayerWaitSwitch { get { return Enable && manualLobbyPlayerWaitSwitch; } }
    public bool OverridePlayerLimit { get { return Enable && overridePlayerLimit; } }
    public bool OverrideMapPick { get { return Enable && overrideMapPick; } }
    public bool OverrideRaceProgressAtStart { get { return Enable && overrideRaceProgressAtStart; } }
    public bool OverrideLapCount { get { return Enable && overrideLapCount; } }
    public bool OverrideBots { get { return Enable && overrideBots; } }

    private void Start() 
    {
        if(OverridePlayerLimit && playerLimit <= 0)
            Debug.LogWarning($"DevSettings: Max players is being overridden but the new value is <= 0, this is not recommended.");

        if(OverrideLapCount && lapCount <= 0)
            Debug.LogWarning($"DevSettings: Lap count is being overridden but the new value is <= 0, this is not recommended.");

        PrintDevSettings("#ffff99");

        if(haveStandalonePlayerRunAsServer && !Application.isEditor) {
            NetworkStateManager nsm = InstanceFinder.NetworkManager.GetComponent<NetworkStateManager>();
            nsm.StartServer();
            return;
        }

        if(loadMode != LoadMode.NONE)
            SimulateLoad();
    }

    /// <summary>
    /// When loadMode != LoadMode.NONE this will load the user into a map either in local play or multiplayer depending on load mode.
    /// For multiplayer load mode, there must be a server instance running to recieve the player.
    /// </summary>
    public void SimulateLoad() 
    {
        if(loadMode == LoadMode.NONE) {
            Debug.LogError("Tried to simulate load but the LoadMode was set to NONE.");
            return;
        }

        CoreManager.Instance.isMultiplayer = loadMode == LoadMode.LOAD_LOBBY;

        if(loadMode == LoadMode.LOAD_LOBBY) {
            CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);
        } else if(loadMode == LoadMode.LOAD_MAP_LOCAL) {
            KartLevel mapPick = OverrideMapPick ? map : GameLobby.PickKartLevel();
            if(!OverrideMapPick) 
                Debug.Log($"<color=green>SimulateLoad: loading into local play map but override map pick is off, picked {mapPick} randomly.</color>");
            LevelAtlas la = atlasesPrefab.GetComponent<LevelAtlas>();
            CoreManager.TransitionManager.LoadScene(la.RetrieveData(mapPick).sceneName);
        }
    }

    public void PrintDevSettings(string color) 
    {
        string headerMessage = $"Dev settings are " + (Enable ? "enabled." : "disabled.");
        if(!GameVersion.IsDevelopment)
            headerMessage += " Not in a development version.";
        if(!masterEnable)
            headerMessage += " Master enable is turned off";

        Debug.Log($"<color={color}>{headerMessage}</color>");
        
        if(!Enable) return;

        List<string> devSettings = new();

        if(ManualLobbyPlayerWaitSwitch)
            devSettings.Add("Manual lobby player switch active. Press F4 in game lobby on server instance to pick map.");
        if(OverridePlayerLimit)
            devSettings.Add($"Overriding player limit. New player limit: {playerLimit}");
        if(OverrideMapPick)
            devSettings.Add($"Overriding map pick. New map pick: {map}");
        if(OverrideRaceProgressAtStart)
            devSettings.Add($"Overriding race progress at start. Initial race progress: {raceProgress}");
        if(OverrideLapCount)
            devSettings.Add($"Overriding lap count. New lap count: {lapCount}");
        if(OverrideBots)
            devSettings.Add($"Overriding bots. Are bots enabled: {bots}");

        devSettings.ForEach(s => Debug.Log($"<color={color}> - {s}</color>"));
    }

}

public enum LoadMode {
    NONE, LOAD_LOBBY, LOAD_MAP_LOCAL
}