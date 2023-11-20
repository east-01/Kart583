using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

/** This class will be responsible for one player.
    Manages items currently */
public class PlayerManager : MonoBehaviour 
{



    void Start()
    {
        
    }

    void Update()
    {
        
    }

	/** Callback for when a player hits an item box. 
	    Return true if item successfully recieved, false if not. */
	public bool HitItemBox(GameObject itemBox) { 
		
		print("recieved item");
		return true;
	}

}
