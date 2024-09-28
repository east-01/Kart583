using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothedFollow : StrictFollow
{

	public int bufferSize = 5;
	public AnimationCurve weightCurve;

	public Vector3[] buffer { get; private set; }

	private void Start()
	{
		transform.position = GetTargetPosition();
		LoadBuffer(GetTargetPosition());	
	}

	void FixedUpdate()
    {
		transform.position = AverageBuffer();
		transform.LookAt(subject.position);

		AddToBuffer(GetTargetPosition());
    }

	public Vector3 AverageBuffer() 
	{ 
		Vector3 vectorSum = Vector3.zero;
		float weightSum = 0;
		for(int i = 0; i < bufferSize; i++) { 
			float weight = weightCurve.Evaluate(i / (float)bufferSize);
			vectorSum += buffer[i]*weight;
			weightSum += weight;
		}
		return vectorSum/weightSum;
	}

	public void LoadBuffer(Vector3 targetPosition) 
	{ 
		buffer = new Vector3[bufferSize];
		for(int i = 0; i < bufferSize; i++) {
			buffer[i] = targetPosition; 
		}
	}

	/** Add the new position to the front of the buffer. */
	public void AddToBuffer(Vector3 newPosition) { 
		// Shift buffer to the right
		for(int i = 0; i < bufferSize-1; i++) {
			buffer[i+1] = buffer[i];
		}
		buffer[0]=newPosition;
	}
	
}
