using System.Linq;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;

/// <summary>
/// StateWarmup is for the small amount of time when the players enter the map, before the round
///   starts.
/// Transitions to StateInRound after warmup time passes through.
/// </summary>
public class StateWarmup : LobbyState
{
    public readonly float WARMUP_TIME = 0.5f;

    public StateWarmup(GameLobby gameLobby) : base(gameLobby) 
    {
        // Reset InRoundData for all players
        gameLobby.Players.ToList().ForEach(playerUID => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(playerUID);
            pd.SetData(InRoundData.CreateDefault());
        });
    }

    public override LobbyState CheckForStateChange()
    {
        if(TimeInState < WARMUP_TIME)
            return null;

        return new StatePrepareRound(gameLobby);
    }
}