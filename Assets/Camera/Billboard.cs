using UnityEngine;

public class Billboard : MonoBehaviour {

    public Camera focusCamera;

    [SerializeField] 
    private BillboardType billboardType;

    [Header("Lock Rotation")]
    [SerializeField] 
    private bool lockX;
    [SerializeField] 
    private bool lockY;
    [SerializeField] 
    private bool lockZ;

    private Vector3 originalRotation;

    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake() {
        originalRotation = transform.rotation.eulerAngles;
    }

    // Use Late update so everything should have finished moving.
    void LateUpdate() {
        if(focusCamera == null)
            return; 
        // There are two ways people billboard things.
        switch (billboardType) {
        case BillboardType.LookAtCamera:
            transform.LookAt(focusCamera.transform.position, Vector3.up);
            transform.rotation *= Quaternion.Euler(0, 180, 0);
            break;
        case BillboardType.CameraForward:
            transform.forward = -focusCamera.transform.forward;
            break;
        default:
            break;
        }

        // Modify the rotation in Euler space to lock certain dimensions.
        Vector3 rotation = transform.rotation.eulerAngles;
        if (lockX) { rotation.x = originalRotation.x; }
        if (lockY) { rotation.y = originalRotation.y; }
        if (lockZ) { rotation.z = originalRotation.z; }
        transform.rotation = Quaternion.Euler(rotation);

    }
}