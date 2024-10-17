using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.Networking;
using EMullen.SceneMgmt;
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
    /// <summary>
    /// The amount of menu elements that have players with finished races
    /// </summary>
    private int populatedMenuElements; 

    private PlayerControls controlsReference;

    protected new void Awake() 
    {        
        base.Awake();

        controlsReference = new();

        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

	protected override void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
	{
		if(gameplayManager == null)
			return;
            
		if(context.performed && context.action.name == controlsReference.UI.Submit.name) {
			if(!gameplayManager.HasLobby) {
                Debug.LogError("GameplayManager doesn't have lobby, so we can't send them back to it.");
                LobbyCommunicator.Instance.StopCommunication("Lost connection with lobby.");
                SceneController.Instance.LoadScene(new(SceneNames.MENU_TITLE), false);
                return;
            }
            LobbyManager.Instance.SendLobbyMessage(LobbyCommunicator.Instance.LobbyID, LobbyMessageType.ACTION, LobbyManager.LME_CMD_REQUEST_LOBBY_MOVE, sender: InstanceFinder.ClientManager.Connection, sendOnlyToServer: true);
        }
	}

    public void ShowResults() 
    {
        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        SyncDictionary<string, RacePlacementData> placements = gameplayManager.RaceManager.GetPlacements();
        BLog.Highlight("Placements size: " + placements.Count);
        for(int position = 0; position < gameplayManager.KartsIRManager.KartCount; position++) {
            BLog.Highlight($"Calculating position {position}");
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

            KartManager manager = gameplayManager.KartsIRManager.SearchForKartManager(playerUUID);
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
