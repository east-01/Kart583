using UnityEngine;

public class POIGDelegate : MonoBehaviour
{
    [SerializeField] private GameObject kartObject;
    [SerializeField] private new Camera camera;
    [SerializeField] private Canvas hud;

    public GameObject KartObject { get { return kartObject; } }
    public Camera Camera { get { return camera; } }
    public Canvas HUD { get { return hud; } }
    public PlayerHUDCanvas PlayerHUDCanvas { get { return hud.GetComponent<PlayerHUDCanvas>(); } }
}
