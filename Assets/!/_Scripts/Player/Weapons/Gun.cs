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
    public bool IsReloading { get; private set; } = false;

    private NetworkedAudioController audioController;
    private Player player;
    private PlayerHUDMenuController hud;

    private void Start()
    {
        // Set initial ammo and get reference to audio controller
        Uses = MaxUses;
        audioController = GetComponentInParent<NetworkedAudioController>();
        player = GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
        hud = player.GetComponentInChildren<PlayerHUDMenuController>();
    }

    private void Update()
    {
        // If out of ammo, begin reload
        if (Uses <= 0)
        {
            Reload();
            return;
        }

        if (Input.GetKey(KeyCode.Mouse0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + UseRate;
            Shoot();
        }
    }

    // Begins reloading if not already reloading
    private void Reload()
    {
        if (!IsReloading)
            StartCoroutine(ReloadCoroutine());
    }

    // Plays reload audio and waits before restoring ammo
    private IEnumerator ReloadCoroutine()
    {
        IsReloading = true;
        Debug.Log("Reloading...");

        audioController.PlaySound(reloadSoundID);

        yield return new WaitForSeconds(ReloadTime);

        Uses = MaxUses;
        IsReloading = false;
    }

    // Handles shooting logic, effects, and damage raycast
    private void Shoot()
    {
        Uses--;
        audioController.PlaySound(shootSoundID);
        muzzleFlash.Play();
        ApplyRecoil();

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range))
        {
            Debug.Log(hit.transform.name);

            NetworkObject targetNetObj = hit.transform.GetComponent<NetworkObject>();
            IDamageable damageable = hit.transform.GetComponent<IDamageable>();

            if (damageable != null && targetNetObj != null)
            {
                audioController.PlaySound("hitmarker", 1, false);
                hud.Crosshair.ShowHitmarker();
                Cmd_DealDamage(targetNetObj, damage);

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
