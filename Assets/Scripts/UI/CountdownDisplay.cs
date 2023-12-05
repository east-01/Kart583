using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CountdownDisplay : MonoBehaviour
{

    /* ----- Settings variables ---- */
    [Header("General animation")] public float regularSize;
    public Color regularColor;
    public float regularHeight;
    public int finalCountdownSeconds = 3;
    public float finalCountdownSize;
    public Color finalCountdownColor;
    public float finalCountdownHeight;

    
    [Header("Single Second animation")] public AnimationCurve height;
    public float delay = 0.2f;

    /* ----- Runtime variables ----- */
    private RaceManager rm;
    private RectTransform rt;
    [Header("Runtime fields")] public int displayedSecond;
    public int raceFloor;
    public float secondProgress;

    void Start() 
    {
        rt = GetComponent<RectTransform>();
        rm = null;
        GameObject rmo = GameObject.Find("GameplayManager");
        if(rmo != null && !rmo.TryGetComponent<RaceManager>(out rm)) {
            Debug.LogError("Countdown display failed to find race manager!");
            gameObject.SetActive(false);
        }
    }

    void Update()
    {

        if(rm.raceTime < 1) {
            // -1.2f --> |_-1.2_| == -2 --> |-2 - -1.2| --> 0.8 correct, -1.2 does represent 80% progress through 0.8
            raceFloor = (int)Math.Floor(rm.raceTime);
            secondProgress = Math.Abs(raceFloor-rm.raceTime);

            if(secondProgress < delay) return;

            displayedSecond = -raceFloor;

            Vector3 pos = rt.anchoredPosition;
            pos.y = (displayedSecond <= finalCountdownSeconds ? finalCountdownHeight : regularHeight) + height.Evaluate(secondProgress);
            rt.anchoredPosition = pos;

            TMP_Text text = GetComponent<TMP_Text>();
            text.fontSize = displayedSecond <= finalCountdownSeconds ? finalCountdownSize : regularSize;
            text.color = displayedSecond <= finalCountdownSeconds ? finalCountdownColor : regularColor;
            text.text = displayedSecond > 0 ? displayedSecond.ToString() : "GO!";

        } else {
            displayedSecond = 0;
            GetComponent<TMP_Text>().text = "";
        }
    }
}
