using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using FishNet.Connection;
using UnityEngine;

public class StateWarmup : LobbyState
{
    [SerializeField]
    private float countdownTime = 0.5f;

    private Phase phase;
    private float countdownTarget; // The time in state that the countdown will end

    public StateWarmup(GameLobby gameLobby) : base(gameLobby) {}

    public override LobbyState CheckForStateChange()
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        if(phase == Phase.WAITING_FOR_PLAYERS) {
            if(CheckPlayers()) {
                countdownTarget = TimeInState + countdownTime;
                phase = Phase.COUNTDOWN;
            }
        } else if(phase == Phase.COUNTDOWN && TimeInState > countdownTarget)
            return new StateInRound(lobby);

        return null;
    }

    private bool CheckPlayers() 
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        if(lobby.GameplayManager == null)
            return false;

        PlayerObjectManager pom = lobby.GameplayManager.GetComponent<PlayerObjectManager>();
        List<string> players = lobby.Players.ToList();
        bool allPlayersSpawned = players.All(uid => pom.GetPlayer(uid) != null);

        // Ensure all players have data
        players.ForEach(player => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(player);
            if(!pd.HasData<InRoundData>())
                pd.SetData(new InRoundData(1, 0, "default"));
        });

        bool allPlayersHaveRoundData = players.All(uid => {
            return PlayerDataRegistry.Instance.Contains(uid) && PlayerDataRegistry.Instance.GetPlayerData(uid).HasData<InRoundData>();
        });
        if(!allPlayersHaveRoundData)
            return false;

        if(!allPlayersSpawned)
            return false;

        if(lobby.Players.Count < 2 && !Input.GetKeyDown(KeyCode.B))
            return false;

        if(Input.GetKeyDown(KeyCode.B))
            BLog.Highlight("Bypassed player limit");

        return true;
    }

    private enum Phase { WAITING_FOR_PLAYERS, COUNTDOWN }
}