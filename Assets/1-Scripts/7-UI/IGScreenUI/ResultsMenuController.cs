using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

/** This class will build the results rows out of the ResultRow prefabs. */
public class ResultsMenuController : MenuController, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    public GameObject placementRowPrefab;
    public RectTransform resultsContainer;

    private List<GameObject> menuElements;
    public bool waitingForPlacements = false;

    protected new void Awake() 
    {        
        base.Awake();

        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

    void Update() {
        if(gameplayManager == null)
            return;

        if(waitingForPlacements && gameplayManager.RaceManager.GetPlacements().Count == gameplayManager.PlayerManager.KartCount) {
            waitingForPlacements = false;
            ShowResults();
        }
    }

	protected override void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
	{
		if(gameplayManager == null)
			return;
		if(context.performed && context.action.name == controlsReference.UI.Submit.name) {
			if(!gameplayManager.HasLobby) {
                Debug.LogError("GameplayManager doesn't have lobby, so we can't send them back to it.");
                CoreManager.LobbyCommunicator.StopCommunication();
                SceneController.Instance.LoadScene(new(SceneNames.MENU_TITLE), false);
                return;
            }
            if(CoreManager.IsMultiplayer) {
                NetSceneController.LobbyManager.ServerRpcSendLobbyMessage(CoreManager.LobbyCommunicator.LobbyID, CoreManager.LocalConnection, LobbyMessageType.ACTION, LobbyManager.LME_CMD_REQUEST_LOBBY_MOVE);
			} else {
                NetSceneController.LobbyManager.SendLobbyMessage(CoreManager.LobbyCommunicator.LobbyID, CoreManager.LocalConnection, LobbyMessageType.ACTION, LobbyManager.LME_CMD_REQUEST_LOBBY_MOVE);
			}
        }
	}

    public void ShowResults() 
    {
        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        SyncDictionary<string, RacePlacementData> placements = gameplayManager.RaceManager.GetPlacements();
        for(int position = 0; position < gameplayManager.PlayerManager.KartCount; position++) {
            string playerUUID = null;
            // Find playerUUID from position
            foreach(string testUUID in placements.Keys) {
                if(placements[testUUID].position == position) {
                    playerUUID = testUUID;
                    break;
                }
            }

            if(playerUUID == null) {
                Debug.LogError($"ResultsBuilder failed to find UUID from position " + position);
                continue;
            }

            KartManager manager = gameplayManager.PlayerManager.SearchForKartManager(playerUUID);
            if(manager == null) {
                Debug.LogError($"Couldn't locate KartManager from uuid \"{playerUUID}\"");
                continue;
            }

            RacePlacementData racePlacementData = placements[playerUUID];                
            GameObject newObj = Instantiate(placementRowPrefab, resultsContainer);
            newObj.GetComponent<PlacementRow>().UpdateVisuals(manager, racePlacementData);
            menuElements.Add(newObj);
        }
    }

    public bool ResultsShown { get { return gameObject.activeSelf; } }
}
