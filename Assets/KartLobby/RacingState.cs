using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using UnityEngine;

public class RacingState : KartLobbyState
{
    public RacingState(KartLobby gameLobby) : base(gameLobby)
    {

    }

    public override void Update()
    {
        base.Update();
        
    }

    public override LobbyState CheckForStateChange()
    {
        if(kartLobby.gameplayManager == null)
            return null;

        if(kartLobby.gameplayManager.RaceManager.Phase == RacePhase.FINISHED) {
            kartLobby.AwardPoints();
            return new PostRaceState(kartLobby);
        }
        return null;
    }

    public override string GetID() => "RacingState";
}