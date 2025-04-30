using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The PlayerInfo MonoBehaviour takes a target UID and applies it's relevant PlayerDataClass
///   values to the UI elements.
/// </summary>
public class PlayerInfo : MonoBehaviour 
{
    [SerializeField]
    private TMP_Text nullText; // The text that shows when there is no Player

    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text winsText;

    [SerializeField]
    private GameObject readyButton;
    [SerializeField]
    private TMP_Text readyText;

    private string uid;

    public void SetTarget(string uid) 
    {
        this.uid = uid;   
        UpdateMenu();
    }    

    public void UpdateMenu() 
    {

        void UpdateVisibility(bool playerNull) {
            nullText.gameObject.SetActive(playerNull);
            nameText.gameObject.SetActive(!playerNull);
            winsText.gameObject.SetActive(!playerNull);
            // Get updated later when we have access to InRoundData
            readyButton.SetActive(false);
            readyText.gameObject.SetActive(false);
        }

        UpdateVisibility(uid == null);

        if(uid == null)
            return;

        if(!PlayerDataRegistry.Instance.Contains(uid))
            throw new InvalidOperationException($"Cannot set target to uid \"{uid}\" it is not in the registry.");

        // Check if the player is local.
        List<string> localPlayerUIDs = PlayerManager.Instance.LocalPlayers.Where(lp => lp != null).Select(lp => lp.UID).ToList();
        bool isLocal = localPlayerUIDs.Contains(uid);

        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        pd.EnsureFPSData();

        FPSPlayerData fpsData = pd.GetData<FPSPlayerData>();

        nameText.text = fpsData.displayName;
        winsText.text = "Wins: " + fpsData.wins;

        InRoundData irData = pd.GetData<InRoundData>();
        
        if(isLocal) {
            readyButton.SetActive(!irData.ready);
            readyText.gameObject.SetActive(irData.ready);
            readyText.text = "Ready";
        } else {
            readyText.gameObject.SetActive(true);
            readyText.text = irData.ready ? "Ready" : "Waiting for ready...";
        }
    }

    public void ReadyUp() 
    {
        if(uid == null)
            throw new InvalidOperationException("Can't ready, uid is null.");
        
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        InRoundData data = pd.GetData<InRoundData>();

        data.ready = true;

        pd.SetData(data);
    }
}