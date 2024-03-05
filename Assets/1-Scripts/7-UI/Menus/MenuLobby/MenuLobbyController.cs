using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Interfaces with the network manager to join lobbies, show players.
/// Acts as a backend, actual UI stuff happens in MenuLobbyViewController.
/// Takes inspiration from the Fishnet NetworkHudCanvases class.
/// </summary>
[RequireComponent(typeof(MenuLobbyViewController))]
public class MenuLobbyController : MonoBehaviour
{

    private NetworkManager _networkManager;
    private LocalConnectionState _serverConnectionState;
    private LocalConnectionState _clientConnectionState;

    public LocalConnectionState ServerConnectionState { get { return _serverConnectionState; }}
    public LocalConnectionState ClientConnectionState { get { return _clientConnectionState; }}

    private MenuLobbyViewController _viewController;

    private void Start() 
    {

        _networkManager = InstanceFinder.NetworkManager;
        if(_networkManager == null) {
            Debug.LogError("Failed to find NetworkManager.");
            return;
        }
                    
        _viewController = GetComponent<MenuLobbyViewController>();

        _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

    }

    private void Update() 
    {
        if(Input.GetKeyDown(KeyCode.F10)) {
            StartAsClient();
        }
    }

    private void OnDestroy()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

    /// <summary>
    /// Callback from ui button that starts the menu as a server.
    /// Temporary. In prod, these will automatically be called based on build type
    /// </summary>
    public void StartAsServer() 
    {
        if(_networkManager == null)
            return;

        if (_serverConnectionState != LocalConnectionState.Stopped)
            _networkManager.ServerManager.StopConnection(true);
        else
            _networkManager.ServerManager.StartConnection();
    }

    /// <summary>
    /// Callback from ui button that starts the menu as a client.
    /// Temporary. In prod, these will automatically be called based on build type
    /// </summary>
    public void StartAsClient() 
    {
        if(_networkManager == null)
            return;

        if (_clientConnectionState != LocalConnectionState.Stopped)
            _networkManager.ClientManager.StopConnection();
        else
            _networkManager.ClientManager.StartConnection();
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        _clientConnectionState = args.ConnectionState;
        _viewController.UpdateView();
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        _serverConnectionState = args.ConnectionState;
        _viewController.UpdateView();
    }
}
