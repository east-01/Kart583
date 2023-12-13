using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoostDisplay : MonoBehaviour
{

	public KartController kartController;

	/* Value is the true value, display value interpolates towards the true value and is shown on screen. */
	public float value;
	public float displayValue { get; private set; }
	public float interpolationFactor = 10;
	public Gradient colors;
	public AnimationCurve size;
	public AnimationCurve appearCurve, fadeCurve;
	public float minHeight, maxHeight;
	public float animationTime;

	private TMP_Text text;
	private float initialFontSize;
	private float animationTimeCnt;
	private bool showing;

	private float targetFontSize;
	private Vector3 targetColor;

    void Start()
    {
		text = GetComponent<TMP_Text>();   
		initialFontSize = text.fontSize;
    }

    void Update()
    {
		/* Animations */
		value = Mathf.Clamp01(kartController.BoostRatio);

		if(value > kartController.requiredBoostPercentage && displayValue <= kartController.requiredBoostPercentage) { 
			Display(true);	
		} else if(value < kartController.requiredBoostPercentage && displayValue >= kartController.requiredBoostPercentage && !kartController.ActivelyBoosting) { 
			Display(false);	
		}

		if(!kartController.ActivelyBoosting && showing && value < kartController.requiredBoostPercentage) { 
			Display(false);
		}

        displayValue = value;

		if(animationTimeCnt > 0) { 
			animationTimeCnt -= Time.deltaTime;	
		} else if(animationTimeCnt < 0) { 
			animationTimeCnt = 0;
		}

		RectTransform rt = GetComponent<RectTransform>();
		Vector3 pos = rt.anchoredPosition;
		if(showing) { 
			pos.y = minHeight + appearCurve.Evaluate(1-animationTimeCnt/animationTime)*(maxHeight-minHeight);
		} else { 			
			pos.y = minHeight + fadeCurve.Evaluate(1-animationTimeCnt/animationTime)*(maxHeight-minHeight);
		}
		rt.anchoredPosition = pos;

		/* Color/size */
		Color eval = colors.Evaluate(displayValue);
		targetColor = new Vector3(eval.r, eval.g, eval.b);
		targetFontSize = initialFontSize * size.Evaluate(displayValue);

		if(kartController.ActivelyBoosting) { 
			targetColor = new Vector3(1, 0, 0);
			targetFontSize = initialFontSize * (size.Evaluate(1)+0.5f);
		}

		Vector3 currentColor = new Vector3(text.color.r, text.color.g, text.color.b);
		currentColor = Vector3.Lerp(currentColor, targetColor, (targetColor-currentColor).magnitude*10*Time.deltaTime);
		text.color = new Color(currentColor.x, currentColor.y, currentColor.z, text.color.a);
		text.fontSize = Mathf.Lerp(text.fontSize, targetFontSize, Mathf.Abs(targetFontSize-text.fontSize)*5*Time.deltaTime);

		/* Text */
		int percent = (int)(displayValue*100);
		text.text = "<b>" + percent + "</b>" + "<size=75%>%</size>";

    }

	void Display(bool display) { 
		showing = display;
		animationTimeCnt = animationTime;
		text.color = colors.Evaluate(displayValue);
	}

}
