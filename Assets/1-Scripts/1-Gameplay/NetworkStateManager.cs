using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the current state of the network and should persist between scene changes.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class NetworkStateManager : MonoBehaviour
{

    private NetworkManager _networkManager;
    private LocalConnectionState _serverConnectionState;
    private LocalConnectionState _clientConnectionState;

    public LocalConnectionState ServerConnectionState { get { return _serverConnectionState; }}
    public LocalConnectionState ClientConnectionState { get { return _clientConnectionState; }}

    [SerializeField]
    private GameObject debugCanvas;
    [SerializeField]
    private TMP_Text clientStatusText;
    [SerializeField]
    private TMP_Text serverStatusText;

    void Start() 
    {
        _networkManager = GetComponent<NetworkManager>();

        _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F2) && Input.GetKey(KeyCode.LeftShift)) {
            print("Pressed server toggle.");
            if (_serverConnectionState != LocalConnectionState.Stopped)
                _networkManager.ServerManager.StopConnection(true);
            else
                _networkManager.ServerManager.StartConnection();
        }

        if(Input.GetKeyDown(KeyCode.F1) && Input.GetKey(KeyCode.LeftShift)) {
            print("Pressed client toggle.");
            if (_clientConnectionState != LocalConnectionState.Stopped)
                _networkManager.ClientManager.StopConnection();
            else
                _networkManager.ClientManager.StartConnection();
        }
    }

    private void OnDestroy()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        _clientConnectionState = args.ConnectionState;
        clientStatusText.text = "Client: " + _clientConnectionState;
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        _serverConnectionState = args.ConnectionState;
        serverStatusText.text = "Server: " + _serverConnectionState;
    }

}
