using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class WallInteraction : NetworkBehaviour
{
    [SerializeField] private Collider wallCollider;
    [SerializeField] private Renderer wallRenderer;
    [SerializeField] private bool isDisableable = true;

    private NetworkedAudioController audioController;

    // Keeps track of all destructible walls, used for resetting on new rounds
    private static List<WallInteraction> allDestructibleWalls = new();

    private bool isDisabled = false;


    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();

        if (!allDestructibleWalls.Contains(this))
        {
            allDestructibleWalls.Add(this);
        }
    }

    private void OnDestroy()
    {
        allDestructibleWalls.Remove(this);
    }

    /// Called by a client to request the wall be disabled.
    [ServerRpc(RequireOwnership = false)]
    public void Interact()
    {
        if (isDisabled || !isDisableable) return;

        audioController.PlaySound("destroy", 0.3f);

        isDisabled = true;

        // Apply server-side state
        SetWallState(false);

        // Sync to all clients
        SetWallStateObservers(false);
    }

    /// Updates the state of the wall on all clients.
    [ObserversRpc]
    private void SetWallStateObservers(bool enabled)
    {
        SetWallState(enabled);
    }

    /// Disables/enables the wall collider and visuals.
    private void SetWallState(bool enabled)
    {
        if (wallCollider != null)
            wallCollider.enabled = enabled;

        if (wallRenderer != null)
            wallRenderer.enabled = enabled;
    }

    // Reset all walls used for resetting the walls at the beginning of each round
    [Server]
    public static void ResetAllWalls()
    {
        foreach (WallInteraction wall in allDestructibleWalls)
        {
            wall.isDisabled = false;
            wall.isDisableable = true;
            wall.SetWallState(true);
            wall.SetWallStateObservers(true);
        }
    }

    // Makes it so that the walls cant be disabled after the round starts
    [Server]
    public static void LockAllWalls()
    {
        foreach (WallInteraction wall in allDestructibleWalls)
        {
            wall.isDisableable = false;
        }
    }
}