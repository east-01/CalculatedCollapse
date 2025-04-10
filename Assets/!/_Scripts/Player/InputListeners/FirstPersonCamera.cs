using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The FirstPersonCamera class is an input listener monobehaviour that controls a camera attached
///   to the player with the standard first person camera controls.
/// Uses input polling.
/// </summary>
public class FirstPersonCamera : MonoBehaviour, IInputListener
{
    // Public input variable for mouse movement (set from another script or the Inspector)
    public Vector2 input;
    public Transform playerTransform;

    // Sensitivity for camera movement
    public float sensitivity = 5.0f;

    // Rotation limits for the vertical axis
    public float verticalRotationLimit = 90.0f;

    // Internal state to track vertical rotation
    private float verticalRotation = 0f;

    private bool locked = false;

    private void Start() 
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            locked = !locked;
            Debug.Log("Locked: " + locked);
        }

        if(locked)
            return;

        // TODO: Remove, temp input- true input in InputPoll
        // input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // Calculate the rotation amounts based on input and sensitivity
        float horizontalRotation = input.x * sensitivity;
        float verticalDelta = input.y * sensitivity;

        // Apply vertical rotation and clamp it within the rotation limits
        verticalRotation -= verticalDelta;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);

        // Apply vertical rotation to the camera (child)
        transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);

        // Apply horizontal rotation to the parent
        playerTransform.Rotate(0f, horizontalRotation, 0f);
    }

    public void InputEvent(InputAction.CallbackContext context)
    {

    }

    public void InputPoll(InputAction action)
    {
        if(action.name == "Look") {
            input = action.ReadValue<Vector2>();
        }
    }
}
