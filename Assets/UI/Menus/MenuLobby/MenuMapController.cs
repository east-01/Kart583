using System.Collections.Generic;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/** This class is responsible for the overarching operation of the map select menu */
public class MenuMapController : MenuController
{
    
    protected new void Awake() 
    {
        // We should load the map icons first so we can utilize MenuController's firstSelect feature
        MapSelectBuilder builder = GetComponent<MapSelectBuilder>();
        builder.ReloadMenu();  
        firstSelect = builder.MenuElements[0].GetComponent<Button>();

        base.Awake();
    }

    public void ClickedMapIcon(KartLevel level) 
    {   
        if(CoreManager.IsLocal) {
            string lobbyID = LobbyCommunicator.Instance.LobbyID;
            BLog.Highlight($"id: \"{lobbyID}\"");
            GameLobby localLobby = LobbyManager.Instance.GetLobby(LobbyCommunicator.Instance.LobbyID);
            BLog.Highlight($"local lob: \"{localLobby}\"");
            if(localLobby is not KartLobby) {
                Debug.LogError("Can't load level, localLobby is not a KartLobby.");
                return;
            }
            KartLobby localKartLobby = localLobby as KartLobby;
            localKartLobby.SetLevel(level);
        } else {
            // TODO: Map voting
            BLog.Highlight("TODO: Map voting");
        }
    }

    public override void SendMenuBack() => ParentMenu.SendMenuBack();

}