using UnityEngine;

/** Makes the title kart fly in the direction of it's transform.forward */
public class TitleShipFlight : MonoBehaviour
{
    [Header("Settings")]
    public float flightSpeed = 22.5f;
    public float rotateSpeed = 7.5f;
    public float accelerationTime = 0.15f;

    [Header("Runtime Fields")]
    public bool fly;

    private Transform menuCameraTransform;
    private Vector3? closestOffAngle;
    private float flightTime;

    void Start() { menuCameraTransform = GameObject.Find("MenuCamera").GetComponent<Transform>(); }

    void Update()
    {
        if(!fly) {
            flightTime = 0;
            return;
        }

        flightTime += Time.deltaTime;

        if(!closestOffAngle.HasValue) {
            float largestDot = 0;
            foreach(Vector3 offAngle in new Vector3[] {menuCameraTransform.up, -menuCameraTransform.up, menuCameraTransform.right, -menuCameraTransform.right}) {
                float dot = Vector3.Dot(offAngle, transform.forward);
                if(dot > largestDot) {
                    largestDot = dot;
                    closestOffAngle = offAngle;
                }
            }
        }

        float accelerationFactor = Mathf.Clamp01(flightTime/accelerationTime);

        if(Vector3.Dot(transform.forward, closestOffAngle.Value) > 0.5f)
            transform.position += flightSpeed*accelerationFactor*Time.deltaTime*transform.forward;
            
        transform.forward = Vector3.Lerp(transform.forward, closestOffAngle.Value, rotateSpeed*accelerationFactor*Time.deltaTime);
    }

}
