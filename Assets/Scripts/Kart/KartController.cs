using UnityEngine;
using System;

public class KartController : MonoBehaviour
{
	public float maxSpeed = 5f;
    public float acceleration = 10f;
	public float passiveDeceleration = 1f;
    public float rotationSpeed = 100f;

	private PlayerControls controls;
    private Rigidbody rb;
	private KartStateManager stateMgr;
	private KartState state { get { return stateMgr.state; } }

	// Runtime variables
	[SerializeField]
    private float rotationInput;
	[SerializeField]
	private float velocity;

    private void Start()
    {
		if(controls != null) controls.Disable(); // Disable existing controls
		controls = new PlayerControls();
		controls.Enable();

        rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();
    }

    private void Update()
    {

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		float throttle = controls.Gameplay.Throttle.ReadValue<float>();

		switch(state) { 
			case KartState.DRIVING:	
				if(throttle > 0) {
					velocity += throttle * acceleration * Time.deltaTime;
					if(velocity > maxSpeed) velocity = maxSpeed;
				} else { 
					velocity -= passiveDeceleration * Time.deltaTime;
					// WARNING: This might cause the kart to stop if it bounced off the wall and rolled backwards
					if(velocity < 0) velocity = 0; 
				}

				rotationInput = turn.x * (velocity / maxSpeed);
				break;
			case KartState.DRIFTING:
				break;
			case KartState.REVERSING:
				break;
		}
    }

    private void FixedUpdate()
    {
		Vector3 movement = transform.forward * velocity * maxSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);


		float rotation = rotationInput * rotationSpeed * TurnFunction(0.1f, 0.3f, 0.5f, velocity / maxSpeed) * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

	/** Gets turn power percentage from a speed percentage.
		See desmos: https://www.desmos.com/calculator/tmojijx91i 
		
		In interval [a, b] you will get max turn power percentage.
		'c' is the turn power percentage at max speed.
		Outside the interval you will get a sloped value. See desmos. */
	public float TurnFunction(float a, float b, float c, float x) { 
		if(a >= b) throw new ArgumentException("a (" + a + ") must be less than b (" + b + ")");
		x = Mathf.Clamp01(x);
		if(x < a) { 
			return x/a;
		} else if(x > b) { 
			return -(c*(x-1))/(1-b) + c;
		} else { 
			return 1;	
		}
	}

}
