using FishNet.Object;
using UnityEngine;

public class WallInteraction : NetworkBehaviour
{
    [SerializeField] private Collider wallCollider;
    [SerializeField] private Renderer wallRenderer;

    private bool isDisabled = false;

    /// Called by a client to request the wall be disabled.
    /// Must be marked as RequireOwnership = false if wall isn't client-owned.
    [ServerRpc(RequireOwnership = false)]
    public void Interact()
    {
        Debug.Log("✅ Interact RPC called on server!");

        if (isDisabled)
        {
            Debug.Log("⚠️ Wall already disabled.");
            return;
        }

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
        Debug.Log($"🔧 SetWallState: {(enabled ? "ENABLED" : "DISABLED")}");

        if (wallCollider != null)
            wallCollider.enabled = enabled;

        if (wallRenderer != null)
            wallRenderer.enabled = enabled;
    }
}