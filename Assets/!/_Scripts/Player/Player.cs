using System;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
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

    private PlayerInputManager playerInputManager;

    [SerializeField]
    private new Camera camera;

#region Initializers
    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
        UpdateActiveComponents();
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
        gameplayManager.GetComponent<PlayerObjectManager>().PlayerConnectedEvent += PlayerObjectManager_PlayerConnectedEvent;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager.GetComponent<PlayerObjectManager>().PlayerConnectedEvent -= PlayerObjectManager_PlayerConnectedEvent;
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

    private void PlayerObjectManager_PlayerConnectedEvent(string uuid, Player player)
    {
        if(uuid != uid.Value)
            return;

        ConnectPlayer(uuid, player);
    }

    public void ConnectPlayer(string uuid, Player player) 
    {
        int? idx = PlayerManager.Instance.GetLocalIndex(uuid);
        if(!idx.HasValue) {
            Debug.LogError("Failed to connect player locally, couldn't resolve index.");
            return;
        }

        ConnectPlayer(PlayerManager.Instance.LocalPlayers[idx.Value]);
    }

    public void ConnectPlayer(LocalPlayer localPlayer) 
    {
        if(localPlayer.UID != uid.Value) {
            Debug.LogError($"Failed to connect Player to LocalPlayer, uuids mismatch. Stored on player: \"{uid.Value}\" Attempting to connect: \"{localPlayer.UID}\"");
            return;
        }

        this.localPlayer = localPlayer;
        GetComponent<PlayerInputManager>().ConnectPlayer(localPlayer.Input);       

        UpdateActiveComponents(); 
    }

    /// <summary>
    /// Update the components related to having a localplayer attached or not
    /// </summary>
    private void UpdateActiveComponents() 
    {
        for(int childIdx = 0; childIdx < transform.childCount; childIdx++) {
            GameObject child = transform.GetChild(childIdx).gameObject;
            if(child.name == "Root")
                continue;
            child.SetActive(localPlayer != null);
        }

        GetComponent<PlayerMovement>().enabled = localPlayer != null;
        // GetComponent<ToolBelt>().enabled = localPlayer != null;
    }
}
