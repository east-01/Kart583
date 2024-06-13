using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** A subcontroller to the player panel controller, manages the layout of karts to select
      and updates the stat panels */
public class KartSelectController : PlayerPanelControllerSubMenu
{

    [Header("Settings"), SerializeField] float scrollCooldown;
    [Header("Vehicle view"), SerializeField] TMP_Text vehicleNameText;
    [SerializeField] Image centerPosition;
    [SerializeField] Image leftPosition;
    [SerializeField] Image rightPosition;
    [Header("Statistics rows"), SerializeField] StatRow speedStats;
    [SerializeField] StatRow boostStats;
    [SerializeField] StatRow handlingStats;

    /* Runtime fields */
    private KartType currentName;
    private KartSettings highestStats;

    protected override void Opened() 
    {
        base.Opened();
        highestStats = CoreManager.KartAtlas.HighestStats;

        if(focusedPlayer.data.kartType != KartType.NONE)
            currentName = focusedPlayer.data.kartType;
        else
            currentName = (KartType)1;

        UpdateVisuals();
    }

    protected override void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
    {
        if(context.performed && context.action.name == controlsReference.UI.Navigate.name && Math.Abs(context.ReadValue<Vector2>().x) > 0.05) {
            currentName = KartNameArithmetic(currentName, (int)Mathf.Sign(context.ReadValue<Vector2>().x));
            uiElementSounds.PlaySelectSound();
            UpdateVisuals();
        } else if(context.phase == InputActionPhase.Canceled && context.action.name == controlsReference.UI.Submit.name) {
            uiElementSounds.PlayButtonSound();
            SetKartName(currentName);
        }
    }

    public void SetKartName(KartType kartName) {
        focusedPlayer.data.kartType = kartName;
        PlayerPanelController.UpdatePanel();
    }

    void UpdateVisuals() 
    {
        KartAtlas ka = CoreManager.KartAtlas;
        // Update stats
        KartDataPackage kdp = ka.RetrieveData(currentName);
        KartSettings currentStats = kdp.settings;
        speedStats.SetValue(currentStats.maxSpeed/highestStats.maxSpeed);
        boostStats.SetValue(currentStats.maxBoost/highestStats.maxBoost);
        handlingStats.SetValue(currentStats.turnSpeed/highestStats.turnSpeed);
        
        // Update vehicle images/name
        vehicleNameText.text = kdp.name;

        centerPosition.sprite = kdp.image;
        leftPosition.sprite = ka.RetrieveData(KartNameArithmetic(currentName, -1)).image;
        rightPosition.sprite = ka.RetrieveData(KartNameArithmetic(currentName, 1)).image;
    }

    private KartType KartNameArithmetic(KartType current, int offset) {
        int kartCount = Enum.GetValues(typeof(KartType)).Length;
        current += offset;
        if((int)current < 1) {
            current = (KartType)(kartCount-1);
        } else if((int)current >= kartCount) {
            current = (KartType)1;
        }
        return current;
    }

}
