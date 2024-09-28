using System.Linq;
using GameKit.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

/** The PlayerMenuController will interface between the PlayerObjectManager and
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
        PlayerObjectManager pom = PlayerObjectManager.Instance;
        if(pom.PlayerObjectCount > 0)
            foreach(PlayerObject obj in pom.PlayerObjects) {
                AddPanel(obj);
            }
    }

    protected new void OnDestroy() 
    {
        base.OnDestroy();
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerJoinedEvent;
    }

    protected new void OnEnable() 
    {
        base.OnEnable();
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerJoinedEvent;
        PlayerObjectManager.Instance.PlayerObjectLeftEvent += PlayerObjectManager_PlayerLeftEvent;
    }

    protected new void OnDisable() 
    {
        base.OnDisable();
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerJoinedEvent;
        PlayerObjectManager.Instance.PlayerObjectLeftEvent -= PlayerObjectManager_PlayerLeftEvent;
    }

    private void Update() 
    {
        // BLog.Highlight("Should allow joining: " + shouldAllowJoining);
        PlayerInputManager pim = PlayerObjectManager.Instance.PlayerInputManager;
        if(!pim.joiningEnabled && shouldAllowJoining)
            pim.EnableJoining();
        else if(pim.joiningEnabled && !shouldAllowJoining)
            pim.DisableJoining();
    }

#region Panel management
    private void AddPanel(PlayerObject obj) 
    {
        // Spawn player panel
        GameObject playerPanel = Instantiate(playerPanelPrefab, playerPanelContainer.transform);
        PlayerPanelController playerPanelController = playerPanel.GetComponent<PlayerPanelController>();
        playerPanelController.Open(obj);

        UpdatePanels();
    }

    public void RemovePanel(PlayerObject obj, bool removePlayerInput = true) 
    {
        if(obj.PlayerIndex == 0) {
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

            if(ppc.FocusedPlayerIncludingChildren.PlayerIndex == obj.PlayerIndex) {
                RemovePanel(ppc, removePlayerInput);
                return; // Call return so error message isn't shown
            }
        }

        Debug.LogError("Failed to find target panel for PlayerObject " + obj);
    }

    public void RemovePanel(PlayerPanelController ppc, bool removePlayerInput = true) 
    {
        PlayerObject focusedPlayer = ppc.FocusedPlayerIncludingChildren;
        Destroy(ppc.gameObject);

        if(removePlayerInput)
            PlayerObjectManager.Instance.RemovePlayer(focusedPlayer);
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
            PlayerObjectManager.Instance.PlayerInputManager.DisableJoining();

            if(maxPlayers == 1) {
                // Double scale so player's panel fills screen
                GameObject playerPanel = playerPanelContainer.transform.GetChild(0).gameObject;
                playerPanel.GetComponent<RectTransform>().SetScale(new Vector3(2, 2, 1));
            }
        }
    }
#endregion

#region Event handlers
    private void PlayerObjectManager_PlayerJoinedEvent(PlayerObject obj) 
    {
        AddPanel(obj);
    }

    private void PlayerObjectManager_PlayerLeftEvent(PlayerObject obj) 
    {

    }   
#endregion

    /** Check if everyone's ready, if so, transition to map select. */
    public void CheckReady() 
    {
        if(!PlayerObjectManager.Instance.PlayerObjects.All(po => po.data.ready)) return;

        shouldAllowJoining = false;

        CoreManager.LobbyCommunicator.StartCommunication();
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);
    }

    /// <summary>
    /// Called by RemovePanel when the player one panel gets removed
    /// </summary>
    protected override void SendMenuBack() 
    {
        for(int i = 0; i < playerPanelContainer.transform.childCount; i++) {
            GameObject child = playerPanelContainer.transform.GetChild(i).gameObject;
            PlayerPanelController ppc = child.GetComponentInChildren<PlayerPanelController>();
            if(ppc == null)
                continue;
            
            if(ppc.FocusedPlayerIncludingChildren.PlayerIndex == 0)
                continue;

            RemovePanel(ppc, true);
        }
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_TITLE);
    }

}
