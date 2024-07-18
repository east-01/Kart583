using System.Linq;
using GameKit.Utilities;
using UnityEngine;

/** The PlayerMenuController will interface between the PlayerObjectManager and
      the child PlayerPanelControllers */
public class MenuPlayerController : MenuController
{

    [SerializeField] private GameObject playerPanelPrefab;

    [SerializeField] private GameObject playerPanelContainer;
    [SerializeField] private GameObject joinMessage;

    [SerializeField] private int maxPlayers;

    void Start() 
    {
        allowInputEvents = false;

        PlayerObjectManager pom = PlayerObjectManager.Instance;

        pom.GetPlayerInputManager().EnableJoining();
        pom.PlayerObjectJoinedEvent += HandleJoin;

        maxPlayers = CoreManager.IsMultiplayer ? 1 : 4;

        // Spawn player menus for ppl already in the player input manager
        if(pom.PlayerObjectCount > 0)
            foreach(PlayerObject obj in pom.GetPlayerObjects()) {
                AddPanel(obj);
            }
    }

    protected new void OnDestroy() 
    {
        base.OnDestroy();
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= HandleJoin;
    }

    private void HandleJoin(PlayerObject obj) 
    {
        AddPanel(obj);
    }

    private void HandleLeave(PlayerObject obj) 
    {

    }   

    private void AddPanel(PlayerObject obj) 
    {
        // Spawn player panel
        GameObject playerPanel = Instantiate(playerPanelPrefab, playerPanelContainer.transform);
        PlayerPanelController playerPanelController = playerPanel.GetComponent<PlayerPanelController>();
        playerPanelController.Open(obj);

        UpdatePanels();
    }

    public void RemovePanel(PlayerObject obj, bool removePlayerInput) 
    {
        if(obj.PlayerIndex == 0) {
            SendMenuBack();
            return;
        }

        for(int i = 0; i < playerPanelContainer.transform.childCount; i++) {
            GameObject child = playerPanelContainer.transform.GetChild(i).gameObject;
            PlayerPanelController ppc = child.GetComponentInChildren<PlayerPanelController>();
            if(ppc == null)
                continue;

            if(ppc.FocusedPlayer.PlayerIndex == obj.PlayerIndex) {
                Destroy(child);

                if(removePlayerInput)
                    PlayerObjectManager.Instance.RemovePlayer(obj);
                
                return; // Call return so error message isn't shown
            }
        }

        Debug.LogError("Failed to remove player panel for object " + obj);
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
            PlayerObjectManager.Instance.GetPlayerInputManager().DisableJoining();

            if(maxPlayers == 1) {
                // Double scale so player's panel fills screen
                GameObject playerPanel = playerPanelContainer.transform.GetChild(0).gameObject;
                playerPanel.GetComponent<RectTransform>().SetScale(new Vector3(2, 2, 1));
            }
        }
    }

    /** Check if everyone's ready, if so, transition to map select. */
    public void CheckReady() 
    {
        if(!PlayerObjectManager.Instance.GetPlayerObjects().All(po => po.data.ready)) return;

        CoreManager.LobbyCommunicator.StartCommunication();
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);

        PlayerObjectManager.Instance.GetPlayerInputManager().DisableJoining();
    }

    /// <summary>
    /// Called by RemovePanel when the player one panel gets removed
    /// </summary>
    protected override void SendMenuBack() 
    {
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_TITLE);
    }

}
