using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatRow : MonoBehaviour
{

    public RectTransform statRowForeground;
    public RectTransform statRowBackground;

    public void SetValue(float value) 
    {
        statRowForeground.offsetMax = new(-(statRowBackground.rect.width*(1-value)), statRowBackground.offsetMax.y);
    }

}
