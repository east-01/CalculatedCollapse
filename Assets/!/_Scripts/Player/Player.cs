using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

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
    /// A list of action settings to be parsed on awake.
    /// The action settings objects' can only be a GameObject (for SetActive() call) or Behaviour
    ///   (for .enabled variable).
    /// </summary>
    [Header("Action setting objects are GameObject or Behaviour")]
    [SerializeField]
    private List<AttachSetting> attachSettings;
    private Dictionary<AttachBehaviour, List<GameObject>> gameObjectAttachSettings;
    private Dictionary<AttachBehaviour, List<Behaviour>> behaviourAttachSettings; 

    public bool isPaused = false;

#region Initializers
    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        ParseAttachBehaviours();
        UpdateAttachBehaviours();
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

        string uidOut = uid.Value != null ? uid.Value[1..7] : "nouid";
        if(localPlayer != null)
            uidOut += $" local{localPlayer.Input.playerIndex}";
        gameObject.name = $"Player ({uidOut})";

        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }
    }

    public void ConnectPlayer(string uuid, Player player) 
    {
        int? idx = PlayerManager.Instance.GetLocalIndex(uuid);
        if(idx.HasValue) {
            ConnectPlayer(PlayerManager.Instance.LocalPlayers[idx.Value]);
        } else {
            UpdateAttachBehaviours();
            gameObject.name = "Player unattached";
        }
    }

    public void ConnectPlayer(LocalPlayer localPlayer) 
    {
        if(localPlayer.UID != uid.Value) {
            Debug.LogError($"Failed to connect Player to LocalPlayer, uuids mismatch. Stored on player: \"{uid.Value}\" Attempting to connect: \"{localPlayer.UID}\"");
            return;
        }

        this.localPlayer = localPlayer;
        GetComponent<PlayerInputManager>().ConnectPlayer(localPlayer.Input);       

        UpdateAttachBehaviours();

        gameObject.name = $"Player (LocalPlayer {localPlayer.Input.playerIndex})";
    }

    private void ParseAttachBehaviours() 
    {
        gameObjectAttachSettings = new();
        behaviourAttachSettings = new();

        List<UnityEngine.Object> parsedObjects = new();

        foreach(AttachSetting attachSetting in attachSettings) {
            AttachBehaviour behaviour = attachSetting.behaviour;
            UnityEngine.Object obj = attachSetting.obj;

            if(parsedObjects.Contains(obj)) {
                Debug.LogWarning($"Already parsed object \"{obj}\"... skipping");
                continue;
            }

            if(obj is GameObject) {

                if(!gameObjectAttachSettings.ContainsKey(behaviour))
                    gameObjectAttachSettings.Add(behaviour, new());
                
                List<GameObject> gos = gameObjectAttachSettings[behaviour];
                gos.Add(obj as GameObject);
                gameObjectAttachSettings[behaviour] = gos;

            } else if(obj is Behaviour) {

                if(!behaviourAttachSettings.ContainsKey(behaviour))
                    behaviourAttachSettings.Add(behaviour, new());
                
                List<Behaviour> behs = behaviourAttachSettings[behaviour];
                behs.Add(obj as Behaviour);
                behaviourAttachSettings[behaviour] = behs;

            } else {
                Debug.LogError($"Action setting object \"{obj}\" is not a GameObject or Behavior! This is not allowed.");
            }

        }

    }

    public void UpdateAttachBehaviours() 
    {
        Dictionary<AttachBehaviour, bool> states = new() {
            { AttachBehaviour.ACTIVE_ATTACHED, localPlayer != null },
            { AttachBehaviour.ACTIVE_UNPAUSED, localPlayer != null && !isPaused },
            { AttachBehaviour.DISABLED_ATTACHED, localPlayer == null }
        };

        foreach(AttachBehaviour behaviour in states.Keys) {
            if(gameObjectAttachSettings.ContainsKey(behaviour)) {
                foreach(GameObject go in gameObjectAttachSettings[behaviour]) {
                    go.SetActive(states[behaviour]);                    
                }
            }

            if(behaviourAttachSettings.ContainsKey(behaviour)) {
                foreach(Behaviour beh in behaviourAttachSettings[behaviour]) {
                    beh.enabled = states[behaviour];                    
                }
            }
        }

    }

    [Serializable]
    public enum AttachBehaviour { 
        ACTIVE_ATTACHED, // The object is active when there is a local player attached
        ACTIVE_UNPAUSED, // The object is active when there is a local player attached, and the pause menu is not shown
        DISABLED_ATTACHED // The object is inactive when there is a local player attached
    }

    [Serializable]
    public struct AttachSetting 
    {
        public UnityEngine.Object obj;
        public AttachBehaviour behaviour;
    }

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
