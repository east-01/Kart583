using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public abstract class MenuController : MonoBehaviour
{

    protected PlayerControls controlsReference;

    [SerializeField]
    protected Selectable firstSelect;
    [SerializeField]
    private bool autoFocusOnPlayerOne;

    [SerializeField]
    protected List<SubMenuData> subMenus;
    [SerializeField]
    protected List<ToolTip> tooltips;

    protected PlayerObject focusedPlayer;
    private string focusedPlayerInitialActionMap; // Stores the action map of the focused player so we can revert them to it once they become unfocused.
    private MenuController parentMenu;
    protected bool allowInputEvents = true;
    private bool menuControllerLoadedProperly = false;

    protected void Awake() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerJoined;

        if(PlayerObjectManager.Instance.PlayerOne != null && autoFocusOnPlayerOne)
            SetFocus(PlayerObjectManager.Instance.PlayerOne);

        controlsReference = new();

        InitializeSubMenus();

        menuControllerLoadedProperly = true;
    }

    protected void OnDestroy() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerJoined;
        if(focusedPlayer != null)
            RemoveFocus();
    }

    protected void OnDisable() 
    {
        if(focusedPlayer != null)
            Debug.LogWarning($"Disabling MenuController \"{this}\" while it still has a focused player.");
    }

    public void SetFocus(PlayerObject playerObj) 
    {
        if(focusedPlayer != null)
            RemoveFocus();

        focusedPlayer = playerObj;
        focusedPlayer.input.onActionTriggered += PlayerInput_ActionTriggered;
        focusedPlayerInitialActionMap = focusedPlayer.input.currentActionMap.name;

        focusedPlayer.input.SwitchCurrentActionMap("UI");

        tooltips.ForEach(tt => tt.SetObservedInput(focusedPlayer.input));

        if(ShouldSelect && firstSelect != null)
            firstSelect.Select();
    }

    public void RemoveFocus() 
    {
        if(focusedPlayer == null)
            return;

        focusedPlayer.input.SwitchCurrentActionMap(focusedPlayerInitialActionMap);

        focusedPlayer.input.onActionTriggered -= PlayerInput_ActionTriggered;
        focusedPlayer = null;
        focusedPlayerInitialActionMap = null;
    }

    protected void LateUpdate() 
    {
        if(!menuControllerLoadedProperly)
            Debug.LogError($"MenuController script \"{this}\" on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
    }

    private void PlayerObjectManager_PlayerJoined(PlayerObject obj) 
    {
        if(obj.PlayerIndex == 0 && autoFocusOnPlayerOne)
            SetFocus(obj);
    }

    /// <summary>
    /// Input events from the currently focused player.
    /// </summary>
    protected void PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
    {
        if(!allowInputEvents)
            return;
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name)
            SendMenuBack();

        Child_PlayerInput_ActionTriggered(context);
    }

    /// <summary>
    /// An optional method to recieve PlayerInput events after PlayerInput_ActionTriggered gets them.
    /// </summary>
    protected virtual void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) {}

#region Navigation
    /// <summary>
    /// Send the current menu back to the one before it.
    /// Default implementation will send a submenu to a parent menu if it exists.
    /// </summary>
    protected virtual void SendMenuBack() 
    {
        // TODO: Send submenu to parent menu if it exists 
    }

    public void OpenSubMenu(string id, PlayerObject focus = null) 
    {
        // TODO: Open sub menu
    }
#endregion

#region SubMenus
    /// <summary>
    /// Cache the submenus by ID for easy reference with GetSubMenu
    /// </summary>
    private Dictionary<string, SubMenuData> cachedSubmenus;

    /// <summary>
    /// Disables all sub-menu GameObjects and sets their parent MenuController to this
    /// </summary>
    private void InitializeSubMenus() 
    {
        foreach(SubMenuData smd in subMenus) {
            if(smd.id.Length == 0) {
                Debug.LogError($"SubMenu data on \"{this}\" has an empty string. Not caching it.");
                continue;
            }
            if(cachedSubmenus.ContainsKey(smd.id)) {
                Debug.LogError($"SubMenu data on \"{this}\" has an identical id ({smd.id}) already cached. Not caching it.");
                continue;
            }

            cachedSubmenus.Add(smd.id, smd);

            MenuController subMenu = smd.menuController;

            subMenu.SetParentMenuController(this);
            subMenu.gameObject.SetActive(false);
        }
    }

    public void SetParentMenuController(MenuController parent) 
    {
        if(parentMenu != null) {
            Debug.LogError($"Can't set parent menu for sub-menu \"{this}\" since it already has parent \"{parentMenu}\"");
            return;
        }
        this.parentMenu = parent;
    }

    protected SubMenuData? GetSubMenuData(string id) 
    {
        if(!cachedSubmenus.ContainsKey(id))
            return null;
        return cachedSubmenus[id];
    }

    /// <summary>
    /// Get a sub menu controller from its string id
    /// </summary>
    protected MenuController GetSubMenu(string id) 
    {
        if(!cachedSubmenus.ContainsKey(id))
            return null;
        return GetSubMenuData(id).Value.menuController;
    }
#endregion

    public bool ShouldSelect { get {
        if(focusedPlayer == null)
            return true;
        return focusedPlayer.input.currentControlScheme != "KeyboardMouse";
    } }

}

[Serializable]
public struct SubMenuData 
{
    public string id;
    public MenuController menuController;   
}