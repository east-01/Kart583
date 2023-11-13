using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class KartBoostDisplay : MonoBehaviour
{
    
	public TMP_Text boostDisplay;
	
	private KartController kc;

	private void Start()
	{
		kc = GetComponent<KartController>();	
	}

	void Update()
    {
        
    }
}
