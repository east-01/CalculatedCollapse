using UnityEngine;

public class TestDummy : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. Remaining health: {health}");

        if (health <= 0)
        {
            Debug.Log($"{gameObject.name} died.");
            Destroy(gameObject); // Optional: destroy the target
        }
    }
}
