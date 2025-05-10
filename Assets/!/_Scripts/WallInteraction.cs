using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class WallInteraction : NetworkBehaviour
{
    [SerializeField] private Collider wallCollider;
    [SerializeField] private Renderer wallRenderer;

    private bool isDisabled = false;

    // Called by the local player, runs on the server
    [ServerRpc]
    public void Interact()
    {
        if (isDisabled) return;

        isDisabled = true;

        // Apply change on server
        SetWallState(false);

        // Tell all clients to update their wall state
        SetWallStateObservers(false);
    }

    // Called on all clients by the server
    [ObserversRpc]
    private void SetWallStateObservers(bool enabled)
    {
        SetWallState(enabled);
    }

    // Handles local state toggling (visuals + collider)
    private void SetWallState(bool enabled)
    {
        if (wallCollider != null)
            wallCollider.enabled = enabled;

        if (wallRenderer != null)
            wallRenderer.enabled = enabled;
    }
}
