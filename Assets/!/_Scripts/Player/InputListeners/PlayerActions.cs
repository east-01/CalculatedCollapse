using EMullen.Core;
using EMullen.PlayerMgmt;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player actions input listener class is a catch all for all extra player actions that don't
///   group well.
/// </summary>
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(NetworkedAudioController))]
public class PlayerActions : MonoBehaviour, IInputListener
{
    private Player player;
    private NetworkedAudioController audioController;

    private void Awake()
    {
        player = GetComponent<Player>();
        audioController = GetComponent<NetworkedAudioController>();
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        switch(context.action.name) {
            case "Interact":
                TryInteract();
                break;
        }
    }
    
    private void TryInteract()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f)) // Adjust interaction range as needed
        {
            if (hit.collider.CompareTag("Destructible"))
            {
                WallInteraction wall = hit.collider.GetComponent<WallInteraction>();
                if (wall != null && wall.IsSpawned) // Make sure it's a valid networked object
                {
                    wall.Interact(); // Tell server to disable wall
                }
            }
        }
    }


    public void InputPoll(InputAction action) {}

}
