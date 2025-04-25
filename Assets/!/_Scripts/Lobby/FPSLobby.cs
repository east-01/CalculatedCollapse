using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using FishNet.Managing.Scened;
using UnityEngine;

public class FPSLobby : GameLobby 
{

    public GameplayManager GameplayManager { get; private set; }

    private SceneLookupData waitingForGM;

    public FPSLobby() : base() 
    {
        State = new StateWarmup(this);
        waitingForGM = null;
    }

    public override void Update() 
    {
        base.Update();

        if(waitingForGM is not null)
            ConnectGameplayManager(waitingForGM);
    }

    public override void ClaimedScene(SceneLookupData sceneLookupData)
    {
        base.ClaimedScene(sceneLookupData);

        if(!SceneSingletons.Contains(sceneLookupData, typeof(GameplayManager)))
            waitingForGM = sceneLookupData;
        else
            ConnectGameplayManager(sceneLookupData);
    }

    private void ConnectGameplayManager(SceneLookupData sld) 
    {
        if(waitingForGM is not null && waitingForGM != sld)
            throw new InvalidOperationException($"Can't connect gameplay manager to scene lookup data {sld} since we're currently waiting on {waitingForGM}");
        
        GameplayManager = SceneSingletons.Get(sld, typeof(GameplayManager)) as GameplayManager;
        GameplayManager.Lobby = this;
        waitingForGM = null;
        Debug.LogWarning("TODO: We'll have to dispose of the gameplay manager, need to polish scene claiming/unclaiming.");
    }

}