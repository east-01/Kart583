using System.Collections.Generic;
using EMullen.PlayerMgmt;
using UnityEngine;

public class POIGDelegate : MonoBehaviour
{
    public LocalPlayer owner;
    [SerializeField] private new Camera camera;
    [SerializeField] private Canvas hud;

    public Camera Camera { get { return camera; } }
    public Canvas HUD { get { return hud; } }
    public PlayerHUDCanvas PlayerHUDCanvas { get { return hud.GetComponent<PlayerHUDCanvas>(); } }
}

public static class POIGDelegateExtensions 
{
    private static Dictionary<string, POIGDelegate> uidDelegateDict = new();

    public static POIGDelegate GetPOIGDelegate(this LocalPlayer localPlayer) 
    {
        if(!uidDelegateDict.ContainsKey(localPlayer.UID))
            return null;
        return uidDelegateDict[localPlayer.UID];
    }

    public static void SetPOIGDelegate(this LocalPlayer localPlayer, POIGDelegate poigDelegate) 
    {
        if(uidDelegateDict.ContainsKey(localPlayer.UID)) {
            uidDelegateDict[localPlayer.UID] = poigDelegate;
        } else {
            uidDelegateDict.Add(localPlayer.UID, poigDelegate);
        }
    }

    public static void ClearPOIGDelegate(this LocalPlayer localPlayer) 
    {
        if(uidDelegateDict.ContainsKey(localPlayer.UID))
            uidDelegateDict.Remove(localPlayer.UID);
    }
}