using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ported from a previous project, the PlayerMovement input listener class is a rudimentary first
///   person view character controller using WASD controls. The movement can be greatly improved.
/// </summary>
[RequireComponent(typeof(NetworkedAudioController))]
public class PlayerMovement : MonoBehaviour, IInputListener
{

    private NetworkedAudioController audioController;

    public Vector2 movementInput;
    public bool sprintingInput;
    public bool jumpInput;
    private bool lastJump;
    public bool JumpDown;

    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed of movement
    public float sprintSpeed = 9f;
    public float speedTransitionTime = 0.35f;
    
    [Header("Other")]
    public float stepLength = 0.5f;

    /// <summary>
    /// The actual speed of the player.
    /// </summary>
    public float speed { get; private set; }
    private float _targetSpeed;
    private float targetSpeed { 
        get => _targetSpeed;
        set { 
            targetSpeedChangeInitialSpeed = speed;
            targetSpeedChangeTime = Time.time;
            _targetSpeed = value;
        }
    }
    /// <summary>
    /// The initial speed before the target change
    /// </summary>
    private float targetSpeedChangeInitialSpeed;
    private float targetSpeedChangeTime;

    public float jumpPower = 40f;
    public float jumpDecay = 0.65f;
    public float jumpPowerRemaining { get; private set; }

    private CharacterController characterController;

    private Vector3 lastSteppedPosition = Vector3.zero;

    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("CharacterController is missing on this GameObject. Adding one.");
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    private void Update()
    {
        CharacterController cc = GetComponent<CharacterController>();

        // Check if the jump button was just pressed
        JumpDown = !lastJump && jumpInput && cc.isGrounded;
        if(JumpDown)
            jumpPowerRemaining = 13f;

        if(Time.time - targetSpeedChangeTime < speedTransitionTime) {
            speed = Mathf.Lerp(targetSpeedChangeInitialSpeed, targetSpeed, (Time.time-targetSpeedChangeTime)/speedTransitionTime);
        }

        // Handle movement
        // The target speed this tick
        float tickTargetSpeed = sprintingInput ? sprintSpeed : moveSpeed;
        if(tickTargetSpeed != targetSpeed)
            targetSpeed = tickTargetSpeed;

        Vector3 forward = movementInput.y * transform.forward * speed * Time.deltaTime;
        Vector3 horizontal = movementInput.x * (Quaternion.Euler(0, 90, 0)*transform.forward) * speed * Time.deltaTime;
        Vector3 vertical = (Physics.gravity + transform.up*jumpPowerRemaining) * Time.deltaTime;
        characterController.Move(forward+horizontal+vertical);
        
        // Reduce jump power over time
        if (jumpPowerRemaining > 0)
        {
            jumpPowerRemaining -= jumpDecay * Time.deltaTime;
            if (jumpPowerRemaining < 0)
                jumpPowerRemaining = 0;
        }

        lastJump = jumpInput;

        // Play step sound        
        // if(lastSteppedPosition == Vector3.zero) lastSteppedPosition = transform.position;
        // if(Vector3.Distance(lastSteppedPosition, transform.position) >= stepLength && cc.isGrounded) {
        //     lastSteppedPosition = transform.position;
        //     audioController.PlaySound($"walk{Random.Range(1, 7)}");
        // }
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        
    }

    public void InputPoll(InputAction action)
    {
        if(action.name == "Move") {
            movementInput = action.ReadValue<Vector2>(); // Direction of movement input
        } else if(action.name == "Sprint") {
            sprintingInput = action.ReadValue<float>() > 0.1f;
        } else if(action.name == "Jump") {
            jumpInput = action.ReadValue<float>() > 0.1f;
        } 
    }

}
