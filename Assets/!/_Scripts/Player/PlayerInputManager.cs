using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The PlayerInputManager holds a list of IInputListener objects associated with a string
///   actionName, only one IInputListener per action.
/// IInputListeners can recieve PlayerInput events (returns InputAction.CallbackContext) or poll
///   the InputActionAsset object itself.
/// IInputListeners can either be added through the inspector or programmatically via SubscribeListener.
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    [SerializeField]
    private List<InputListenerListItem> inputListeners;

    private PlayerInput input;
    private Dictionary<string, IInputListener> listeners = new();
    private List<string> pollingListeners = new();

    private void Awake() 
    {
        inputListeners.ForEach(listItem => {
            if(listItem.listener is not IInputListener) {
                Debug.LogError($"Skipping subscription of action type \"{listItem.actionName}\" the provided MonoBehaviour is not an instance of IInputListener");
                return;
            }
            SubscribeListener(listItem.actionName, listItem.listener as IInputListener, listItem.pollInput);
        });
    }

    public void ConnectPlayer(PlayerInput input) 
    {
        input.onActionTriggered += PlayerControls_OnActionTriggered;
        this.input = input;
    }

    private void Update()
    {
        if(input == null)
            return;
            
        pollingListeners.ForEach(actionName => listeners[actionName].InputPoll(input.actions[actionName]));
    }

    private void PlayerControls_OnActionTriggered(InputAction.CallbackContext context)
    {
        string actionName = context.action.name;
        if(!listeners.ContainsKey(actionName))
            return;

        listeners[actionName].InputEvent(context);
    }

    public void SubscribeListener(string actionName, IInputListener listener, bool pollInput = false) 
    {
        if(listeners.ContainsKey(actionName)) 
            throw new InvalidOperationException($"Tried to subscribe listener to action \"{actionName}\" but it's already registered.");

        listeners.Add(actionName, listener);

        if(pollInput)
            pollingListeners.Add(actionName);
    }

    public void UnsubscribeListener(string actionName) 
    {
        if(listeners.ContainsKey(actionName))
            listeners.Remove(actionName);
        
        if(pollingListeners.Contains(actionName))
            listeners.Remove(actionName);
    }
}

public interface IInputListener 
{
    public void InputEvent(InputAction.CallbackContext context);
    public void InputPoll(InputAction action);
}

/// <summary>
/// A wrapper struct to represent an action name, input listener, and poll input combo; it allows
///   us to insert InputListeners from the inspector.
/// </summary>
[Serializable]
public struct InputListenerListItem 
{
    public string actionName;
    public MonoBehaviour listener;
    public bool pollInput;
}