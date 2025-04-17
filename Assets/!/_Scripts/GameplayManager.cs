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

    private void Start() 
    {
        SceneSingletons.Register(this);
    }

    private void Update() 
    {
          
    }

    public Transform GetSpawnPosition(string uid) 
    {
        if(!spawnPosAssignments.ContainsKey(uid)) {
            int nextAvailable = GetFirstAvailableSpawnIndex();
            if(nextAvailable == -1)
                throw new InvalidOperationException("Can't assign new spawn position, one's not available. Ensure GameplayManager has enough spawnpositions for all possible players.");
            
            spawnPosAssignments.Add(uid, nextAvailable);
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

}
