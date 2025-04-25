using EMullen.PlayerMgmt;

public class InRoundData : PlayerDataClass
{
    public float health;
    public int wins;
    public string gun; // TODO: Replace with enum/scriptable object reference

    public InRoundData() {}

    public InRoundData(float health, int wins, string gun) 
    {
        this.health = health;
        this.wins = wins;
        this.gun = gun;
    }
}