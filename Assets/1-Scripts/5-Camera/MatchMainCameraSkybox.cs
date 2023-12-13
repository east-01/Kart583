using UnityEngine;

public class MatchMainCameraSkybox : MonoBehaviour
{
    private void Start()
    {
        if(!GameplayManager.HasRaceCamera) return;
        Camera mainCamera = GameplayManager.RaceCamera.GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found in the scene!");
            return;
        }

        // Copy the skybox material from the main camera to this camera
        if (mainCamera.clearFlags == CameraClearFlags.Skybox)
        {
            Material skyboxMaterial = mainCamera.GetComponent<Skybox>().material;
            if (skyboxMaterial != null)
            {
                GetComponent<Skybox>().material = Instantiate(skyboxMaterial);
            }
            else
            {
                Debug.LogError("Main camera's skybox material not found!");
            }
        }
        else
        {
            Debug.LogWarning("Main camera does not have a skybox!");
        }
    }
}
