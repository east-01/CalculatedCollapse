using UnityEngine;
using UnityEngine.InputSystem;
using EMullen.Core;
using EMullen.PlayerMgmt;

[RequireComponent(typeof(Player))]
public class GunRaycast : MonoBehaviour, IInputListener
{
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 25f;
    public float fireRate = 0.2f;

    [Header("References")]
    public FirstPersonCamera fpCamera;

    private bool isFiring = false;
    private float nextTimeToFire = 0f;
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

    public void InputEvent(InputAction.CallbackContext context)
    {
        // Not used, required for interface
    }

    public void InputPoll(InputAction action)
    {
        if (action.name == "Fire")
        {
            isFiring = action.ReadValue<float>() > 0.1f;
        }
    }

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
