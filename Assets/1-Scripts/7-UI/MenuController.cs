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
    /// <summary>
    /// Automatically focus on player one when they are available.
    /// Does not happen if there's already a focused player.
    /// </summary>
    [SerializeField]
    private bool autoFocusOnPlayerOne;
    [SerializeField]
    private bool hidesParent = true;
    [SerializeField]
    private bool hidesSiblings = true;

    [SerializeField]
    protected List<SubMenuData> subMenus;
    [SerializeField]
    protected List<ToolTip> tooltips;

    protected PlayerObject focusedPlayer;
    private string focusedPlayerInitialActionMap; // Stores the action map of the focused player so we can revert them to it once they become unfocused.
    protected MenuController parentMenu;
    protected CanvasGroup canvasGroup;
    protected bool allowInputEvents = true;
    private bool menuControllerLoadedProperly = false;

    protected void Awake() 
    {
        controlsReference = new();

        if(!TryGetComponent(out canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        BLog.Highlight($"Canvas group is \"{canvasGroup}\"");

        InitializeSubMenus();

        menuControllerLoadedProperly = true;
    }

    protected void OnEnable() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerJoined;
        if(autoFocusOnPlayerOne && focusedPlayer == null && PlayerObjectManager.Instance.PlayerOne != null)
            SetFocus(PlayerObjectManager.Instance.PlayerOne);
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
            RemoveFocus();
    }

    protected void LateUpdate() 
    {
        if(!menuControllerLoadedProperly)
            Debug.LogError($"MenuController script \"{this}\" on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
    }

#region Focus
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

        if(focusedPlayer.input.enabled)
            focusedPlayer.input.SwitchCurrentActionMap(focusedPlayerInitialActionMap);

        focusedPlayer.input.onActionTriggered -= PlayerInput_ActionTriggered;
        focusedPlayer = null;
        focusedPlayerInitialActionMap = null;
    }
#endregion

#region Events
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
        BLog.Log($"MenuController \"{this}\" recieved input event \"{context.action.name}\"", LogChannel.MenuController, 5);
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name)
            SendMenuBack();

        Child_PlayerInput_ActionTriggered(context);
    }

    /// <summary>
    /// More Input Action events called from the MenuController class so that children can recieve them.
    /// i.e. If you have a PlayerSelect MenuController and you want to recieve input action events, you'll
    ///   override this method to get those events instead of subscribing to the playerInput directly.
    /// </summary>
    protected virtual void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) {}
#endregion

#region Open and Close
    public void Open(PlayerObject focus = null) 
    {
        // gameObject.SetActive(true);
        canvasGroup.alpha = 1.0f;
        canvasGroup.interactable = true;

        if(focus != null)
            SetFocus(focus);
        else if(!autoFocusOnPlayerOne)
            RemoveFocus();

        if(hidesParent && parentMenu != null)
            parentMenu.Close();

        BLog.Log($"MenuController \"{this}\" opened with focus \"{focus}\"", LogChannel.MenuController, 2);
        Opened();
    }

    /// <summary>
    /// Callback for when this MenuController was opened after the focus is set.
    /// </summary>
    protected virtual void Opened() {}

    public void Close() 
    {
        Closed();
        BLog.Log($"MenuController \"{this}\" closed", LogChannel.MenuController, 2);
        RemoveFocus();
        // gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// Callback for when this MenuController was closed, BEFORE we lose focus and it is disabled.
    /// </summary>
    protected virtual void Closed() {}
#endregion

#region Navigation
    /// <summary>
    /// Send the current menu back to the one before it.
    /// Default implementation will send a submenu to a parent menu if it exists.
    /// </summary>
    protected virtual void SendMenuBack() 
    {
        if(parentMenu == null)
            return;

        Close();
        parentMenu.Open();
    }

    public void OpenSubMenu(string id, PlayerObject focus = null) 
    {
        MenuController subMenu = GetSubMenu(id);
        if(subMenu == null) {
            Debug.LogError($"MenuController \"{this}\" failed to open SubMenu id \"{id}\" (subMenu is null).");
            return;
        }

        if(subMenu.hidesSiblings) {
            subMenus.ForEach(smd => {
                MenuController sm = GetSubMenu(smd.id);
                if(sm.IsOpen)
                    sm.Close();
            });
        }

        BLog.Log($"Menu \"{this}\" opening submenu \"{id}\" with focus \"{focus}\"", LogChannel.MenuController);
        subMenu.Open(focus);
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
        if(cachedSubmenus != null && cachedSubmenus.Count > 0)
            Debug.LogWarning($"Caching submenus on \"{this}\" but there's already {cachedSubmenus.Count} cached. This probably shouldn't happen.");
        
        cachedSubmenus = new();

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
            subMenu.Close();
        }
    }

    public void SetParentMenuController(MenuController parent) { this.parentMenu = parent; }

    protected SubMenuData? GetSubMenuData(string id) 
    {
        if(!cachedSubmenus.ContainsKey(id))
            return null;
        return cachedSubmenus[id];
    }

    /// <summary>
    /// Get a sub menu controller from its string id
    /// </summary>
    public MenuController GetSubMenu(string id) 
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

    public bool IsOpen { get { 
        return canvasGroup.interactable && canvasGroup.alpha == 1; 
    } }
    public PlayerObject FocusedPlayer => focusedPlayer; 

}

[Serializable]
public struct SubMenuData 
{
    public string id;
    public MenuController menuController;   
}