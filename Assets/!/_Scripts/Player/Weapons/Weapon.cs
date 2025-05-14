using UnityEngine;
using FishNet.Object;

public class Weapon : NetworkBehaviour
{
    public int MaxUses = 10;
    public float ReloadTime = 1f;
    public float UseRate = 1f / 15f;

    public Sprite uiImage;

    public int Uses { get; protected set; }

    [ServerRpc]
    protected void Cmd_DealDamage(NetworkObject target, float damage)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}
