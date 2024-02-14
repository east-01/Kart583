using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PositionDisplay : MonoBehaviour
{

    public PlayerHUDCanvas parent;
    /* ----- Settings variables ---- */
    [Header("Lap")] public TMP_Text lapText;
    public Color lapTextColor;

    [Header("Race Position")]public TMP_Text positionText;
    public Color[] positionColors;

    private void Update() 
    {
        if(GameplayManager.Instance == null) return;
        if(parent.subject == null) return;
        
        RaceManager rm = GameplayManager.RaceManager;
        PositionTracker positionTracker = parent.subject.GetPositionTracker();

        // Lap text
        lapText.text = "LAP " + Mathf.Clamp(positionTracker.lapNumber+1, 0, rm.settings.laps) + "/" + rm.settings.laps;
        lapText.color = lapTextColor;

        // Position text
        Vector3 currentColor = new Vector3(positionText.color.r, positionText.color.g, positionText.color.b);
        Color targetCol = GetTargetPositionColor();
        Vector3 targetColor = new Vector3(targetCol.r, targetCol.g, targetCol.b);
        Vector3 newColor = Vector3.Lerp(currentColor, targetColor, 10*Time.deltaTime);

        int displayPos = positionTracker.racePos+1;

        positionText.text = displayPos + GetNumberSuffix(displayPos).ToUpper();
        positionText.color = new Color(newColor.x, newColor.y, newColor.z, 0.8f);
    }

    public Color GetTargetPositionColor() 
    {   
        if(parent.subject == null) return positionColors[0];
        PositionTracker positionTracker = parent.subject.GetPositionTracker();
        return positionColors[Mathf.Clamp(positionTracker.racePos, 0, positionColors.Length-1)];
    }

    private String GetNumberSuffix(int i) 
    {
        switch(i) {
            case 1:
                return "st";
            case 2:
                return "nd";
            case 3:
                return "rd";
            default:
                return "th";
        }
    }

}
