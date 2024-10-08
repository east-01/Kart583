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
    private KartType currentType;
    private KartSettings highestStats;

    private readonly PlayerControls controlsReference = new();

    protected override void Opened() 
    {
        base.Opened();
        highestStats = CoreManager.KartAtlas.HighestStats;

        if(!focusedPlayer.HasPlayerData()) {
            Debug.LogError("Can't open KartSelectController the focused player does not have PlayerData.");
            return;
        }

        RaceData rd = focusedPlayer.GetPlayerData().GetData<RaceData>();
        if(rd.kartType != KartType.NONE)
            currentType = rd.kartType;
        else
            currentType = (KartType)1;

        UpdateVisuals();
    }

    protected override void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context) 
    {
        if(context.performed && context.action.name == controlsReference.UI.Navigate.name && Math.Abs(context.ReadValue<Vector2>().x) > 0.05) {
            currentType = KartNameArithmetic(currentType, (int)Mathf.Sign(context.ReadValue<Vector2>().x));
            uiElementSounds.PlaySelectSound();
            UpdateVisuals();
        } else if(context.phase == InputActionPhase.Canceled && context.action.name == controlsReference.UI.Submit.name) {
            uiElementSounds.PlayButtonSound();
            SetKartName(currentType);
        }
    }

    public void SetKartName(KartType kartName) {
        RaceData rd = focusedPlayer.GetPlayerData().GetData<RaceData>();
        rd.kartType = kartName;
        focusedPlayer.GetPlayerData().SetData(rd);
        PlayerPanelController.UpdateBuildPhase();
    }

    void UpdateVisuals() 
    {
        KartAtlas ka = CoreManager.KartAtlas;
        // Update stats
        KartDataPackage kdp = ka.RetrieveData(currentType);
        KartSettings currentStats = kdp.settings;
        speedStats.SetValue(currentStats.maxSpeed/highestStats.maxSpeed);
        boostStats.SetValue(currentStats.maxBoost/highestStats.maxBoost);
        handlingStats.SetValue(currentStats.turnSpeed/highestStats.turnSpeed);
        
        // Update vehicle images/name
        vehicleNameText.text = kdp.name;

        centerPosition.sprite = kdp.image;
        leftPosition.sprite = ka.RetrieveData(KartNameArithmetic(currentType, -1)).image;
        rightPosition.sprite = ka.RetrieveData(KartNameArithmetic(currentType, 1)).image;
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
