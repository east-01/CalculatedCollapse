using UnityEngine;
using UnityEngine.InputSystem;
using EMullen.Core;
using EMullen.PlayerMgmt;

/// <summary>
/// Handles raycast-based shooting logic for a player. Uses PlayerInputManager's polling system
/// to determine if the fire input is active. Applies damage to IDamageable targets.
/// </summary>
public class GunRaycast : MonoBehaviour, IInputListener
{
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.2f;

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

    private void Awake() 
    {
        player = GetComponent<Player>();

        if (fpCamera == null)
            Debug.LogWarning("GunRaycast: Missing fpCamera reference.");
    }

    private void Update() 
    {
        if (!Application.isFocused) return;

        if (!isFiring || fpCamera == null || player == null)
            return;

        if (Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Polls the Fire input using PlayerInputManager.
    /// </summary>
    /// <param name="action">InputAction from Input System</param>
    public void InputPoll(InputAction action) 
    {
        if (action.name == "Fire")
            isFiring = action.ReadValue<float>() > 0.1f;
    }

    /// <summary>
    /// Required by IInputListener, but unused here.
    /// </summary>
    /// <param name="context">Callback context from Input System</param>
    public void InputEvent(InputAction.CallbackContext context) { }

    /// <summary>
    /// Shoots a ray from the camera's position forward and applies damage if it hits an IDamageable.
    /// </summary>
    private void Shoot() 
    {
        Debug.Log("GunRaycast: Shoot() called");

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
}
