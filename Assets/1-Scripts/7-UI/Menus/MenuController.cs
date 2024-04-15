using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public abstract class MenuController : MonoBehaviour
{

    [SerializeField]
    protected Selectable firstSelect;
    private bool menuControllerLoadedProperly = false;
    private bool disablePlayerOneInputEvents = false;

    protected PlayerControls controlsReference;

    protected void Awake() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerJoined;

        if(PlayerObjectManager.Instance.PlayerOne != null)
            Initialize();

        controlsReference = new();

        menuControllerLoadedProperly = true;
    }

    protected void OnDestroy() 
    {
        PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerJoined;
        if(PlayerObjectManager.Instance.PlayerOne != null)
            PlayerObjectManager.Instance.PlayerOne.input.onActionTriggered -= PlayerOneInput_ActionTriggered;
    }

    private void Initialize() 
    {
        if(firstSelect != null && PlayerObjectManager.Instance.PlayerOne.input.currentControlScheme != "KeyboardMouse")
            firstSelect.Select();
        
        PlayerObjectManager.Instance.PlayerOne.input.onActionTriggered += PlayerOneInput_ActionTriggered;
    }

    protected void LateUpdate() 
    {
        if(!menuControllerLoadedProperly)
            print($"MenuController script on \"{gameObject.name}\" wasn't loaded properly. Make sure you call base.Awake() if you're overriding it.");
    }

    private void PlayerObjectManager_PlayerJoined(PlayerObject obj) 
    {
        if(obj.PlayerIndex == 0)
            Initialize();
    }

    private void PlayerOneInput_ActionTriggered(InputAction.CallbackContext context) 
    {
        if(disablePlayerOneInputEvents)
            return;
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name)
            SendMenuBack();
    }

    /// <summary>
    /// Send the current menu back to the one before it.
    /// </summary>
    protected abstract void SendMenuBack();

    protected void DisablePlayerOneInputEvents(bool disabled) 
    {
        this.disablePlayerOneInputEvents = disabled;
    }

}
