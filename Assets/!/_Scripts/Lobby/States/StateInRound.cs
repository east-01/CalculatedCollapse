using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// StateInRound watches players while they're in the round.
/// Transitions to StatePostRound once a winning player is found (last player standing).
/// </summary>
public class StateInRound : LobbyState
{
    public static readonly float MIN_ROUND_TIME = 3f;
    public static readonly float ROUND_TIME = 60 * 3;

    public StateInRound(GameLobby gameLobby) : base(gameLobby) 
    {
        WallInteraction.LockAllWalls();
    }

    public override LobbyState CheckForStateChange()
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        // Find the winner uid to transition to a winning state.
        string winnerUID = FindWinnerUID();

        if(Input.GetKeyDown(KeyCode.B)) {
            BLog.Highlight("Bypassed winner");
            winnerUID = lobby.Players[UnityEngine.Random.Range(0, lobby.Players.Count)];
        }

        // No transition if there's no winner
        if(winnerUID == null)
            return null;

        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(winnerUID);
        InRoundData data = pd.GetData<InRoundData>();
        data.wins += 1;
        pd.SetData(data);

        return new StatePostRound(gameLobby, winnerUID);
    }

    private string FindWinnerUID() 
    {
        // No winner if player count is less than required players
        if(gameLobby.PlayerCount < FPSLobby.REQUIRED_PLAYERS)
            return null;

        if(TimeInState < MIN_ROUND_TIME)
            return null;

        string winnerUID = null;
        foreach(string uid in gameLobby.Players) {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
            InRoundData data = pd.GetData<InRoundData>();

            // The player with >0 health is still standing and is the winner.
            if(data.health > 0) {
                if(winnerUID == null) {
                    winnerUID = uid;
                } else {
                    // If we want to select a winner and there already is one, that means there is
                    //   no current winner
                    winnerUID = null;
                    break;
                }
            }
        }
        return winnerUID;
    }
}