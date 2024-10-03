using System.Collections.Generic;
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
            GameLobby localLobby = LobbyManager.Instance.GetLobby(CoreManager.LobbyCommunicator.LobbyID);
            localLobby.SetLevel(level);
        } else {
            // TODO: Map voting
        }
    }

    protected override void SendMenuBack()
    {
        parentMenu.SendMenuBackPublic();
    }

}