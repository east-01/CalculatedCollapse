using UnityEngine;
using UnityEngine.InputSystem;
using EMullen.PlayerMgmt;

public class GunRaycast : MonoBehaviour
{
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.2f;

    [Header("References")]
    public ParticleSystem muzzleFlash;
    public FirstPersonCamera fpCamera;
    public Player player;
    private PlayerInput input; // Get input via PlayerInput component

    private float nextTimeToFire = 0f;

    void Start()
    {
        // Try to get input component from player at runtime
        if (player != null)
            input = player.GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (player == null || fpCamera == null || input == null)
            return;

        // Check if the player owns this input (local), and if "Fire" is pressed
        if (input.actions["Fire"].IsPressed() && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        muzzleFlash?.Play();

        Vector3 rayOrigin = fpCamera.transform.position;
        Vector3 rayDirection = fpCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range))
        {
            Debug.Log("Hit object: " + hit.collider.name);

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}
