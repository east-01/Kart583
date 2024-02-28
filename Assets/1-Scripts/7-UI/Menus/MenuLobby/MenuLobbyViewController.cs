using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FishNet.Transporting;

/// <summary>
/// Communicates with the MenuLobbyController to display whats going on
/// </summary>
public class MenuLobbyViewController : MonoBehaviour
{

    private MenuLobbyController _controller;

    [SerializeField]
    private GameObject connectionButtons;
    [SerializeField]
    private TMP_Text lobbyStatusText;

    private void Start() 
    {
        _controller = GetComponent<MenuLobbyController>();
    }

    public void UpdateView() 
    {
        // Connection buttons
        bool showConnectionButtons = _controller.ServerConnectionState == LocalConnectionState.Stopped && 
                                     _controller.ClientConnectionState == LocalConnectionState.Stopped 
                                     /*&& TODO: Editor field determining build type*/;
        connectionButtons.SetActive(showConnectionButtons);

        // Status text
        string lobbyStatusText = "";
        if(_controller.ServerConnectionState != LocalConnectionState.Stopped)
            lobbyStatusText = "[Server - " + _controller.ServerConnectionState + "]";
        else if(_controller.ClientConnectionState != LocalConnectionState.Stopped)
            lobbyStatusText = "[Client - " + _controller.ClientConnectionState + "]";
        this.lobbyStatusText.text = lobbyStatusText;
    }
}
