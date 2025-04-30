using System.Linq;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using UnityEngine;

/// <summary>
/// StateInLobby is for when the players are in the lobby scene, interacting with the
///   LobbySceneMenucontroller.
/// State transitions to loading scene state when the required player count is met, and all players
///   are ready.
/// </summary>
public class StateInLobby : LobbyState
{
    public StateInLobby(GameLobby gameLobby) : base(gameLobby) {}

    public override LobbyState CheckForStateChange()
    {
        FPSLobby lobby = gameLobby as FPSLobby;

        if(Input.GetKeyDown(KeyCode.B)) {
            BLog.Highlight("Bypassed lobby");
            return new StateLoadScene(lobby);
        }

        if(lobby.Players.Count < 2)
            return null;

        bool allReady = lobby.Players.All(playerUID => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(playerUID);
            pd.EnsureFPSData();

            InRoundData data = pd.GetData<InRoundData>();
            return data.ready;
        });

        if(!allReady)
            return null;

        lobby.Players.ToList().ForEach(playerUID => {
            PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(playerUID);
            pd.EnsureFPSData();

            InRoundData data = pd.GetData<InRoundData>();
            data.ready = false;
            pd.SetData(data);
        });

        return new StateLoadScene(lobby);
    }
}