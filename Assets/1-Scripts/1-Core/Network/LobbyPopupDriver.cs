using System;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// Uses the MenuController's popup menu system to provide important lobby messages as popups.
/// </summary>
public class LobbyPopupDriver : MonoBehaviour
{
    
    private void Awake() 
    {
        CoreManager.LobbyCommunicator.LobbyLeftEvent += LobbyCommunicator_LobbyLeftEvent;
        CoreManager.LobbyCommunicator.LobbyMessageEvent += LobbyCommunicator_LobbyMessageEvent;
    }

    private void OnDestroy() 
    {
        CoreManager.LobbyCommunicator.LobbyLeftEvent -= LobbyCommunicator_LobbyLeftEvent;
        CoreManager.LobbyCommunicator.LobbyMessageEvent -= LobbyCommunicator_LobbyMessageEvent;
    }

    private void LobbyCommunicator_LobbyLeftEvent(string lobbyID, string reason)
    {
        PopupMenuController.Instance.Open(PopupMenuController.POPUP_GROUP_ID_SINGLE_CONFIRM, "Disconnected from lobby", reason);
    }

    private void LobbyCommunicator_LobbyMessageEvent(string lobbyID, NetworkConnection sender, LobbyMessageType type, string message) 
    {
        BLog.Highlight("Recieved message: " + type + ": " + message);
        if(type == LobbyMessageType.ACTION && message.StartsWith(LobbyManager.LME_CMD_FORCE_DISCONNECT)) {
            string reason = message.Replace(LobbyManager.LME_CMD_FORCE_DISCONNECT, "");
            PopupMenuController.Instance.Open(PopupMenuController.POPUP_GROUP_ID_SINGLE_CONFIRM, "Disconnected from lobby", reason);
        }
    } 

}