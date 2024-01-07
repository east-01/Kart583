using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
public class UIInputDelegate : EventTrigger
{

    private Vector2 mousePos;
    // private bool mouseMode;

    void Update()
    {
        if(Vector2.Distance(mousePos, Input.mousePosition) > 1f) {
            mousePos = Input.mousePosition;
            // mouseMode = true;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public override void OnMove(AxisEventData data) { InputAction(); }
    public override void OnSubmit(BaseEventData data) { InputAction(); }
    public override void OnCancel(BaseEventData data) { InputAction(); }

    public void InputAction() 
    {
        print("recieved action");
        // mouseMode = false;
        EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);
    }

}
