using UnityEngine;

public class Grenade : Weapon
{
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public GameObject explosionEffect;

    float countdown;
    bool hasExploded = false;

    //Audio Manager
    private AudioManager audioManager;

    void Start()
    {
        countdown = UseRate;
    }

    void Update()
    {
        countdown -= Time.deltaTime;
        if (countdown <= 0f && !hasExploded)
        {
            Explode();
            hasExploded = true;
        }
    }

    void Explode()
    {
        //Play explosion sound
        //AudioManager.Instance.PlaySound(AudioManager.Instance.grenadeExplosion);
        
        // Show visual effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        // Detect nearby objects with rigidbodies
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            Target target = nearbyObject.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(50f); 
            }
        }

        // Destroy grenade object
        Destroy(gameObject);
    }
}
