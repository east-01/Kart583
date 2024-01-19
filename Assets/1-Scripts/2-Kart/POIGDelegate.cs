using UnityEngine;

public class POIGDelegate : MonoBehaviour
{
    [SerializeField] private GameObject kartObject;
    [SerializeField] private new Camera camera;

    public GameObject KartObject { get { return kartObject; } }
    public Camera Camera { get { return camera; } }
}
