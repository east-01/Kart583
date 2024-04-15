using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.UTP;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the current state of the network and should persist between scene changes.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(TransportManager))]
[RequireComponent(typeof(Tugboat))]
[RequireComponent(typeof(FishyUnityTransport))]
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

        // Switch transport between FishyUnityTransport and Tugboat
        GetComponent<TransportManager>().Transport = GameVersion.IsDevelopment ? GetComponent<Tugboat>() : GetComponent<FishyUnityTransport>();

    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F2) && Input.GetKey(KeyCode.LeftShift)) {
            print("Pressed server toggle.");
            if (_serverConnectionState != LocalConnectionState.Stopped)
                _networkManager.ServerManager.StopConnection(true);
            else
                _networkManager.ServerManager.StartConnection();
        }
    }

    public void StartClient() 
    {
        if(ServerConnectionState != LocalConnectionState.Stopped) {
            Debug.LogError("Can't start client when server is active.");
            return;
        }

        if(ClientConnectionState != LocalConnectionState.Stopped) {
            Debug.LogWarning("Ignoring StartClient call. Client is already started.");
            return;            
        }

        _networkManager.ClientManager.StartConnection();
    }

    public void StopClient() 
    {
        if(ClientConnectionState == LocalConnectionState.Stopped)
            return;

        _networkManager.ClientManager.StopConnection();
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
        if(_clientConnectionState != LocalConnectionState.Stopped) {
            clientStatusText.gameObject.SetActive(true);
            clientStatusText.text = "Client: " + _clientConnectionState;
        } else {
            clientStatusText.gameObject.SetActive(false);
        }
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        _serverConnectionState = args.ConnectionState;
        if(_serverConnectionState != LocalConnectionState.Stopped) {
            serverStatusText.gameObject.SetActive(true);
            serverStatusText.text = "Server: " + _serverConnectionState;
        } else {
            serverStatusText.gameObject.SetActive(false);
        }
    }

}
