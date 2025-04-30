using EMullen.PlayerMgmt;
using FishNet.Managing.Statistic;

class FPSPlayerData : PlayerDataClass 
{
    public string displayName;
    public int wins;
    
    // Constructor for JSON deserialization
    public FPSPlayerData() {}

    public FPSPlayerData(string displayName, int wins) 
    {
        this.displayName = displayName;
        this.wins = wins;
    }

    public static FPSPlayerData CreateDefault() => new("DefaultName", 0);
}