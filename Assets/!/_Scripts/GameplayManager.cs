using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameplayManager class is a networked scene singleton that controls general gameplay actions.
/// </summary>
public class GameplayManager : NetworkBehaviour
{

    public Scene GameplayScene => gameObject.scene;

    [SerializeField]
    private List<Transform> spawnPoints;
    /// <summary>
    /// A list of spawn position assignments. The integer corresponds to the index in spawnPoints.
    /// </summary>
    private Dictionary<string, int> spawnPosAssignments = new();

    public FPSLobby Lobby;
    private GameObject startingWall;

    private void Awake()
    {
        LobbyManager.Instance.LobbyUpdatedEvent += LobbyManager_LobbyUpdatedEvent;
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.LobbyUpdatedEvent -= LobbyManager_LobbyUpdatedEvent;
    }

    private void Start() 
    {
        SceneSingletons.Register(this);
        startingWall = GameObject.FindWithTag("Starting Wall");
    }

    private void Update()
    {
        if (LobbyManager.Instance.LobbyData.HasValue && LobbyManager.Instance.LobbyData.Value.stateTypeString != typeof(StateInRound).ToString() && LobbyManager.Instance.LobbyData.Value.stateTypeString != typeof(StatePostRound).ToString())
            ResetPlayerHealth();
    }

    public Transform GetSpawnPosition(string uid) 
    {
        if(!spawnPosAssignments.ContainsKey(uid)) {
            int nextAvailable = GetFirstAvailableSpawnIndex();
            if(nextAvailable == -1)
                throw new InvalidOperationException("Can't assign new spawn position, one's not available. Ensure GameplayManager has enough spawnpositions for all possible players.");
            
            spawnPosAssignments.Add(uid, nextAvailable);

            BLog.Highlight($"Assigned {uid} to spawn pos {nextAvailable}");
        }
        return spawnPoints[spawnPosAssignments[uid]];
    }

    /// <summary>
    /// Returns the first available index in spawnPoints that is not already assigned.
    /// </summary>
    /// <returns>Index of first available spawn point, or -1 if all are taken.</returns>
    private int GetFirstAvailableSpawnIndex()
    {
        HashSet<int> assignedIndices = new HashSet<int>(spawnPosAssignments.Values);

        for (int i = 0; i < spawnPoints.Count; i++) {
            if (!assignedIndices.Contains(i)) {
                return i;
            }
        }

        return -1;
    }

    public void ResetPlayerHealth()
    {
        if(Lobby == null)
            return;

        Lobby.Players.ToList().ForEach(playerUID => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(playerUID);
            pd.EnsureFPSData();

            InRoundData data = pd.GetData<InRoundData>();
            data.health = 1f;
            pd.SetData(data);
        });
    }

    private void LobbyManager_LobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason)
    {
        if (newData.stateTypeString == typeof(StatePrepareRound).ToString())
        {
            startingWall.SetActive(true);
        }
        else if (newData.stateTypeString == typeof(StateInRound).ToString())
        {
            startingWall.SetActive(false);
        }
    }

}
