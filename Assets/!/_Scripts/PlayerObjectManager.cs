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

        List<string> playersToConnect = GetPlayersToConnect();
        if(playersToConnect != null)
            MakeConnections(playersToConnect);

        if(InstanceFinder.IsServerStarted)
            RemoveDisconnectedPlayers();
    }

    /// <summary>
    /// Get the list of playere uids to connect, functionality differs between host/server and 
    ///   clients.
    /// </summary>
    /// <returns>The list of player uids to connect, returns null if shouldn't be connecting.</returns>
    private List<string> GetPlayersToConnect() 
    {
        List<string> playersToConnect = new();

        if(InstanceFinder.IsServerStarted) {
            // If the server is started, this PlayerObjectManager is responsible for spawning all
            //   players in the lobby
            GameplayManager gm = GetComponent<GameplayManager>();
            if(gm.Lobby == null)
                return null;

            playersToConnect = gm.Lobby.Players.ToList();
        } else if(InstanceFinder.IsClientOnlyStarted) {
            if(!LobbyManager.Instance.InLobby)
                return null;

            // If only the client is started, this PlayerObjectManager is responsible for
            //   connecting LocalPlayers to their respective player GameObject.
            playersToConnect = PlayerManager.Instance.LocalPlayers.Where(lp => lp != null).ToList().Select(lp => lp.UID).ToList();
        }

        return playersToConnect;
    }

    /// <summary>
    /// Make player connections. If server, spawn networked player prefabs for players that don't
    ///   have one yet; if client, make connections.
    /// </summary>
    /// <param name="playersToConnect">The players to connect</param>
    private void MakeConnections(List<string> playersToConnect) 
    {
        foreach(string uid in playersToConnect) {

            if(playersObjects.ContainsKey(uid))
                continue;

            IEnumerable<Player> playerOptions = GetComponent<GameplayManager>().GameplayScene.GetRootGameObjects()
            .Where(go => go.GetComponent<Player>() != null)
            .Select(go => go.GetComponent<Player>())
            .Where(player => player.uid.Value == uid);

            Player player = null;

            // Failed to find player, spawn one if we're the server
            if(playerOptions.Count() == 0) {
                if(InstanceFinder.IsServerStarted) {
                    PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
                    NetworkIdentifierData nid = pd.GetData<NetworkIdentifierData>();
                    player = SpawnPlayer(nid.GetNetworkConnection(), uid);
                } else
                    continue; // Clients will wait for their player to be spawned by the server
            } else if(playerOptions.Count() >= 1) {
                if(playerOptions.Count() > 1)
                    Debug.LogError("More than one suitable option for connecting player! Connecting first available option.");

                player = playerOptions.ToArray()[0];
            }

            BLog.Highlight($"Making connection for {uid} got: {player} (is def {player==default})");

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
    private Player SpawnPlayer(NetworkConnection owner, string uid) {

        if(!InstanceFinder.IsServerStarted)
            throw new InvalidOperationException("Can only spawn player on server.");

        GameObject spawnedPlayer = Instantiate(playerPrefab);
        spawnedPlayer.transform.position = spawnPoint.transform.position;
        Player player = spawnedPlayer.GetComponent<Player>();
        player.uid.Value = uid;

        InstanceFinder.ServerManager.Spawn(spawnedPlayer, owner, GetComponent<GameplayManager>().GameplayScene);
        return player;
    }

    /// <summary>
    /// Remove and despawn player objects that are no longer in the lobby.
    /// </summary>
    private void RemoveDisconnectedPlayers() 
    {
        if(!InstanceFinder.IsServerStarted)
            throw new InvalidOperationException("Can only remove disconnected players on the server.");

        // If the server is started, this PlayerObjectManager is responsible for spawning all
        //   players in the lobby
        GameplayManager gm = GetComponent<GameplayManager>();
        if(gm.Lobby == null)
            return;

        IEnumerable<string> lobbyPlayers = gm.Lobby.Players;
        IEnumerable<string> playersToRemove = playersObjects.Keys.Except(lobbyPlayers).ToList();

        foreach(string uid in playersToRemove) {
            Player playerObj = playersObjects[uid];
            Despawn(playerObj.GetComponent<NetworkObject>());

            playersObjects.Remove(uid);
        }
    }

}
