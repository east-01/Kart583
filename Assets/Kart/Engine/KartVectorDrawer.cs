using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartVectorDrawer : KartBehavior
{
	public bool drawVectors = false;

    private void FixedUpdate()
    {
        DrawVector(kartCtrl.KartForward, Color.blue);
		DrawVector(rb.velocity, Color.cyan);
		DrawVector(rb.angularVelocity, Color.cyan);
		DrawVector(Up, Color.green);
    }

    public void DrawVector(Vector3 forceVector, Color col) 
	{
		if(drawVectors) Debug.DrawLine(transform.position, transform.position + forceVector, col);
	}
}
