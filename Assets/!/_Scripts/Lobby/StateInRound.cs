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

public class StateInRound : LobbyState
{
    public StateInRound(GameLobby gameLobby) : base(gameLobby) 
    {
        FPSLobby lobby = gameLobby as FPSLobby;
        GameplayManager gm = lobby.GameplayManager;
        PlayerObjectManager pom = gm.GetComponent<PlayerObjectManager>();

        foreach(string uid in lobby.Players) 
        {
            Player player = pom.GetPlayer(uid);
            if(player == null) {
                // Debug.LogWarning("Can't spawn player player is null");
                continue;
            }
            
            Transform pos = gm.GetSpawnPosition(uid);

            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
            NetworkIdentifierData nid = pd.GetData<NetworkIdentifierData>();
            NetworkConnection conn = nid.GetNetworkConnection();

            if(InstanceFinder.ClientManager.Connection.IsValid && InstanceFinder.ClientManager.Connection == conn) {
                // We don't need to send a TargetRPC to this player, they're the host
                player.SetPositionAndRotation(pos.position, pos.rotation);
            } else {
                player.TargetRPCSetPositionAndRotation(conn, pos.position, pos.rotation);
            }
        }
    }

    public override LobbyState CheckForStateChange()
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        // TEMP: Kill random player
        if(Input.GetKeyDown(KeyCode.K)) {
            string uid = lobby.Players[UnityEngine.Random.Range(0, lobby.Players.Count)];
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
            InRoundData data = pd.GetData<InRoundData>();
            data.health = 0;
            pd.SetData(data);
        }

        // if(gameLobby.PlayerCount < 2)
        //     return null;
        if(TimeInState < 5) {
            if(Mathf.Floor(TimeInState) != Mathf.Floor(TimeInState - Time.deltaTime))
                BLog.Log("waiting for timer");
            return null;
        }
    
        // Find the winner uid to transition to a winning state.
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

        if(winnerUID != null) {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(winnerUID);
            InRoundData data = pd.GetData<InRoundData>();
            data.wins += 1;
            pd.SetData(data);
            return new StateTransitionRounds(gameLobby, winnerUID);
        } else {
            return null;
        }
    }
}