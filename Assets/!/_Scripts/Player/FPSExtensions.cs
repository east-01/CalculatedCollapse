using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet.Managing.Scened;

public static class FPSExtensions 
{
    public static FPSLobby GetLobby(this Player player) 
    {
        SceneLookupData sld = player.gameObject.scene.GetSceneLookupData();
        if(!SceneSingletons.Contains(sld, typeof(GameplayManager)))
            return null;

        GameplayManager gm = SceneSingletons.Get(sld, typeof(GameplayManager)) as GameplayManager;
        return gm.Lobby;
    }

    /// <summary>
    /// Check if the PlayerData has the required FPS PlayerDataClasses, and give them defaults if
    ///   they don't.
    /// </summary>
    public static void EnsureFPSData(this PlayerData pd) 
    {
        if(!pd.HasData<FPSPlayerData>())
            pd.SetData(FPSPlayerData.CreateDefault());

        if(!pd.HasData<InRoundData>())
            pd.SetData(InRoundData.CreateDefault());
    }
}