using EMullen.PlayerMgmt;

public class InRoundData : PlayerDataClass
{
    public float health;
    public int wins;
    public string gun; // TODO: Replace with enum/scriptable object reference
    public bool ready;

    public InRoundData() {}

    public InRoundData(float health, int wins, string gun) 
    {
        this.health = health;
        this.wins = wins;
        this.gun = gun;
        this.ready = false;
    }

    public static InRoundData CreateDefault() => new(1, 0, "default");

    public bool IsAlive => health > 0f;

    public void TakeDamage(float amount)
    {
        health = UnityEngine.Mathf.Clamp01(health - amount);
    }

    public void ResetHealth()
    {
        health = 1f;
    }
}
