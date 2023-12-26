using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeldItem : KartBehavior
{
    
    public void Show(Item item) 
    {
        gameObject.SetActive(true);
        // TODO: Update held items texture to reflect what's in the held item slot			
    }

    public void Hide(bool animate) 
    {
        gameObject.SetActive(false);
        // TODO: Play animation
    }

}
