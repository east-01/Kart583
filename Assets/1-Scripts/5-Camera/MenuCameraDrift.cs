using UnityEngine;

public class MenuCameraDrift : MonoBehaviour
{
    public float rotationSpeed = 1.0f;
    public float maxRandomOffset = 30.0f;
    public bool startRandomized = true;
    
    private Vector3 rotVector;
    private float rotVectorSelectTime;

    void Start() 
    {
        if(startRandomized) {
            rotVector = RollNewVector(); 
        }
    }

    void Update()
    {
        if(rotVectorSelectTime <= 0) {
            rotVector = RollNewVector();
            rotVectorSelectTime = Random.Range(15, 45);
        } else {
            rotVectorSelectTime -= Time.deltaTime;
        }
       
        // Rotate the camera slowly around its up axis plus the random rotation vector
        transform.Rotate(Vector3.up, (rotationSpeed*Random.Range(-maxRandomOffset, maxRandomOffset)) * Time.deltaTime);
        transform.Rotate(rotVector * Time.deltaTime);
    }

    public Vector3 RollNewVector() 
    {
        return new(
            Random.Range(-maxRandomOffset, maxRandomOffset),
            Random.Range(-maxRandomOffset, maxRandomOffset),
            Random.Range(-maxRandomOffset, maxRandomOffset)
        );
    }

}
