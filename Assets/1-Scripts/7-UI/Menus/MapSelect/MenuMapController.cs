using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/** This class is responsible for the overarching operation of the map select menu */
public class MenuMapController : MonoBehaviour
{
    
    [SerializeField] List<GameObject> toolTips;

    void Start() 
    {
        // Find player 1's input and let them control
        PlayerInput p0 = PlayerObjectManager.Instance.GetPlayerObjects()[0].input;
        p0.uiInputModule = GetComponentInChildren<InputSystemUIInputModule>();
        p0.SwitchCurrentActionMap("UI");

        // Select first map
        MapSelectBuilder builder = GetComponent<MapSelectBuilder>();
        builder.ReloadMenu();  
        Button toSelect = builder.MenuElements[0].GetComponent<Button>();
        toSelect.Select();

        // Set observed inputs on tooltips
        toolTips.ForEach(tt => tt.GetComponent<ToolTip>().SetObservedInput(p0));
    }

}