using FishNet;
using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.UTP;
using TMPro;
using Unity.VisualScripting;
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
    private TransportManager transportManager;
    private Tugboat tugboat;
    private FishyUnityTransport fishyUnityTransport;

    [SerializeField] private LocalConnectionState _serverConnectionState;
    [SerializeField] private LocalConnectionState _clientConnectionState;

    public LocalConnectionState ServerConnectionState { get { return _serverConnectionState; }}
    public LocalConnectionState ClientConnectionState { get { return _clientConnectionState; }}

    [SerializeField]
    private GameObject debugCanvas;
    [SerializeField]
    private TMP_Text clientStatusText;
    [SerializeField]
    private TMP_Text serverStatusText;

    /// <summary>
    /// Used for communcation started/ended events, the update method checks if the connected 
    /// </summary>
    private bool trackedConnectionStatus = false;
    /// <summary>
    /// Is the client connected
    /// </summary>
    public bool IsConnected => ClientConnectionState == LocalConnectionState.Started && CoreManager.HasLocalConnection;

    public delegate void CommuncationStartedHandler();
    public event CommuncationStartedHandler CommunicationStartedEvent;
    public delegate void CommunicationEndedHandler();
    public event CommunicationEndedHandler CommunicationEndedEvent;

    void Start() 
    {
        _networkManager = GetComponent<NetworkManager>();
        transportManager = GetComponent<TransportManager>();
        tugboat = GetComponent<Tugboat>();
        fishyUnityTransport = GetComponent<FishyUnityTransport>();

        _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

        // Switch transport between FishyUnityTransport and Tugboat
        if(DevSettings.IsDevelopment())
            UseLocalTransport();
        else
            UseGlobalTransport();

        if(DevSettings.Settings.HaveStandalonePlayerRunAsServer && !Application.isEditor) {
            StartServer(true);
            return;
        }

    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F12)) {
            print("Pressed server toggle.");
            if (_serverConnectionState != LocalConnectionState.Stopped)
                StopServer();
            else
                StartServer(true);
        }

        if(trackedConnectionStatus == false && IsConnected) {
            trackedConnectionStatus = true;
            CommunicationStartedEvent?.Invoke();
        } else if(trackedConnectionStatus == true && !IsConnected) {
            trackedConnectionStatus = false;
            CommunicationEndedEvent?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
    }

#region TransportSelection
    public bool IsUsingLocalTransport { get {
        return transportManager.Transport = tugboat;
    } }

    public void UseLocalTransport() { 
        transportManager.Transport = tugboat; 
        tugboat.SetServerBindAddress("", IPAddressType.IPv4);
        tugboat.SetClientAddress("localhost");
        tugboat.SetPort(5000);
    }
    public void UseGlobalTransport() { 
        // transportManager.Transport = fishyUnityTransport; 
        transportManager.Transport = tugboat; 
        tugboat.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        tugboat.SetClientAddress("99.120.146.136");
        tugboat.SetPort(7770);
    }
#endregion

#region Server/Client Start and Stop
    public void StartClient() 
    {
        // if(ServerConnectionState != LocalConnectionState.Stopped) {
        //     Debug.LogError("Can't start client when server is active.");
        //     return;
        // }

        if(ClientConnectionState != LocalConnectionState.Stopped) {
            Debug.LogWarning("Ignoring StartClient call. Client is already started.");
            return;            
        }
        BLog.Log("Starting client connection", LogChannel.NetworkManager);
        _networkManager.ClientManager.StartConnection();
    }

    public void StopClient() 
    {
        if(ClientConnectionState == LocalConnectionState.Stopped)
            return;

        BLog.Log("Stopping client connection", LogChannel.NetworkManager);
        _networkManager.ClientManager.StopConnection();
    }

    public void StartServer(bool isMultiplayer) 
    {
        // if(ClientConnectionState != LocalConnectionState.Stopped) {
        //     Debug.LogError("Can't start server when client is active.");
        //     return;
        // }

        if(ServerConnectionState != LocalConnectionState.Stopped) {
            Debug.LogWarning("Ignoring StartServer call. Server is already started.");
            return;            
        }

        CoreManager.IsMultiplayer = isMultiplayer;

        BLog.Log("Starting server connection", LogChannel.NetworkManager);
        _networkManager.ServerManager.StartConnection();
    }

    public void StopServer() 
    {
        if(ServerConnectionState == LocalConnectionState.Stopped)
            return;

        BLog.Log("Stopping server connection", LogChannel.NetworkManager);
        _networkManager.ServerManager.StopConnection(true);
    }

    public void StartHost() 
    {
        if(ServerConnectionState == LocalConnectionState.Stopped)
            StartServer(false);

        if(ClientConnectionState == LocalConnectionState.Stopped)
            StartClient();
    }

    public void StopHost() 
    {
        if(ServerConnectionState != LocalConnectionState.Stopped)
            StopServer();

        if(ClientConnectionState != LocalConnectionState.Stopped)
            StopClient();
    }
#endregion

#region Events
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
#endregion

}
