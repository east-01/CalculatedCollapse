using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;

/// <summary>
/// StatePostRound is for after a player dies and the other player is still running around.
/// Transitions to: 
///   - StateInRound if the win count is below FPSLobby#WINS_PER_MAP
///   - StateUnloadScene if the win count is above threshold
/// </summary>
public class StatePostRound : LobbyState
{
    /// <summary> The time for transitions </summary>
    public readonly float POST_ROUND_TIME = 1f;

    private string winner;

    public StatePostRound(GameLobby gameLobby, string winner) : base(gameLobby) 
    {
        this.winner = winner;

        gameLobby.Players.ToList().ForEach(playerUID => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(playerUID);
            pd.EnsureFPSData();

            InRoundData data = pd.GetData<InRoundData>();
            data.health = 1f;
            pd.SetData(data);
        });
    }

    public override LobbyState CheckForStateChange()
    {
        if(TimeInState < 1)
            return null;
        
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(winner);
        pd.EnsureFPSData();

        InRoundData data = pd.GetData<InRoundData>();

        if(data.wins >= FPSLobby.WINS_PER_MAP) {
            // Give win to player
            FPSPlayerData fpsData = pd.GetData<FPSPlayerData>();
            fpsData.wins += 1;
            pd.SetData(fpsData);

            return new StateUnloadScene(gameLobby);
        } else
            return new StateInRound(gameLobby);
    }
}