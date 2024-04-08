using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameKit.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngineInternal;

/** The PlayerMenuController will interface between the PlayerObjectManager and
      the child PlayerPanelControllers */
public class MenuPlayerController : MonoBehaviour
{

    [SerializeField] private GameObject playerPanelPrefab;

    [SerializeField] private GameObject playerPanelContainer;
    [SerializeField] private GameObject joinMessage;

    [SerializeField] private int maxPlayers;

    void Start() 
    {
        PlayerObjectManager.Instance.GetPlayerInputManager().EnableJoining();
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent += HandleJoin;

        maxPlayers = CoreManager.Instance.isMultiplayer ? 1 : 4;

        // Spawn player menus for ppl already in the player input manager
        if(PlayerObjectManager.Instance.GetPlayerObjects().Count > 0)
            Debug.LogWarning("TODO: Handle existing player objects at start");
    }

    void OnDestroy() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= HandleJoin;
    }

    private void HandleJoin(PlayerObject obj) 
    {
        // Spawn player panel
        GameObject playerPanel = Instantiate(playerPanelPrefab, playerPanelContainer.transform);
        PlayerPanelController playerPanelController = playerPanel.GetComponent<PlayerPanelController>();
        playerPanelController.SetPlayerObject(obj);
        playerPanelController.UpdateVisuals();

        // Connect ui input
        obj.input.uiInputModule = playerPanel.GetComponentInChildren<InputSystemUIInputModule>();

        UpdatePanels();
    }

    private void HandleLeave(PlayerObject obj) 
    {

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

        string targetScene = CoreManager.Instance.isMultiplayer ? SceneNames.MENU_LOBBY : SceneNames.MENU_MAP;
        GameObject tmo = GameObject.Find("TransitionManager");
        tmo.GetComponent<TransitionManager>().LoadScene(targetScene);

        PlayerObjectManager.Instance.GetPlayerInputManager().DisableJoining();
    }

}
