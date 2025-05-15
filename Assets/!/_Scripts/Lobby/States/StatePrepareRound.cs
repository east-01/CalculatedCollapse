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
public class StatePrepareRound : LobbyState
{
    public static readonly float PREPARE_TIME = 3;

    public StatePrepareRound(GameLobby gameLobby) : base(gameLobby) 
    {
        SpawnPlayers();

        // Reset the walls
        WallInteraction.ResetAllWalls();
    }

    public override LobbyState CheckForStateChange()
    {
        if(TimeInState < PREPARE_TIME)
            return null;

        return new StateInRound(gameLobby);
    }

    private void SpawnPlayers() 
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
}