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
            case "":

                break;
        }
    }

    public void InputPoll(InputAction action) {}

}
