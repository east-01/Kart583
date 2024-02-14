using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

public class BoostDisplay : MonoBehaviour
{

	public PlayerHUDCanvas parent;

	[SerializeField] Image foregroundImage;

	[SerializeField] RectTransform backgroundTransform;
	[SerializeField] Image backgroundImage;
	[SerializeField] float backgroundMaxWidth;
	[SerializeField] float backgroundMinWidth;

	[SerializeField] RectTransform minLine;

	// [SerializeField] float interpolationFactor = 10;
	[SerializeField] Gradient colors;
	[SerializeField] AnimationCurve appearCurve, fadeCurve;
	[SerializeField] float minHeight, maxHeight;
	[SerializeField] float animationTime;

	/* Runtime fields */
	public float value; // Value is the true value, display value interpolates towards the true value and is shown on screen.
	public float displayValue { get; private set; }
	private float animationTimeCnt;
	private bool showing;

	private Vector3 targetColor;

    void Update()
    {

		if(parent.subject == null) return;
		KartController kartController = parent.subject.GetKartController();

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

		// RectTransform rt = GetComponent<RectTransform>();
		// Vector3 pos = rt.anchoredPosition;
		// if(showing) { 
		// 	pos.y = minHeight + appearCurve.Evaluate(1-animationTimeCnt/animationTime)*(maxHeight-minHeight);
		// } else { 			
		// 	pos.y = minHeight + fadeCurve.Evaluate(1-animationTimeCnt/animationTime)*(maxHeight-minHeight);
		// }
		// rt.anchoredPosition = pos;

		/* Color/size */
		Color eval = colors.Evaluate(displayValue);
		targetColor = new Vector3(eval.r, eval.g, eval.b);

		if(kartController.ActivelyBoosting) { 
			targetColor = new Vector3(1, 0, 0);
		}

		Vector3 currentColor = new Vector3(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b);
		currentColor = Vector3.Lerp(currentColor, targetColor, (targetColor-currentColor).magnitude*10*Time.deltaTime);
		backgroundImage.color = new Color(currentColor.x, currentColor.y, currentColor.z, backgroundImage.color.a);

		/* Background bar width */
		float width = displayValue*(backgroundMaxWidth-backgroundMinWidth);
		if(width < backgroundMinWidth) width = backgroundMinWidth;

		backgroundTransform.sizeDelta = new(width, backgroundTransform.sizeDelta.y);

		float minLineX = kartController.requiredBoostPercentage*(backgroundMaxWidth-backgroundMinWidth);
		minLine.anchoredPosition = new(minLineX, minLine.anchoredPosition.y);

    }

	void Display(bool display) { 
		showing = display;
		animationTimeCnt = animationTime;
		backgroundImage.color = colors.Evaluate(displayValue);
	}

}
