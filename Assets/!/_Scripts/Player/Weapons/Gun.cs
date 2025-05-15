using UnityEngine;
using System.Collections;
using FishNet.Object;

/// <summary>
/// Gun is a weapon that uses raycasting to deal damage and plays effects and audio.
/// It handles reloading, firing, recoil, and hit detection.
/// </summary>
public class Gun : Weapon
{
    // Gun stats and visual/audio effects
    public float damage = 10f;
    public float range = 100f;
    public float impactForce = 30f;

    public string shootSoundID = "shoot_rifle";
    public string reloadSoundID = "reload_rifle";

    public Camera fpsCam;
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;

    // Recoil values for pitch and yaw
    public float recoilX = 2f;
    public float recoilY = 1f;

    // Internal control variables
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    private NetworkedAudioController audioController;

    private void Start()
    {
        // Set initial ammo and get reference to audio controller
        Uses = MaxUses;
        audioController = GetComponentInParent<NetworkedAudioController>();
    }

    private void Update()
    {
        // If out of ammo, begin reload
        if (Uses <= 0)
        {
            Reload();
            return;
        }

        // Handle shooting input and fire rate cooldown
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + UseRate;
            Shoot();
        }
    }

    // Begins reloading if not already reloading
    private void Reload()
    {
        if (!isReloading)
            StartCoroutine(ReloadCoroutine());
    }

    // Plays reload audio and waits before restoring ammo
    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        audioController?.PlaySound(reloadSoundID);

        yield return new WaitForSeconds(ReloadTime);

        Uses = MaxUses;
        isReloading = false;
    }

    // Handles shooting logic, effects, and damage raycast
    private void Shoot()
    {
        Uses--;
        audioController?.PlaySound(shootSoundID);
        muzzleFlash?.Play();
        ApplyRecoil();

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range))
        {
            Debug.Log(hit.transform.name);

            NetworkObject targetNetObj = hit.transform.GetComponent<NetworkObject>();
            IDamageable damageable = hit.transform.GetComponent<IDamageable>();

            if (damageable != null && targetNetObj != null)
                Cmd_DealDamage(targetNetObj, damage);

            if (impactEffect != null)
            {
                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactGO, 1f);
            }
        }
    }

    // Adds random upward and sideways recoil to the camera
    private void ApplyRecoil()
    {
        float recoilPitch = Random.Range(recoilX * 0.8f, recoilX * 1.2f);
        float recoilYaw = Random.Range(-recoilY, recoilY);

        fpsCam.transform.localEulerAngles += new Vector3(-recoilPitch, recoilYaw, 0f);
    }
}
