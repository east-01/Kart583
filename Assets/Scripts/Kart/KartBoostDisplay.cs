using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/** Translation layer between the KartController and the BoostDisplay class.
    KartController feeds boostAmount/maxBoost to BoostDisplay value. 

    TODO: This class is dumb and should be deleted. This task should be 
	  delegated to the GameplayManager because it needs to be different 
	  for each splitscreen display. */
public class KartBoostDisplay : MonoBehaviour
{
    
	public BoostDisplay boostDisplay;
	private KartController kc;

	private void Start()
	{
		kc = GetComponent<KartController>(); 
	}

	void Update()
    {
		boostDisplay.value = kc.boostAmount/kc.maxBoost;
    }
}
