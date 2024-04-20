using UnityEngine;

public class POIGDelegate : MonoBehaviour
{
    public PlayerObject owner;
    [SerializeField] private new Camera camera;
    [SerializeField] private Canvas hud;

    public Camera Camera { get { return camera; } }
    public Canvas HUD { get { return hud; } }
    public PlayerHUDCanvas PlayerHUDCanvas { get { return hud.GetComponent<PlayerHUDCanvas>(); } }
}
