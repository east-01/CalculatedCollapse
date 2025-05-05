using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.MenuController;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

public class LobbyMenuController : MenuController 
{
    [SerializeField]
    private PlayerInfo leftDriver;
    [SerializeField]
    private PlayerInfo rightDriver;

    protected new void Awake()
    {
        base.Awake();

        LobbyManager.Instance.LobbyUpdatedEvent += LobbyManager_LobbyUpdated;
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent += PlayerDataRegistry_PlayerDataUpdated;
    }

    protected new void OnDestroy() 
    {
        base.OnDestroy();

        LobbyManager.Instance.LobbyUpdatedEvent -= LobbyManager_LobbyUpdated;
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent -= PlayerDataRegistry_PlayerDataUpdated;
    }

    protected override void Opened()
    {
        base.Opened();
        UpdateMenu();
    }

    private void LobbyManager_LobbyUpdated(string lobbyID, LobbyData newData, LobbyUpdateReason reason) => UpdateMenu();
    private void PlayerDataRegistry_PlayerDataUpdated(PlayerData playerData, PlayerDataClass newData) => UpdateMenu();

    private void UpdateMenu() 
    {
        void UpdateVisibility(bool lobbyNull) 
        {
            leftDriver.gameObject.SetActive(!lobbyNull);
            rightDriver.gameObject.SetActive(!lobbyNull);
        }

        UpdateVisibility(!LobbyManager.Instance.LobbyData.HasValue);

        if(!LobbyManager.Instance.LobbyData.HasValue)
            return;

        LobbyData data = LobbyManager.Instance.LobbyData.Value;

        if(data.playerUIDs.Count > 2)
            Debug.LogWarning("Don't know how to handle more than 2 players in lobby!");

        string leftUID = null;
        string rightUID = null;

        List<string> localPlayerUIDs = PlayerManager.Instance.LocalPlayers.Where(lp => lp != null).Select(lp => lp.UID).ToList();
        foreach(string uid in data.playerUIDs) {
            if(localPlayerUIDs.Contains(uid))
                leftUID = uid;
            else
                rightUID = uid;
        }

        leftDriver.SetTarget(leftUID);
        rightDriver.SetTarget(rightUID);

    }

}