using System.Linq;
using EMullen.MenuController;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

/** The PlayerMenuController will interface between the LocalPlayerManager and
      the child PlayerPanelControllers */
public class MenuPlayerController : MenuController
{

    [SerializeField] private GameObject playerPanelPrefab;

    [SerializeField] private GameObject playerPanelContainer;
    [SerializeField] private GameObject joinMessage;

    [SerializeField] private int maxPlayers;

    private bool shouldAllowJoining;

    protected new void Start() 
    {
        base.Start();
        
        allowInputEvents = false;
        shouldAllowJoining = true;
        maxPlayers = CoreManager.IsMultiplayer ? 1 : 4;

        // Spawn player menus for ppl already in the player input manager
        PlayerManager pm = PlayerManager.Instance;
        if(pm.PlayerCount > 0)
            foreach(LocalPlayer obj in pm.LocalPlayers) {
                if(obj != null)
                    AddPanel(obj);
            }
    }

    protected new void OnDestroy() 
    {
        base.OnDestroy();
        PlayerManager.Instance.LocalPlayerJoinedEvent -= LocalPlayerManager_PlayerJoinedEvent;
    }

    protected new void OnEnable() 
    {
        base.OnEnable();
        PlayerManager.Instance.LocalPlayerJoinedEvent += LocalPlayerManager_PlayerJoinedEvent;
        PlayerManager.Instance.LocalPlayerLeftEvent += LocalPlayerManager_PlayerLeftEvent;
    }

    protected new void OnDisable() 
    {
        base.OnDisable();
        PlayerManager.Instance.LocalPlayerJoinedEvent -= LocalPlayerManager_PlayerJoinedEvent;
        PlayerManager.Instance.LocalPlayerLeftEvent -= LocalPlayerManager_PlayerLeftEvent;
    }

    private void Update() 
    {
        // BLog.Highlight("Should allow joining: " + shouldAllowJoining);
        PlayerInputManager pim = PlayerManager.Instance.PlayerInputManager;
        if(!pim.joiningEnabled && shouldAllowJoining)
            pim.EnableJoining();
        else if(pim.joiningEnabled && !shouldAllowJoining)
            pim.DisableJoining();
    }

#region Panel management
    private void AddPanel(LocalPlayer lp) 
    {
        // Spawn player panel
        GameObject playerPanel = Instantiate(playerPanelPrefab, playerPanelContainer.transform);
        PlayerPanelController playerPanelController = playerPanel.GetComponent<PlayerPanelController>();
        playerPanelController.Open(lp);

        UpdatePanels();
    }

    public void RemovePanel(LocalPlayer obj, bool removePlayerInput = true) 
    {
        if(obj.Input.playerIndex == 0) {
            SendMenuBack();
            return;
        }

        // Loop through child panels, if one of them has the player we're removing we remove it
        // This means if we make it all the way through the loop we didn't find target player
        for(int i = 0; i < playerPanelContainer.transform.childCount; i++) {
            GameObject child = playerPanelContainer.transform.GetChild(i).gameObject;
            PlayerPanelController ppc = child.GetComponentInChildren<PlayerPanelController>();
            if(ppc == null)
                continue;

            if(ppc.FocusedPlayerIncludingChildren.Input.playerIndex == obj.Input.playerIndex) {
                RemovePanel(ppc, removePlayerInput);
                return; // Call return so error message isn't shown
            }
        }

        Debug.LogError("Failed to find target panel for LocalPlayer " + obj);
    }

    public void RemovePanel(PlayerPanelController ppc, bool removePlayerInput = true) 
    {
        LocalPlayer focusedPlayer = ppc.FocusedPlayerIncludingChildren;
        Destroy(ppc.gameObject);

        if(removePlayerInput)
            PlayerManager.Instance.RemovePlayer(focusedPlayer);
    }

    /// <summary>
    /// Moves PlayerPanels to where they belong and manages the joinMessage.
    /// If there's only one playerpanel, it should take up the whole screen
    /// </summary>
    public void UpdatePanels() 
    {
        joinMessage.transform.SetAsLastSibling();

        // Ensure the join message stays at the end and disable once we reach max
        if(playerPanelContainer.transform.childCount > maxPlayers) {
            joinMessage.SetActive(false);
            PlayerManager.Instance.PlayerInputManager.DisableJoining();

            if(maxPlayers == 1) {
                // Double scale so player's panel fills screen
                GameObject playerPanel = playerPanelContainer.transform.GetChild(0).gameObject;
                playerPanel.GetComponent<RectTransform>().SetScale(new Vector3(2, 2, 1));
            }
        }
    }
#endregion

#region Event handlers
    private void LocalPlayerManager_PlayerJoinedEvent(LocalPlayer obj) 
    {
        AddPanel(obj);
    }

    private void LocalPlayerManager_PlayerLeftEvent(LocalPlayer obj) 
    {

    }   
#endregion

    /** Check if everyone's ready, if so, transition to map select. */
    public void CheckReady() 
    {
        if(!PlayerManager.Instance.LocalPlayers.Where(po => po != null).All(po => po.GetPlayerData().IsReady())) return;

        shouldAllowJoining = false;

        LobbyCommunicator.Instance.StartCommunication();
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);
    }

    /// <summary>
    /// Called by RemovePanel when the player one panel gets removed
    /// </summary>
    public override void SendMenuBack() 
    {
        for(int i = 0; i < playerPanelContainer.transform.childCount; i++) {
            GameObject child = playerPanelContainer.transform.GetChild(i).gameObject;
            PlayerPanelController ppc = child.GetComponentInChildren<PlayerPanelController>();
            if(ppc == null)
                continue;
            
            if(ppc.FocusedPlayerIncludingChildren.Input.playerIndex == 0)
                continue;

            RemovePanel(ppc, true);
        }
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_TITLE);
    }

}
