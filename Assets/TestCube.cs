using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class TestCube : NetworkBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F6))
            print("Test cube spawned: " + base.IsSpawned);       
    }
}
