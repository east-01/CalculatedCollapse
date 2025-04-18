using UnityEngine;
using UnityEngine.InputSystem;
using EMullen.Core;
using EMullen.PlayerMgmt;

/// <summary>
/// Handles raycast-based shooting logic for a player. Uses PlayerInputManager's polling system
/// to determine if the fire or reload input is active. Applies damage to IDamageable targets.
/// </summary>
[RequireComponent(typeof(Player))]
public class GunRaycast : MonoBehaviour, IInputListener
{
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.2f;
    public int maxAmmo = 10;

    [Header("References")]
    public FirstPersonCamera fpCamera;

    /// <summary>
    /// Whether the player is holding the fire input.
    /// </summary>
    private bool isFiring = false;

    /// <summary>
    /// The next time the player can fire based on fire rate.
    /// </summary>
    private float nextTimeToFire = 0f;

    /// <summary>
    /// Reference to the player component on the object.
    /// </summary>
    private Player player;

    /// <summary>
    /// Current number of bullets in the magazine.
    /// </summary>
    private int currentAmmo;

    private void Awake() 
    {
        player = GetComponent<Player>();
        currentAmmo = maxAmmo;

        if (fpCamera == null)
            Debug.LogWarning("GunRaycast: Missing fpCamera reference.");
    }

    private void Update() 
    {
        if (!Application.isFocused) return;

        if (!isFiring || fpCamera == null || player == null || currentAmmo <= 0)
            return;

        if (Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Polls the Fire and Reload input using PlayerInputManager.
    /// </summary>
    /// <param name="action">InputAction from Input System</param>
    public void InputPoll(InputAction action) 
    {
        switch (action.name)
        {
            case "Fire":
                isFiring = action.ReadValue<float>() > 0.1f;
                break;
            case "Reload":
                if (action.triggered)
                    Reload();
                break;
        }
    }

    /// <summary>
    /// Required by IInputListener, but unused here.
    /// </summary>
    /// <param name="context">Callback context from Input System</param>
    public void InputEvent(InputAction.CallbackContext context) { }

    /// <summary>
    /// Shoots a ray from the camera's position forward and applies damage if it hits an IDamageable.
    /// Also reduces ammo count.
    /// </summary>
    private void Shoot() 
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("GunRaycast: Out of ammo!");
            return;
        }

        Debug.Log("GunRaycast: Shoot() called");
        currentAmmo--;
        Debug.Log($"GunRaycast: Ammo left = {currentAmmo}");

        Vector3 rayOrigin = fpCamera.transform.position;
        Vector3 rayDirection = fpCamera.transform.forward;

        Debug.DrawRay(rayOrigin, rayDirection * range, Color.red, 1f);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, range)) 
        {
            Debug.Log("GunRaycast: Hit " + hit.collider.name);

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null) 
            {
                damageable.TakeDamage(damage);
                Debug.Log($"GunRaycast: Applied {damage} damage to {hit.collider.name}");
            }
            else 
            {
                Debug.Log("GunRaycast: Hit object is not damageable.");
            }
        }
        else 
        {
            Debug.Log("GunRaycast: Raycast did not hit anything.");
        }
    }

    /// <summary>
    /// Reloads the gun to full magazine capacity.
    /// </summary>
    private void Reload()
    {
        currentAmmo = maxAmmo;
        Debug.Log("GunRaycast: Reloaded. Ammo = " + currentAmmo);
        Debug.Log("🔁 Reload pressed");
    }
}
