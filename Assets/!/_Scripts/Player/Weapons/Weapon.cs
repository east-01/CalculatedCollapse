using UnityEngine;
using FishNet.Object;

/// <summary>
/// Base Weapon class providing shared functionality and values for all weapon types.
/// Handles use limits, reload timing, and networked damage.
/// </summary>
public class Weapon : MonoBehaviour
{
    // Max number of uses before reload
    public int MaxUses = 10;

    // Time it takes to reload the weapon
    public float ReloadTime = 1f;

    // Minimum delay between consecutive uses
    public float UseRate = 1f / 15f;

    // Sprite used for weapon display in UI
    public Sprite uiImage;

    // Remaining uses, settable only within class or subclass
    public int Uses { get; protected set; }

    // Sends damage to a networked target if it implements IDamageable
    protected void Cmd_DealDamage(NetworkObject target, float damage)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}
