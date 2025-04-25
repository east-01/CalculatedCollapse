using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet.Managing.Scened;

public static class FPSPlayerExtensions 
{
    public static FPSLobby GetLobby(this Player player) 
    {
        SceneLookupData sld = player.gameObject.scene.GetSceneLookupData();
        if(!SceneSingletons.Contains(sld, typeof(GameplayManager)))
            return null;

        GameplayManager gm = SceneSingletons.Get(sld, typeof(GameplayManager)) as GameplayManager;
        return gm.Lobby;
    }
}