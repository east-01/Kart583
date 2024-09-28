using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FishNet.Connection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The LobbyMessagePopup is an addon component to the lobby system that displays messages as a
///   popup when the LobbyCommunicator recieves a server message.
/// It requires the MenuController system.
/// </summary>
public class PopupMenuController : MenuController
{
    
    public const string POPUP_GROUP_ID_SINGLE_CONFIRM = "SingleConfirm";
    public const string POPUP_GROUP_ID_CONFIRM_DENY = "ConfirmDeny";

    public static PopupMenuController Instance;

    [SerializeField]
    private TMP_Text messageTitle;
    [SerializeField]
    private TMP_Text messageDetails;
    [SerializeField]
    private List<PopupMessageUIGroup> popupMessageUIGroups;

    public string UsedGroupID { get; private set; }
    /// <summary>
    /// When using group id SingleConfirm and the confirm button is pressed, the popup will close
    ///   and the scenes recommended menu will open.
    /// </summary>
    public bool AllowDefaultSingleConfirmBehaviour = true;

#region Events
    public delegate void PopupConfirmClicked();
    /// <summary>
    /// Called when the confirm button is clicked on the popup menu.
    /// The confirm button exists in all popup groups
    /// </summary>
    public event PopupConfirmClicked PopupConfirmClickedEvent;
    public delegate void PopupDenyClicked();
    /// <summary>
    /// Called when the deny button is clicked on the popup menu.
    /// The deny button only exists on the Confirm/Deny group.
    /// </summary>
    public event PopupDenyClicked PopupDenyClickedEvent;
#endregion

    protected new void Awake() 
    {
        base.Awake();
        if(Instance != null) {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);

        Close();
        GetComponentInParent<Canvas>().enabled = true;
    }

    protected override void Opened()
    {
        base.Opened();
        // (MenuController, PlayerObject) history = RetrieveHistory(1).Value;
        // BLog.Highlight($"Previous focus: {history.Item1.GetType()} focused {history.Item2.PlayerName}");
    }

    public void Open(string id, string messageTitle, string messageDetails = "None") 
    {
        BLog.Highlight($"opening popup: \"{id}\" \"{messageTitle}\" \"{messageDetails}\"");

        this.messageTitle.text = messageTitle;
        this.messageDetails.text = messageDetails;

        PopupMessageUIGroup? nullableGroup = GetPopupMessageUIGroup(id);
        if(nullableGroup == null) {
            Debug.LogError($"Can't open popup with invalid button group id \"{id}\"");
            return;
        }
        PopupMessageUIGroup group = nullableGroup.Value;
        UsedGroupID = id;

        popupMessageUIGroups.ForEach(g => g.grouping.SetActive(false));
        group.grouping.SetActive(true);

        firstSelect = group.firstSelect;

        base.Open();

    }

    public PopupMessageUIGroup? GetPopupMessageUIGroup(string id) {
        foreach(PopupMessageUIGroup group in popupMessageUIGroups) {
            if(group.id == id)
                return group;
        }
        return null;
    }

#region Button callbacks
    public void ConfirmClicked() 
    {
        if(AllowDefaultSingleConfirmBehaviour && UsedGroupID == POPUP_GROUP_ID_SINGLE_CONFIRM)
            SingleConfirmDefaultBehaviour();

        PopupConfirmClickedEvent?.Invoke();
    }

    public void DenyClicked() 
    {
        PopupDenyClickedEvent?.Invoke();
    }
#endregion

    public void SingleConfirmDefaultBehaviour() 
    {
        Close();
        // (MenuController, PlayerObject)? historyNullable = RetrieveHistory(1);
        // if(historyNullable != null) {
        //     (MenuController, PlayerObject) history = historyNullable.Value;
        //     history.Item1.Open(history.Item2);
        // }
    }
}

[Serializable]
public struct PopupMessageUIGroup {
    public string id;
    public GameObject grouping;
    public Selectable firstSelect;
}