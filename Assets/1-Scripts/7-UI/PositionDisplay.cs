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

    public PositionTracker positionTracker;
    /* ----- Settings variables ---- */
    [Header("Lap")] public TMP_Text lapText;
    public Color lapTextColor;

    [Header("Race Position")]public TMP_Text positionText;
    public Color[] positionColors;

    /* ----- Runtime variables ----- */
    private RaceManager rm;
    private PlayerManager pm;

    private void Start() 
    {
        GameObject gm = GameObject.Find("GameplayManager");
        if(gm == null) throw new InvalidOperationException("Failed to find GameplayManager in scene!");
        rm = gm.GetComponent<RaceManager>();
        pm = gm.GetComponent<PlayerManager>();
    }

    private void Update() 
    {

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
