using System.Collections;
using System.Collections.Generic;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The PlayerHUDMenuController class is responsible for controlling everything in the player's
///   pov, shows warning messages, balance, and tool belt.
/// </summary>
public class PlayerHUDMenuController : MenuController
{

    private Player player;

    protected new void Awake()
    {
        base.Awake();
    
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
    }

    private void Update()
    {
        if(player == null && player.uid.Value != null)
            return;
        
    }

}
