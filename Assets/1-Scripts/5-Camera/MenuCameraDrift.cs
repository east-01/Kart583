using UnityEngine;

public class MenuCameraDrift : MonoBehaviour
{
    public float rotationSpeed = 1.0f;
    public float maxRandomOffset = 30.0f;
    private Vector3 rotVector;
    public float rotVectorSelectTime;

    void Update()
    {
        if(rotVectorSelectTime <= 0) {
            rotVector = new Vector3(
                Random.Range(-maxRandomOffset, maxRandomOffset),
                Random.Range(-maxRandomOffset, maxRandomOffset),
                Random.Range(-maxRandomOffset, maxRandomOffset)
            );
            rotVectorSelectTime = Random.Range(15, 45);
        } else {
            rotVectorSelectTime -= Time.deltaTime;
        }
       
        // Rotate the camera slowly around its up axis plus the random rotation vector
        transform.Rotate(Vector3.up, (rotationSpeed*Random.Range(-maxRandomOffset, maxRandomOffset)) * Time.deltaTime);
        transform.Rotate(rotVector * Time.deltaTime);
    }
}
