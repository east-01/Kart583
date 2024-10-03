using EMullen.Networking.Lobby;
using EMullen.SceneMgmt;
using FishNet.Managing.Scened;

public class PostRaceState : KartLobbyState
{
    public PostRaceState(KartLobby gameLobby) : base(gameLobby)
    {

    }

    public override void Update()
    {
        base.Update();
        
    }

    public override LobbyState CheckForStateChange()
    {
        bool kickPlayersOut = CoreManager.IsMultiplayer && TimeInState >= KartLobby.ROUND_END_TIME;
        bool noMapScene = kartLobby.MapSceneData == null || !NetSceneController.Instance.IsSceneRegistered(kartLobby.MapSceneData);
        bool mapSceneEmpty = NetSceneController.Instance.GetSceneElements(kartLobby.MapSceneData).Clients.Count == 0;
        if(kickPlayersOut || noMapScene || mapSceneEmpty) {
            kartLobby.MovePlayersToLobby();
            return new WaitingForPlayersState(kartLobby);
        }
        return null;
    }

    public override string GetID() => "PostRaceState";
}