using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.SceneMgmt;
using UnityEngine;

[DefaultExecutionOrder(1)]
public class DBNetworkConfigurator : MonoBehaviour {

    public static DBNetworkConfigurator Instance { get; private set; }

    [SerializeField]
    private BLogChannel logSettings;

    public static bool StandaloneServer { get; private set; }
    private bool configuredLobbyManager = false;

    private void Awake() 
    {
        if(Instance != null) {
            Debug.LogError($"Tried to instantiate new DBNetworkConfigurator when one already exists, deleting gameObject \"{gameObject.name}\"");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update() 
    {
        if(LobbyManager.Instance == null) 
            configuredLobbyManager = false;
     
        if(!configuredLobbyManager && LobbyManager.Instance != null) {
            configuredLobbyManager = true;
 
            LobbyManager.Instance.InstantiateLobbyAction = CreateKartLobby;
            BLog.Log("Configured LobbyManager", logSettings);
        }
    }

    public GameLobby CreateKartLobby() => new KartLobby();

    public static void ConfigureNetwork(bool runAsServer) 
    {
        string[,] optionMatrix = new string[,] { 
            {"LocalServer", "LocalClient", "LocalHost"}, // If using dev preview, or running local lobby
            {"WANServer", "WANClient", "null"}
        };

        bool useLocalNet = CoreManager.IsLocal || DevSettings.IsDevelopment;
        string debugOut = "Configured " + (useLocalNet ? "LAN" : "WAN") + " ";
        int y = useLocalNet ? 0 : 1;
        int x;
        
        if(CoreManager.IsLocal) {
            x = 2;
            debugOut += "host";
            StandaloneServer = false;
        } else if(runAsServer) {
            x = 0;
            debugOut += "server";
            StandaloneServer = true;
        } else {
            x = 1;
            debugOut += "client";
            StandaloneServer = false;
        }

        BLog.Log(debugOut, Instance.logSettings);

        NetworkController.Instance.NetworkConfig = NetworkController.Instance.GetNetworkConfiguration(optionMatrix[y, x]);        
    }
}