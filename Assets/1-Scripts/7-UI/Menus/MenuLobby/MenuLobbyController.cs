using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Interfaces with the network manager to join lobbies, show players.
/// Acts as a backend, actual UI stuff happens in MenuLobbyViewController.
/// </summary>
[RequireComponent(typeof(MenuLobbyViewController))]
public class MenuLobbyController : MonoBehaviour
{

    private MenuLobbyViewController _viewController;
    private NetworkManager networkManager; // The networkmanager that this menu is connected to.

    private void Start() 
    {

        _viewController = GetComponent<MenuLobbyViewController>();

        networkManager = InstanceFinder.NetworkManager;
        if(networkManager == null) {
            Debug.LogError("MenuLobbyController failed to connect to a NetworkManager.");
            return;
        }

        networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

    }

    private void Update() {
        if(PlayerObjectManager.Instance == null)
            Debug.LogWarning("PlayerObjectManager instance is null!");
        // Ensure client has input
        if(InstanceFinder.IsClient) {
            if(PlayerObjectManager.Instance.PlayerObjectCount == 0 && !PlayerObjectManager.Instance.InputPromptActive) {
                PlayerObjectManager.Instance.PromptForInput();
            } else if(PlayerObjectManager.Instance.PlayerObjectCount > 0 && PlayerObjectManager.Instance.InputPromptActive) {
                PlayerObjectManager.Instance.ClearInputPrompt();
            }
        }
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        _viewController.UpdateView();
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        _viewController.UpdateView();
    }

    public NetworkManager ConnectedNetworkManager { get { return networkManager; } }

}
