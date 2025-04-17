using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The PlayerObjectManager is a network behaviour class that keeps track of the spawned Player
///   prefabs and keeps them connected to a uid.
/// </summary>
public class PlayerObjectManager : NetworkBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject spawnPoint;

    /// <summary>
    /// A client-side dictionary containing string uids and the attached playersObjects
    /// </summary>
    private Dictionary<string, Player> playersObjects = new();
    public Player GetPlayer(string uid) => playersObjects.ContainsKey(uid) ? playersObjects[uid] : null;

    private void OnEnable() 
    {
        playersObjects.Values.ToList().ForEach(po => Destroy(po));
        playersObjects.Clear();
    }

    private void Update() 
    {
        // If the network isn't active do nothing.
        if(!InstanceFinder.IsServerStarted && !InstanceFinder.IsClientStarted)
            return;

        List<string> playersToConnect = new();

        if(InstanceFinder.IsServerStarted) {
            // If the server is started, this PlayerObjectManager is responsible for spawning all
            //   players in the lobby
            GameplayManager gm = GetComponent<GameplayManager>();
            if(gm.Lobby == null)
                return;

            playersToConnect = gm.Lobby.Players.ToList();
        } else if(InstanceFinder.IsClientOnlyStarted) {
            // If only the client is started, this PlayerObjectManager is responsible for
            //   connecting LocalPlayers to their respective player GameObject.
            playersToConnect = PlayerManager.Instance.LocalPlayers.Where(lp => lp != null).ToList().Select(lp => lp.UID).ToList();
        }

        foreach(string uid in playersToConnect) {

            if(playersObjects.ContainsKey(uid))
                continue;

            Player player = GetComponent<GameplayManager>().GameplayScene.GetRootGameObjects()
            .Where(go => go.GetComponent<Player>() != null)
            .Select(go => go.GetComponent<Player>())
            .FirstOrDefault(player => player.uid.Value == uid);

            // Failed to find player, spawn one if we're the server
            if((player == default || player == null) && InstanceFinder.IsServerStarted) {
                if(InstanceFinder.IsServerStarted) {
                    PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
                    NetworkIdentifierData nid = pd.GetData<NetworkIdentifierData>();
                    player = SpawnPlayer(nid.GetNetworkConnection(), uid);
                } else
                    continue; // Clients will wait for their player to be spawned by the server
            }

            playersObjects.Add(uid, player);
            // The player won't recieve PlayerConnectedEvent, they subscribe to it after it's called.
            player.ConnectPlayer(uid, player);
        }

    }

    /// <summary>
    /// Spawn a player with a specific owner and uid, the player will be connected by the Update
    ///   method.
    /// Returns the spawned player object- only usable for the server that is hosting local
    ///    clients to instantly make the connection, otherwise the player will be connected
    ///    next update call.
    /// </summary>
    /// <param name="owner">The NetworkConnection owner of the player</param>
    /// <param name="uid">The uid that will be assigned to the player</param>
    /// <returns>The spawned Player object.</returns>
    public Player SpawnPlayer(NetworkConnection owner, string uid) {

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcSpawnPlayer(LocalConnection, uid);
            return null;
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab);
        spawnedPlayer.transform.position = spawnPoint.transform.position;
        Player player = spawnedPlayer.GetComponent<Player>();
        player.uid.Value = uid;

        InstanceFinder.ServerManager.Spawn(spawnedPlayer, owner, GetComponent<GameplayManager>().GameplayScene);
        return player;
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcSpawnPlayer(NetworkConnection owner, string uid) => SpawnPlayer(owner, uid);


}
