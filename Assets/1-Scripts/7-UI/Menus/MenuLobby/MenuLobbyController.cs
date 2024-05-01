using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Interfaces with the network manager to join lobbies, show players.
/// Acts as a backend, actual UI stuff happens in MenuLobbyViewController.
/// </summary>
[RequireComponent(typeof(MenuLobbyViewController))]
public class MenuLobbyController : MenuController
{

    public static readonly string SUB_MENU_MAP_SELECT = "MapSelect";

    private MenuLobbyViewController _viewController;
    private NetworkManager networkManager; // The networkmanager that this menu is connected to.
    private NetworkStateManager networkStateManager;

    private void Start() 
    {

        _viewController = GetComponent<MenuLobbyViewController>();

        networkManager = InstanceFinder.NetworkManager;
        if(networkManager == null) {
            Debug.LogError("MenuLobbyController failed to connect to a NetworkManager.");
            return;
        }
        networkStateManager = networkManager.GetComponent<NetworkStateManager>();

        networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

        OpenSubMenu(SUB_MENU_MAP_SELECT);
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


        if(GameVersion.IsDevelopment && Input.GetKeyDown(GameLobby.FORCE_MAP_PICK_KEY))
            SceneDelegate.LobbyManager.RequestForceMapPick();
    }

    private IEnumerator StartConnection() 
    {
        yield return new WaitForSeconds(0.3f);
        if(CoreManager.IsMultiplayer) {
            networkStateManager.StartClient();                
        } else if(CoreManager.IsLocal) {
            networkStateManager.StartHost();
        }
    }

    protected new void OnDestroy()
    {
        base.OnDestroy();
        
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

    protected override void SendMenuBack()
    {
        networkStateManager.StopClient();
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_TITLE);
    }

    public NetworkManager ConnectedNetworkManager { get { return networkManager; } }

}
