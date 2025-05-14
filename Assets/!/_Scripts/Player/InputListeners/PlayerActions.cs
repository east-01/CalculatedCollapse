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
    public Camera fpsCam;

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
        Ray ray = fpsCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            Debug.Log($"Ray hit: {hit.collider.name} with tag: {hit.collider.tag}");
            if (hit.collider.CompareTag("Destructible"))
            {
                WallInteraction wall = hit.collider.GetComponent<WallInteraction>();
                if (wall != null && wall.IsSpawned)
                    wall.Interact();
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing.");
        }
    }


    public void InputPoll(InputAction action) {}

}
