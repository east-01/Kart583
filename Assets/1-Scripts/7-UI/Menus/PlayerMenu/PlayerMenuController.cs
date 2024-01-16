using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngineInternal;

/** The PlayerMenuController will interface between the PlayerObjectManager and
      the child PlayerPanelControllers */
public class PlayerMenuController : MonoBehaviour
{

    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private GameObject playerPanelContainer;
    [SerializeField] private GameObject joinMessage;

    void Start() 
    {
        PlayerObjectManager.Instance.GetPlayerInputManager().EnableJoining();
    }

    public void HandleJoin(PlayerObject obj) 
    {
        // Spawn player panel
        GameObject playerPanel = Instantiate(playerPanelPrefab, playerPanelContainer.transform);
        PlayerPanelController playerPanelController = playerPanel.GetComponent<PlayerPanelController>();
        playerPanelController.SetPlayerObject(obj);
        playerPanelController.UpdateVisuals();

        // Ensure the join message stays at the end
        if(playerPanelContainer.transform.childCount > 4) {
            GameObject.Find("JoinMessage").SetActive(false);
        } else {
            joinMessage.transform.SetAsLastSibling();
        }

        // Connect ui input
        obj.input.uiInputModule = playerPanel.GetComponentInChildren<InputSystemUIInputModule>();
    }

    public void HandleLeave(PlayerObject obj) 
    {

    }   

    /** Check if everyone's ready, if so, transition to map select. */
    public void CheckReady() 
    {
        if(!PlayerObjectManager.Instance.GetPlayerObjects().All(po => po.data.ready)) return;

        GameObject tmo = GameObject.Find("TransitionManager");
        tmo.GetComponent<TransitionManager>().LoadScene("MapSelect");

        PlayerObjectManager.Instance.GetPlayerInputManager().DisableJoining();
    }

}
