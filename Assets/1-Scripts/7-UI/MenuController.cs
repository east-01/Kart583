using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public abstract class MenuController : MonoBehaviour
{

    protected PlayerControls controlsReference;

    [SerializeField]
    protected OpenCloseType openCloseType;

    [SerializeField]
    private InputSystemUIInputModule inputSystemUIInputModule;
    /// <summary>
    /// The UIInputModule that this MenuController will use. If null, we will try to use the parent UIInputModule, this
    ///   search happens recursively until we reach an existing one on a parent.
    /// </summary>
    public InputSystemUIInputModule InputSystemUIInputModule { get { 
        if(inputSystemUIInputModule != null)
            return inputSystemUIInputModule;
        else if(parentMenu != null) {
            return parentMenu.InputSystemUIInputModule;
        } else {
            return null;
        }
    } }

    [SerializeField]
    private EventSystem eventSystem;
    /// <summary>
    /// The EventSystem this MenuController will use. If null, we will try to use the parent EventSystem, this search happens
    ///   recursively until we reach an existing one on a parent.
    /// </summary>
    public EventSystem EventSystem { get { 
        if(eventSystem != null)
            return eventSystem;
        else if(parentMenu != null) {
            return parentMenu.EventSystem;
        } else {
            return null;
        }
    } }

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

        if(openCloseType == OpenCloseType.CANVAS_GROUP && !TryGetComponent(out canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

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
        if(focusedPlayer != null && EventSystem.currentSelectedGameObject == null && firstSelect != null && focusedPlayer.input.currentControlScheme != "KeyboardMouse")
            EventSystem.SetSelectedGameObject(firstSelect.gameObject);

        if(!menuControllerLoadedProperly)
            Debug.LogError($"MenuController script \"{this}\" on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
    }

#region Focus
    public void SetFocus(PlayerObject playerObj) 
    {
        if(focusedPlayer != null) {
            if(focusedPlayer.data.uuid == playerObj.data.uuid) {
                BLog.Log($"{this}: Maintaining focus on {playerObj.PlayerIndex}", LogChannel.MenuController, 4);
                return;
            } else {
                BLog.Log($"{this}:Removing focus from {focusedPlayer.PlayerIndex} and placing it on {playerObj.PlayerIndex}", LogChannel.MenuController, 4);
                RemoveFocus();
            }
        } else {
            BLog.Log($"{this}:No focus existing, placing focus on {playerObj.PlayerIndex}", LogChannel.MenuController, 4);
        }

        focusedPlayer = playerObj;
        focusedPlayer.input.onActionTriggered += PlayerInput_ActionTriggered;
        focusedPlayerInitialActionMap = focusedPlayer.input.currentActionMap.name;

        focusedPlayer.input.SwitchCurrentActionMap("UI");
        focusedPlayer.input.uiInputModule = InputSystemUIInputModule;
        if(focusedPlayer.input.uiInputModule == null)
            Debug.LogWarning($"MenuController \"{this}\" failed to assign UIInputModule to new focus. This may be a misconfiguration, ensure that a UIInputModule is assigned on this script or in a parent MenuController.");

        tooltips.ForEach(tt => tt.SetObservedInput(focusedPlayer.input));

        if(ShouldSelect && firstSelect != null) {
            if(EventSystem != null)
                EventSystem.SetSelectedGameObject(firstSelect.gameObject);
            else
                Debug.LogWarning($"MenuController \"{this}\" failed to find an EventSystem. This may be a misconfiguration, ensure that an EventSystem is assigned on this script or in a parent MenuController.");
        }
    }

    public void RemoveFocus() 
    {
        if(focusedPlayer == null)
            return;

        if(focusedPlayer.input != null && focusedPlayer.input.enabled)
            focusedPlayer.input.SwitchCurrentActionMap(focusedPlayerInitialActionMap);

        focusedPlayer.input.onActionTriggered -= PlayerInput_ActionTriggered;
        focusedPlayer = null;
        focusedPlayerInitialActionMap = null;
    }
#endregion

#region Events
    private void PlayerObjectManager_PlayerJoined(PlayerObject obj) 
    {
        if(IsOpen && obj.PlayerIndex == 0 && autoFocusOnPlayerOne && focusedPlayer == null)
            SetFocus(obj);
    }

    /// <summary>
    /// Input events from the currently focused player.
    /// </summary>
    protected void PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
    {
        if(!allowInputEvents)
            return;
        BLog.Log($"MenuController \"{this}\" (focus: \"{(focusedPlayer != null ? focusedPlayer.PlayerIndex : "-")}\") recieved input event \"{context.action.name}\"", LogChannel.MenuController, 5);
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name) {
            SendMenuBack();
        }

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
        if(!gameObject.activeSelf) 
            gameObject.SetActive(true);

        if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE)
            gameObject.SetActive(true);
        else if(openCloseType == OpenCloseType.CANVAS_GROUP) {
            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if(focus != null)
            SetFocus(focus);
        else if(autoFocusOnPlayerOne && PlayerObjectManager.Instance.PlayerOne != null)
            SetFocus(PlayerObjectManager.Instance.PlayerOne);
        else
            RemoveFocus();

        if(hidesParent && parentMenu != null)
            parentMenu.Close();

        BLog.Log($"MenuController \"{this}\" opened with {(focusedPlayer != null ? $"focus \"{focusedPlayer.PlayerIndex}\"" : "no focus")}", LogChannel.MenuController, 0);
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
    
        if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE)
            gameObject.SetActive(false);
        else if(openCloseType == OpenCloseType.CANVAS_GROUP) {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
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
        Close();

        if(parentMenu != null)
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

        BLog.Log($"Menu \"{this}\" opening submenu \"{id}\"", LogChannel.MenuController, 1);
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
        if(openCloseType == OpenCloseType.GAME_OBJECT_ENABLE_DISABLE)
            return gameObject.activeSelf;
        else if(openCloseType == OpenCloseType.CANVAS_GROUP)
            return canvasGroup.interactable && canvasGroup.alpha == 1; 
        else
            return false;
    } }
    public bool IsSubMenuOpen { get {
        return subMenus.Any(sm => GetSubMenu(sm.id).IsOpen);
    } }
    public PlayerObject FocusedPlayer => focusedPlayer; 

}

[Serializable]
public struct SubMenuData 
{
    public string id;
    public MenuController menuController;   
}

public enum OpenCloseType { GAME_OBJECT_ENABLE_DISABLE, CANVAS_GROUP }