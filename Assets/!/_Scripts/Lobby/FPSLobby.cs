using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using FishNet.Managing.Scened;
using Unity.VisualScripting;
using UnityEngine;

public class FPSLobby : GameLobby 
{
    public static readonly int WINS_PER_MAP = 3;
    public static readonly int REQUIRED_PLAYERS = 2;

    public GameplayManager GameplayManager { get; private set; }

    public SceneLookupData GameplayScene { get; private set; }

    public FPSLobby() : base() 
    {
        State = new StateInLobby(this);
        GameplayScene = null;
    
        LobbyManager.Instance.LobbyUpdatedEvent += LobbyManager_LobbyUpdatedEvent;
    }

    ~FPSLobby()
    {
        LobbyManager.Instance.LobbyUpdatedEvent -= LobbyManager_LobbyUpdatedEvent;
    }

    public override void Update() 
    {
        base.Update();

        if(GameplayScene is not null && GameplayManager == null)
            ConnectGameplayManager(GameplayScene);
    }

    public override bool Joinable() => PlayerCount < REQUIRED_PLAYERS;

    public override void ClaimedScene(SceneLookupData sceneLookupData)
    {
        base.ClaimedScene(sceneLookupData);

        GameplayScene = sceneLookupData;
        BLog.Highlight($"Set gameplay scene to {GameplayScene}");

        if(SceneSingletons.Contains(sceneLookupData, typeof(GameplayManager)))
            ConnectGameplayManager(sceneLookupData);
    }

    public override void UnclaimedScene(SceneLookupData sceneLookupData)
    {
        base.UnclaimedScene(sceneLookupData);

        if(sceneLookupData != GameplayScene)
            return;

        DisconnectGameplayManager(sceneLookupData);
        GameplayScene = null;
    }

    private void ConnectGameplayManager(SceneLookupData sld) 
    {
        if(GameplayScene is not null && GameplayScene != sld)
            throw new InvalidOperationException($"Can't connect gameplay manager to scene lookup data {sld} since we're currently waiting on {GameplayScene}");
        
        GameplayManager = SceneSingletons.Get(sld, typeof(GameplayManager)) as GameplayManager;
        GameplayManager.Lobby = this;
    }

    private void DisconnectGameplayManager(SceneLookupData sld) 
    {
        GameplayManager.Lobby = null;
        GameplayManager = null;
    }

    private void GiveWin(string uid) 
    {
        BLog.Highlight($"TODO: Give win to {uid}");
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        pd.EnsureFPSData();

        FPSPlayerData fpsData = pd.GetData<FPSPlayerData>();
        fpsData.wins += 1;
    }

    private void LobbyManager_LobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason)
    {
        if(lobbyID != ID)
            return;

        if(reason == LobbyUpdateReason.PLAYER_LEAVE) {
            if(Players.Count == 1) {
                GiveWin(Players[0]);
            }
            State = new StateWarmup(this);
        }
    }

}