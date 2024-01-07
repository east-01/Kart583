using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlacementRow : MonoBehaviour
{

    public TMP_Text placeText;
    public TMP_Text nameText;
    public TMP_Text timeText;

    /** Update the visual to reflect the kart that it's going to represent.
        This will change the placement text, name text and time text. */
    public void UpdateVisuals(KartManager kart, int placement) 
    {
        placeText.text = placement + ".";
        nameText.text = "Player";
        timeText.text = FormatTime(kart.GetPositionTracker().raceFinishTime);
    }

    public string FormatTime(float time) { 
		if(time < 0) return "--.--";
		int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000);
		string formattedTime = "--.--";
		if (minutes > 0) {
			formattedTime = string.Format("{0}:{1:D2}.{2:D2}", minutes, seconds, (int)(milliseconds/10f));
		} else if(minutes == 0) {
		    formattedTime = string.Format("{0:D1}.{1:D2}", seconds, (int)(milliseconds/10f));
	    }
		return formattedTime;
	}
}
