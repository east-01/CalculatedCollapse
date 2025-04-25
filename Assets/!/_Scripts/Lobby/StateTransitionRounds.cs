using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;

public class StateTransitionRounds : LobbyState
{
    public StateTransitionRounds(GameLobby gameLobby, string winner) : base(gameLobby) 
    {
        BLog.Highlight("Winner of round is is: " + winner);
    }

    public override LobbyState CheckForStateChange()
    {
        if(TimeInState > 3) {
            return new StateInRound(gameLobby);
        } else {
            return null;
        }
    }
}