using UnityEngine;

public class Gun : Weapon
{
    public float damage = 10f;
    public float range = 100f;
    public float impactForce = 30f;

    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    private float nextTimeToFire = 0f;

    void Start()
    {
        Uses = MaxUses;
    }

    void Update()
    {
        // If out of ammo, reload and return early
        if (Uses <= 0)
        {
            Reload();
            return;
        }

        // Handle shooting if Fire1 is pressed and fireRate delay passed
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + UseRate;
            Shoot();
        }
    }

    void Reload()
    {
        Debug.Log("Reloading...");
        Uses = MaxUses;
    }

    void Shoot()
    {
        Uses--;

        if (muzzleFlash != null)
            muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }

            if (impactEffect != null)
            {
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 1f);
            }
        }
    }
}
