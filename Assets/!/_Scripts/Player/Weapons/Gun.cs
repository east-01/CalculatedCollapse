using UnityEngine;
using System.Collections;

public class Gun : Weapon
{
    public float damage = 10f;
    public float range = 100f;
    public float impactForce = 30f;

    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    //Audio Manager
    //private AudioManager audioManager;

    void Start()
    {
        Uses = MaxUses;
    }

    void Update()
    {
        if (Uses <= 0)
        {
            Reload();
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + UseRate;
            Shoot();
        }
    }

    void Reload()
    {
        //AudioManager.Instance.PlaySound(AudioManager.Instance.reload);
        if (!isReloading)
            StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log("Cooldown...");
        yield return new WaitForSeconds(1.3f);
        Uses = MaxUses;
        isReloading = false;
    }

    void Shoot()
    {
        //AudioManager.Instance.PlaySound(AudioManager.Instance.shoot);
        Uses--;

        if (muzzleFlash != null)
            muzzleFlash.Play();

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log(hit.transform.name);

            if (hit.transform.CompareTag("Player"))
            {
                InRoundData playerData = hit.transform.GetComponent<InRoundData>();
                if (playerData != null)
                {
                    playerData.TakeDamage(damage);
                }
            }

            if (impactEffect != null)
            {
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 1f);
            }
        }
    }
}
