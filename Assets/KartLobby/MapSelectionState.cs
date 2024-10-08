using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using UnityEngine;

public class MapSelectionState : KartLobbyState
{

    private bool forceMapPick;

    public MapSelectionState(KartLobby gameLobby) : base(gameLobby)
    {

    }

    public override void Update()
    {
        base.Update();
        if(Input.GetKeyDown(KartLobby.FORCE_MAP_PICK_KEY))
            forceMapPick = true;
    }

    public override LobbyState CheckForStateChange()
    {
        bool autoSelectValid = kartLobby.CanAutoSelectLevel && TimeInState >= KartLobby.MAP_PICK_TIME;
        if(kartLobby.level == null && (autoSelectValid || forceMapPick)) {
            forceMapPick = false;
            KartLevel? selectedLevel;
            if(DevSettings.Settings.OverrideMapPick)
                selectedLevel = DevSettings.Settings.Map;
            else 
                selectedLevel = LevelAtlas.PickRandomLevel();

            kartLobby.SetLevel(selectedLevel.Value);
        } else if(kartLobby.level != null && kartLobby.MapScene != null/* && gameplayManager != null*/) {
            kartLobby.MovePlayersToMap();                    
            return new RacingState(kartLobby);
        }
        return null;
    }
}