using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using UnityEngine;

public class WaitingForPlayersState : KartLobbyState
{

    public WaitingForPlayersState(KartLobby gameLobby) : base(gameLobby)
    {

    }

    public override void Update()
    {
        base.Update();

    }

    public override LobbyState CheckForStateChange()
    {
        bool timePassed = kartLobby.CanAutoSelectLevel && TimeInState >= KartLobby.PLAYER_WAIT_TIME;
        bool noAvailableSpace = kartLobby.CanAutoSelectLevel && !DevSettings.Settings.ManualLobbyPlayerWaitSwitch && gameLobby.OpenSlots == 0;
        if(Input.GetKeyDown(KartLobby.FORCE_MAP_PICK_KEY) || 
        noAvailableSpace || 
        timePassed || 
        CoreManager.IsLocal) {
            BLog.Log($"Advanced to map selection via ForceMapPick: {Input.GetKeyDown(KartLobby.FORCE_MAP_PICK_KEY)}, noAvailableSpace: {noAvailableSpace}, timePassed: {timePassed}, local: {CoreManager.IsLocal}", LobbyManager.Instance.LogSettingsGameLobby);
            return new MapSelectionState(kartLobby);
        }
        return null;
    }

    public override string GetID() => "WaitingForPlayersState";
}