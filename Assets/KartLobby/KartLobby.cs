using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.SceneManagement;


public class KartLobby : GameLobby 
{
    public static readonly float PLAYER_WAIT_TIME = 20;
    public static readonly float ROUND_END_TIME = 15;
    public static readonly float MAP_PICK_TIME = 3;
    public static readonly KeyCode FORCE_MAP_PICK_KEY = KeyCode.F4;

    /* Game related */
    internal KartLevel? level;
    public bool forceMapPick = false;
    internal GameplayManager gameplayManager;

    public SceneLookupData MapSceneData { get; private set; }
    public Scene? MapScene { get { 
        if(MapSceneData is null || !NetSceneController.Instance.IsSceneRegistered(MapSceneData))
            return null;
        return NetSceneController.Instance.GetSceneElements(MapSceneData).Scene;
    } }
    
    public bool CanAutoSelectLevel => CoreManager.IsMultiplayer && !DevSettings.Settings.ManualLobbyPlayerWaitSwitch;

    public KartLobby() 
    {
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if(CoreManager.IsLocal && SceneNames.IsMapScene(activeScene)) {
            State = new RacingState(this);
            level = CoreManager.LevelAtlas.SearchEnumBySceneName(activeScene);
        }

        SceneController.Instance.SceneRegisteredEvent += SceneDelegate_SceneRegistered;
        SceneController.Instance.SceneWillDeregisterEvent += SceneDelegate_SceneWillDeregister;
        SceneController.Instance.SceneDeregisteredEvent += SceneDelegate_SceneDeregistered;
    }

    ~KartLobby() 
    {
        SceneController.Instance.SceneRegisteredEvent -= SceneDelegate_SceneRegistered;
        SceneController.Instance.SceneWillDeregisterEvent -= SceneDelegate_SceneWillDeregister;
        SceneController.Instance.SceneDeregisteredEvent -= SceneDelegate_SceneDeregistered;
    }

    public void MovePlayersToLobby() => GLSceneManager.SendAllPlayersToScene(new(SceneNames.MENU_LOBBY), false);
    public void MovePlayersToMap() => GLSceneManager.SendAllPlayersToScene(MapSceneData);

#region Administrative
    /// <summary>
    /// Gets the placements dictionary from the RaceManager and adds the points awarded to each PlayerData.
    /// </summary>
    public void AwardPoints() 
    {
        if(gameplayManager == null) {
            Debug.LogError("Can't award points, the gameplay manager is null.");
            return;
        }
        if(gameplayManager.RaceManager.Phase != RacePhase.FINISHED) {
            Debug.LogError("Can't award points, the RaceManager's phase isn't FINISHED");
            return;
        }

        SyncDictionary<string, RacePlacementData> placements = gameplayManager.RaceManager.GetPlacements();
        for(int i = 0; i < Players.Count; i++) {
            string uid = Players[i];
            PlayerData data = PlayerDataRegistry.Instance.GetPlayerData(uid);
            if(!placements.ContainsKey(data.GetUID())) {
                Debug.LogWarning($"Tried to award points to \"{uid}\" but they aren't in the placements dictionary.");
                continue;
            }
            RaceData rd = data.GetData<RaceData>();
            rd.points += placements[data.GetUID()].pointsAwarded;
            data.SetData(rd);
        }
    }
#endregion

    /// <summary>
    /// Set the level. Will load the corresponding scene on the server.
    /// </summary>
    public void SetLevel(KartLevel level) 
    {   
        if(this.level != null) {
            Debug.LogError("Can't set level, one already exists");
            return;
        }
        BLog.Log($"{MessagePrefix}Picked level {level} and requesting map scene.", LobbyManager.Instance.LogSettingsGameLobby, 0);
        this.level = level;

        SceneLookupData newMapLookupData = new(CoreManager.LevelAtlas.RetrieveData(level).sceneName);
        NetSceneController.Instance.LoadSceneAsServer(newMapLookupData);
    }

    private void RegisterGameplayManager(GameplayManager gm) 
    {
        gameplayManager = gm;
        gameplayManager.SetGameLobby(this);
    }

    private void DeregisterGameplayManager() 
    {
        level = null;

        if(gameplayManager == null) {
            Debug.LogError("Can't deregister GameplayManager because it is null.");
            return;
        }
    }

    public void SceneDelegate_SceneRegistered(SceneLookupData lookupData) 
    {
        if(LobbyManager.Instance.GetOwner(lookupData) != ID) {
            Debug.LogError($"Can't claim GameplayManager for scene \"{lookupData}\" we are not the owners, \"{LobbyManager.Instance.GetOwner(lookupData)}\" is");
            return;
        }

        SceneElements elements = NetSceneController.Instance.GetSceneElements(lookupData);

        if(SceneNames.IsMapScene(lookupData.Name)) {
            MapSceneData = lookupData;

            GameplayManager gameplayManager = elements.GetGameplayManager();
            if(gameplayManager != null) {
                RegisterGameplayManager(gameplayManager);
            } else {
                Debug.LogError("Can't register gameplay manager, it's null.");
                return;
            }
        } else 
            return;

        elements.DeleteOnLastClientRemove = SceneNames.IsMapScene(lookupData.Name);
        NetSceneController.Instance.SetSceneElements(lookupData, elements);

        BLog.Log($"{MessagePrefix}Set scene elements for \"{lookupData}\"", LobbyManager.Instance.LogSettingsGameLobby, 0);
    }

    public void SceneDelegate_SceneWillDeregister(SceneLookupData lookupData) 
    {
        if(lookupData == MapSceneData) {
            DeregisterGameplayManager();
        }
    }

    public void SceneDelegate_SceneDeregistered(SceneLookupData lookupData) 
    {
        if(lookupData == MapSceneData) {
            MapSceneData = null;
        }
    }
        
    // if(PlayerCount == 0) {
    //         NetSceneController.LobbyManager.DeleteLobby(ID);
    //     }

    // private void LobbyStateChanged(LobbyState prev, LobbyState current) 
    // {
    //     if(current == LobbyState.MAP_SELECTION) {
    //         if(level != null)
    //             Debug.LogWarning($"Entering map selection while the level isn't null, still on level \"{level}\"");
    //     }
    // }

        
    // [Serializable]
    // public enum LobbyState 
    // {
    //     WAITING_FOR_PLAYERS, 
    //     MAP_SELECTION, 
    //     RACING, // The lobby is in game
    //     POST_RACE
    // }

    public MenuLobbyController MenuLobbyController { get {
        MenuLobbyController[] lobbyControllers = GameObject.FindObjectsOfType<MenuLobbyController>();
        if(lobbyControllers.Length != 1) {
            Debug.LogError($"Found != 1 MenuLobbyControllers ({lobbyControllers.Length})");
            return null;
        }
        return lobbyControllers[0];
    } }

    // LobbyManager.Instance.ClaimScene(Lobby.ID, lookupData);

    //         if(SceneNames.IsMapScene(lookupData.Name)) {
    //             mapSceneData = lookupData;

    //             GameplayManager gameplayManager = elements.GameplayManager;
    //             if(gameplayManager != null) {
    //                 RegisterGameplayManager(gameplayManager);
    //             } else {
    //                 Debug.LogError("Can't register gameplay manager, it's null.");
    //                 return;
    //             }
    //         } else 
    //             return;

    // elements.DeleteOnLastClientRemove = SceneNames.IsMapScene(lookupData.Name);

    //     if(lookupData == mapSceneData) {
    //     DeregisterGameplayManager();
    // }

    // BLog.Highlight("Recieved message: " + message);
    // if(type == LobbyMessageType.ACTION) {
    //     switch(message) {
    //         case LME_CMD_REQUEST_FORCE_MAP_PICK:
    //             if(!IsServerInitialized)
    //                 return;

    //             // if(!DevSettings.IsDevelopment()) {
    //             //     Debug.LogWarning("Can't force map pick. We're not in a development build.");
    //             //     return;
    //             // }
    //             GameLobby lobby = GetLobby(sender);
    //             if(lobby == null) {
    //                 Debug.LogError("Can't force map pick, client is not in a lobby.");
    //                 return;
    //             }

    //             lobby.State = LobbyState.MAP_SELECTION;
    //             lobby.forceMapPick = true;
    //             break;
    //         case LME_CMD_REQUEST_LOBBY_MOVE:
    //             if(!IsServerInitialized)
    //                 return;

    //             if(sender == null) {
    //                 Debug.LogError("Can't handle request lobby move, sender is null.");
    //                 return;
    //             }

    //             lobby = GetLobby(sender);
    //             if(lobby == null) {
    //                 Debug.LogError("Can't move client to lobby, they are not in one.");
    //                 return;
    //             }
    //             BLog.Log($"Client \"{sender}\" requested to move to lobby", logSettings, 0);
    //             if(IsHostStarted)
    //                 SceneController.Instance.LoadScene(new(lobbySceneName), false);
    //             else
    //                 NetSceneController.Instance.TargetRpcLoadScene(sender, new(lobbySceneName), false);

    //             break;
    //     }
    // }

        

}

public abstract class KartLobbyState : LobbyState
{
    protected KartLobby kartLobby => gameLobby as KartLobby;

    protected KartLobbyState(KartLobby kartLobby) : base(kartLobby) {}
}