using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// The LobbyMessagePopup is an addon component to the lobby system that displays messages as a
///   popup when the LobbyCommunicator recieves a server message.
/// It requires the MenuController system.
/// </summary>
public class LobbyMessagePopup : MenuController
{
    
    protected new void OnEnable() 
    {
        base.OnEnable();
        CoreManager.LobbyCommunicator.LobbyMessageEvent += LobbyCommunicator_LobbyMessageEvent;
    }

    protected new void OnDisable() 
    {
        base.OnDisable();
        CoreManager.LobbyCommunicator.LobbyMessageEvent -= LobbyCommunicator_LobbyMessageEvent;
    }

    private void LobbyCommunicator_LobbyMessageEvent(string lobbyID, NetworkConnection sender, LobbyMessageType type, string message) 
    {
        if(type == LobbyMessageType.ACTION && message.StartsWith(LobbyManager.LME_CMD_FORCE_DISCONNECT)) {
            string reason = message.Replace(LobbyManager.LME_CMD_FORCE_DISCONNECT, "");
        }
    } 

}
