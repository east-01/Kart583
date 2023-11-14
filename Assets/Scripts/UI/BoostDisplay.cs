using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoostDisplay : MonoBehaviour
{

	/* Value is the true value, display value interpolates towards the true value and is shown on screen. */
	public float value;
	public float displayValue { get; private set; }
	public float interpolationFactor = 10;
	public Gradient colors;
	public AnimationCurve size;

	private TMP_Text text;
	private float initialFontSize;

    void Start()
    {
		text = GetComponent<TMP_Text>();   
		initialFontSize = text.fontSize;
    }

    void Update()
    {
		value = Mathf.Clamp01(value);
        displayValue = Mathf.Lerp(displayValue, value, interpolationFactor*Time.deltaTime);

		int percent = (int)(displayValue*100);
		text.text = "<b>" + percent + "</b>" + "<size=75%>%</size>";
		text.fontSize = initialFontSize * size.Evaluate(displayValue);
		text.color = colors.Evaluate(displayValue);
    }
}
