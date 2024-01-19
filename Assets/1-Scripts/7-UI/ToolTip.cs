using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** Show a tooltip for a single input type, will observe a specific player's 
      input and pick the corresponding image. */
public class ToolTip : MonoBehaviour
{

    [SerializeField] private Sprite gamepadImage;
    [SerializeField] private Sprite keyboardMouseImage;

    private Image childImage;
    private PlayerInput observedInput;

    void Awake() 
    {
        childImage = GetComponentInChildren<Image>();
        if(childImage == null) 
            throw new InvalidOperationException("Failed to find child image.");
    }

    /* Yeah, this isn't really efficient. It would be better to do with events but led
         to problems when I tried it. If efficiency really becomes an issue I'll look into
         it again. */
    void Update() 
    {
        childImage.sprite = observedInput.devices[0] is Gamepad ? gamepadImage : keyboardMouseImage;
    }

    public void SetObservedInput(PlayerInput input) 
    {
        observedInput = input;
    }

}