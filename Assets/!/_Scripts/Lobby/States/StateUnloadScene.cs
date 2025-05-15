using EMullen.Networking.Lobby;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;

/// <summary>
/// StateUnloadScene faciliates the map scene unloading process.
/// Transitions to StateInLobby right away.
/// </summary>
public class StateUnloadScene : LobbyState
{
    public StateUnloadScene(GameLobby gameLobby) : base(gameLobby) 
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        SceneUnloadData sud = new SceneUnloadData(lobby.GameplayScene);
        InstanceFinder.SceneManager.UnloadConnectionScenes(lobby.Connections.ToArray(), sud);
        
        foreach(NetworkConnection conn in lobby.Connections) {
            SceneController.Instance.LoadScenesOnConnection(conn, new() {new("LobbyScene")});
        }
    }

    public override LobbyState CheckForStateChange()
    {
        return new StateInLobby(gameLobby);
    }

}