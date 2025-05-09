using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player class is the top level controller for the Player prefab, it is activated by the
///   ConnectPlayer() call when the PlayerManager sends a LocalPlayer.
/// </summary>
[RequireComponent(typeof(PlayerInputManager))]
public class Player : NetworkBehaviour, IS3
{
    public readonly SyncVar<string> uid = new();
#if UNITY_EDITOR
    [SerializeField]
    private string uidReadout; // Here to show uid in editor
#endif

    public bool HasPlayerData => PlayerDataRegistry.Instance != null && uid.Value != null && PlayerDataRegistry.Instance.Contains(uid.Value);
    public PlayerData PlayerData => PlayerDataRegistry.Instance.GetPlayerData(uid.Value);

    private GameplayManager gameplayManager;
    private LocalPlayer localPlayer;
    public bool HasLocalPlayer => localPlayer != null;

    private PlayerInputManager playerInputManager;

    [SerializeField]
    private new Camera camera;
    /// <summary>
    /// A list of components that should be active/inactive
    /// </summary>
    [SerializeField]
    private List<Behaviour> activeComponents;

#region Initializers
    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
        SetPlayerActive(localPlayer != null);
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }
#endregion

    private void Update() 
    {
#if UNITY_EDITOR
        uidReadout = uid.Value;
#endif

        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }

        // Mute AudioListener if there's no player.
        bool localPlayerExists = localPlayer != null && localPlayer.Input != null;

        if(!localPlayerExists && gameObject.GetComponentInChildren<AudioListener>() != null) {
            gameObject.GetComponentInChildren<AudioListener>().gameObject.SetActive(false);
        }
    }

    public void ConnectPlayer(string uuid, Player player) 
    {
        int? idx = PlayerManager.Instance.GetLocalIndex(uuid);
        if(idx.HasValue) {
           ConnectPlayer(PlayerManager.Instance.LocalPlayers[idx.Value]);
        }

        SetPlayerActive(localPlayer != null);
    }

    public void ConnectPlayer(LocalPlayer localPlayer) 
    {
        if(localPlayer.UID != uid.Value) {
            Debug.LogError($"Failed to connect Player to LocalPlayer, uuids mismatch. Stored on player: \"{uid.Value}\" Attempting to connect: \"{localPlayer.UID}\"");
            return;
        }

        this.localPlayer = localPlayer;
        GetComponent<PlayerInputManager>().ConnectPlayer(localPlayer.Input);       
    }

    /// <summary>
    /// Update behaviours that should be enabled/disabled if the general player actions should be
    ///   enabled/disabled.
    /// </summary>
    public void SetPlayerActive(bool active) => activeComponents.ForEach(beh => beh.enabled = active);

    /// <summary>
    /// Set the player's position and rotation, requires disabling CharacterController for
    ///   teleports.
    /// </summary>
    /// <param name="position">The target position.</param>
    /// <param name="rotation">The target rotation.</param>
    public void SetPositionAndRotation(Vector3 position, Quaternion rotation) 
    {
        CharacterController cc = GetComponent<CharacterController>();

        cc.enabled = false;
        gameObject.transform.SetPositionAndRotation(position, rotation);
        cc.enabled = true;
    }

    /// <summary>
    /// Teleport the player on the target. This is necessary as the target's CharacterController
    ///   will block the teleport, even if the server demands it. This problem should be fixed
    ///   once we move to server authoritative movement.
    /// </summary>
    /// <param name="connection">The target connection.</param>
    /// <param name="position">The target position.</param>
    /// <param name="rotation">The target rotation.</param>
    [TargetRpc]
    public void TargetRPCSetPositionAndRotation(NetworkConnection connection, Vector3 position, Quaternion rotation) => SetPositionAndRotation(position, rotation);
}
