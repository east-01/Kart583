using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class NameSelectController : PlayerPanelControllerSubMenu
{
    [SerializeField] private TMP_InputField nameInputField;

    protected new void Opened() 
    {
        // TODO: Add on screen keyboard for name select
        if(focusedPlayer.input.currentControlScheme == "Gamepad") {
            focusedPlayer.data.name = KartsIRManager.SelectRandomBotName();
            PlayerPanelController.UpdatePanel();
        }
    }

    /* This method is called by the TextInputField, set in inspector */
    public void SubmitText() 
    {
        focusedPlayer.data.name = nameInputField.text;
        PlayerPanelController.UpdatePanel();
    }
}
