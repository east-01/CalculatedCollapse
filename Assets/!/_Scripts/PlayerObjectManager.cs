using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking;
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
    public Player GetPlayer(string uid) => playersObjects[uid];

    public delegate void PlayerConnectedDelegate(string uuid, Player player);
    /// <summary>
    /// This event is called when the Player object is attached to the PlayerObjectManager
    /// </summary>
    public event PlayerConnectedDelegate PlayerConnectedEvent;

    private void OnEnable() 
    {
        SceneController.Instance.ClientNetworkedSceneEvent += SceneController_ClientNetworkedSceneEvent;

        playersObjects.Clear();
        PlayerManager.Instance.LocalPlayers.Where(lp => lp != null).ToList().ForEach(lp => playersObjects.Add(lp.UID, null));
    }

    private void OnDisable() 
    {
        SceneController.Instance.ClientNetworkedSceneEvent -= SceneController_ClientNetworkedSceneEvent;
    }

    private void Update() 
    {
        if(InstanceFinder.IsClientStarted) {
            foreach(string uid in new List<string>(playersObjects.Keys)) {
                if(playersObjects[uid] == null) {
                    Player player = GetComponent<GameplayManager>().GameplayScene.GetRootGameObjects()
                    .Where(go => go.GetComponent<Player>() != null)
                    .Select(go => go.GetComponent<Player>())
                    .FirstOrDefault(player => player.uid.Value == uid);

                    // Failed to find player
                    if(player == default)
                        continue;

                    playersObjects[uid] = player;
                    PlayerConnectedEvent?.Invoke(uid, player);
                    // The player won't recieve PlayerConnectedEvent, they subscribe to it after it's called.
                    player.ConnectPlayer(uid, player);
                }
            }
        }
    }

    private void SceneController_ClientNetworkedSceneEvent(NetworkConnection client, SceneLookupData scene, ClientNetworkedScene.Action action)
    {
        if(scene.Name == "GameplayScene") {
            PlayerDataRegistry.Instance.GetAllData()
            .Where(data => data.GetData<NetworkIdentifierData>().clientID == client.ClientId || data.GetData<NetworkIdentifierData>().clientID == -1)
            .Select(data => data.GetUID())
            .ToList()
            .ForEach(uid => GetComponent<PlayerObjectManager>().SpawnPlayer(client, uid));
        }
    }

    public void SpawnPlayer(NetworkConnection owner, string uid) {

        if(!InstanceFinder.IsServerStarted) {
            ServerRpcSpawnPlayer(LocalConnection, uid);
            return;
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab);
        spawnedPlayer.transform.position = spawnPoint.transform.position;
        spawnedPlayer.GetComponent<Player>().uid.Value = uid;

        InstanceFinder.ServerManager.Spawn(spawnedPlayer, owner, GetComponent<GameplayManager>().GameplayScene);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcSpawnPlayer(NetworkConnection owner, string uid) => SpawnPlayer(owner, uid);


}
