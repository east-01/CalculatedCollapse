using System.Collections;
using System.Collections.Generic;
using EMullen.Bootstrapper;
using EMullen.Networking.Lobby;
using UnityEngine;

public class FPSLobbyBootstrapper : MonoBehaviour, IBootstrapComponent
{
    public bool IsLoadingComplete()
    {
        if(LobbyManager.Instance == null)
            return false;

        LobbyManager.Instance.InstantiateLobbyAction = () => new FPSLobby();
        return true;
    }
}
