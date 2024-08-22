using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneNames
{
    public static readonly string MENU_TITLE = "MenuTitle";
    public static readonly string MENU_PLAYER = "MenuPlayer";
    public static readonly string MENU_LOBBY = "MenuLobby";
    public static readonly string MENU_SERVER = "MenuServer";
    public static readonly string MAP_ATUIN = "ATuinShipyard";
    public static readonly string MAP_TEST_TRACK = "TestTrack";

    public static bool IsLobbyScene(string sceneName) 
    {
        return sceneName == MENU_LOBBY;
    } 

    public static bool IsMapScene(string sceneName) 
    {
        return sceneName == MAP_TEST_TRACK || sceneName == MAP_ATUIN;
    }

    public static bool IsMenuScene(string sceneName) 
    {
        return sceneName.StartsWith("Menu");
    }
}
