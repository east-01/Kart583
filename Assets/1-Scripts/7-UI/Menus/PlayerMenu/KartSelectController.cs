using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** A subcontroller to the player panel controller, manages the layout of karts to select
      and updates the stat panels */
public class KartSelectController : MonoBehaviour
{

    [SerializeField] GameObject gameplayManagerPrefab;
    [Header("Settings"), SerializeField] float scrollCooldown;
    [Header("Vehicle view"), SerializeField] TMP_Text vehicleNameText;
    [SerializeField] Image centerPosition;
    [SerializeField] Image leftPosition;
    [SerializeField] Image rightPosition;
    [Header("Statistics rows"), SerializeField] StatRow speedStats;
    [SerializeField] StatRow boostStats;
    [SerializeField] StatRow handlingStats;

    /* Runtime fields */
    private PlayerPanelController parentPanel;
    private PlayerControls controlsReference;

    private KartType currentName;
    private KartSettings highestStats;
    private float lastScrollTime; // Tracks the last time we recieved a navigate input

    void OnEnable() 
    {
        parentPanel = GetComponentInParent<PlayerPanelController>();
        if(parentPanel == null)
            throw new InvalidOperationException("Failed to find parent panel.");

        controlsReference = new PlayerControls();

        highestStats = gameplayManagerPrefab.GetComponent<KartAtlas>().HighestStats;

        if(parentPanel.PlayerObject.data.kartType != KartType.NONE)
            currentName = parentPanel.PlayerObject.data.kartType;
        else
            currentName = 0;

        UpdateVisuals();
    }

    public void HandleInputAction(InputAction.CallbackContext context) 
    {
        if(context.action.name == controlsReference.UI.Navigate.name && Time.time > (lastScrollTime + scrollCooldown)) {
            currentName = KartNameArithmetic(currentName, (int)Mathf.Sign(context.ReadValue<Vector2>().x));
            lastScrollTime = Time.time;
            UpdateVisuals();
        } else if(context.performed && context.action.name == controlsReference.UI.Submit.name) {
            parentPanel.SetKartName(currentName);
        }

    }

    void UpdateVisuals() 
    {
        KartAtlas ka = gameplayManagerPrefab.GetComponent<KartAtlas>();
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
