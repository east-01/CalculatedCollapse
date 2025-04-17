using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(NetworkedAudioController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IInputListener
{
    private NetworkedAudioController audioController;
    private CharacterController characterController;

    // Input values
    private Vector2 movementInput;
    private bool sprintingInput;
    private bool jumpInput;
    private bool crouchInput;
    private bool zoomInput;
    private bool dashInput;

    [Header("Camera")]
    [SerializeField] private Transform cameraAttachPoint; // Camera bob + pitch
    [SerializeField] private Camera playerCamera; // Actual camera (used for zoom/FOV)
    private float defaultFOV;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = 30f;

    [Header("Crouch Settings")]
    public float crouchHeight = -0.8f;
    public float standHeight = 0f;
    public float crouchTime = 0.25f;
    private bool isCrouching = false;
    private bool crouchAnimating = false;
    private Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    private Vector3 standCenter = new Vector3(0, 0, 0);

    [Header("Jump Settings")]
    public float jumpForce = 8f;
    private float verticalVelocity;
    private bool lastJump;

    [Header("Zoom Settings")]
    public float zoomFOV = 40f;
    public float zoomTime = 0.1f;
    private Coroutine zoomRoutine;

    [Header("Camera Look")]
    public float lookSpeedX = 2f;
    public float lookSpeedY = 2f;
    public float upperLookLimit = 80f;
    public float lowerLookLimit = 80f;
    private float rotationX = 0f;

    [Header("Head Bob Settings")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 18f;
    public float sprintBobAmount = 0.1f;
    public float crouchBobSpeed = 8f;
    public float crouchBobAmount = 0.025f;
    private float defaultYPos;
    private float bobTimer;

    [Header("Camera Lean Settings")]
    public float maxLeanAngle = 5f;     // max lean angle
    public float leanSpeed = 5f;        // how fast camera tilts
    private float currentLean = 0f;     // current Z rotation value

    [Header("Dash Settings")]
    public int maxDashes = 3;           // replenished with power ups - TODO: tune later
    public float dashForce = 30f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;     // cooldown to prevent spam
    private int currentDashes;
    private bool isDashing = false;
    private float lastDashTime;

    /* climbing system by Dave / GameDevelopment on YT */
    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public LayerMask whatIsLadder;

    [Header("Climbing")]
    public float climbSpeed;
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;
    private RaycastHit frontWallHit;
    private bool wallFront;
    private bool climbing;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (cameraAttachPoint == null)
            Debug.LogError("CameraAttachPoint must be assigned in the inspector!");

        defaultFOV = playerCamera.fieldOfView;
        defaultYPos = cameraAttachPoint.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDashes = maxDashes; // set initial dash count
    }

    private void Update()
    {
        if (!Application.isFocused) return;

        HandleLook();
        HandleInput();    // taking inputs
        HandleMovement(); // executing movement
        HandleClimbing();
        HandleZoom();
        HandleHeadBob();

        lastJump = jumpInput;
    }

    private void HandleInput()
    {
        HandleMovementInput();      // WASD
        HandleGravityAndJumping();  // jumping
        HandleDashTrigger();        // dashing
        HandleCrouch();             // crouching
    }

    private void HandleMovement()
    {
        if (!climbing)
            moveDirection.y = verticalVelocity;
        else
            moveDirection.y = 0f; // climbing function will handle Y velocity

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleClimbing()
    {
        // ladder check
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsLadder);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal); // player-to-wall angle within certain bounds to allow climbing

        if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle)
        {
            if (!climbing) climbing = true; // start climbing
            Vector3 climbDirection = (Vector3.up + orientation.forward * 0.2f).normalized; // forward nudge to "walk into" ladder
            characterController.Move(climbDirection * climbSpeed * Time.deltaTime);
        }
        else if (climbing)
        {
            climbing = false;
        }
    }

    private void HandleLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationX -= mouseDelta.y * lookSpeedY * Time.deltaTime;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        transform.Rotate(Vector3.up * mouseDelta.x * lookSpeedX * Time.deltaTime);

        HandleCameraLean(); // apply X (pitch) + Z (lean)
    }

    // tilts the camera based on horizontal input, only when shift is held (X + Z rotation)
    private void HandleCameraLean()
    {
        float targetLean = 0f;

        if (sprintingInput && Mathf.Abs(movementInput.x) > 0.1f)
            targetLean -= movementInput.x * maxLeanAngle;

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        if (cameraAttachPoint != null)
        {
            Quaternion targetRotation = Quaternion.Euler(rotationX, 0, currentLean);
            cameraAttachPoint.localRotation = targetRotation;
        }
    }

    private void HandleMovementInput()
    {
        float targetSpeed = isCrouching ? crouchSpeed : (sprintingInput ? sprintSpeed : walkSpeed);
        currentInput = new Vector2(targetSpeed * movementInput.y, targetSpeed * movementInput.x);
        moveDirection = (transform.forward * currentInput.x) + (transform.right * currentInput.y);
    }

    private void HandleGravityAndJumping()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
            if (!lastJump && jumpInput)
                verticalVelocity = jumpForce;
        }
        else // possibly climbing or jumping
        {
            if (!climbing)
                verticalVelocity -= gravity * Time.deltaTime;
        }
    }

    private void HandleCrouch()
    {
        if (crouchAnimating) return;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        Vector3 targetCenter = isCrouching ? crouchCenter : standCenter;

        if (characterController.height != targetHeight)
            StartCoroutine(AdjustCrouch(targetHeight, targetCenter));

        isCrouching = crouchInput; // continuously reflects current input (hold-to-crouch)
    }

    // animate crouching animation
    private IEnumerator AdjustCrouch(float targetHeight, Vector3 targetCenter)
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
            yield break;

        crouchAnimating = true;

        float elapsed = 0f;
        float startHeight = characterController.height;
        Vector3 startCenter = characterController.center;

        while (elapsed < crouchTime)
        {
            characterController.height = Mathf.Lerp(startHeight, targetHeight, elapsed / crouchTime);
            characterController.center = Vector3.Lerp(startCenter, targetCenter, elapsed / crouchTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;
        crouchAnimating = false;
    }

    private void HandleDashTrigger()
    {
        if (dashInput && !isDashing && Time.time >= lastDashTime + dashCooldown && currentDashes > 0)
            StartCoroutine(DashRoutine());
    }

    private void HandleZoom()
    {
        if (zoomInput && zoomRoutine == null)
            zoomRoutine = StartCoroutine(ToggleZoom(true));
        else if (!zoomInput && zoomRoutine == null)
            zoomRoutine = StartCoroutine(ToggleZoom(false));
    }

    private IEnumerator ToggleZoom(bool zoomingIn)
    {
        float targetFOV = zoomingIn ? zoomFOV : defaultFOV;
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < zoomTime)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / zoomTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;
        if (movementInput == Vector2.zero) return;

        bobTimer += Time.deltaTime * (isCrouching ? crouchBobSpeed : sprintingInput ? sprintBobSpeed : walkBobSpeed);
        float bobAmount = isCrouching ? crouchBobAmount : sprintingInput ? sprintBobAmount : walkBobAmount;

        if (cameraAttachPoint != null)
            cameraAttachPoint.localPosition = new Vector3(
                cameraAttachPoint.localPosition.x,
                defaultYPos + Mathf.Sin(bobTimer) * bobAmount,
                cameraAttachPoint.localPosition.z
            );
    }

    // TODO: powerups across the map will call this function
    public void RefillDash(int amount)
    {
        currentDashes = Mathf.Clamp(currentDashes + amount, 0, maxDashes);
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        currentDashes--;
        lastDashTime = Time.time;

        Vector3 dashDirection = moveDirection.normalized;
        if (dashDirection == Vector3.zero) // handle case where dash received but no movement direction
            dashDirection = transform.forward;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            characterController.Move(dashDirection * dashForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    public void InputEvent(InputAction.CallbackContext context) { }

    public void InputPoll(InputAction action)
    {
        switch (action.name)
        {
            case "Move":
                movementInput = action.ReadValue<Vector2>();
                break;
            case "Sprint":
                sprintingInput = action.ReadValue<float>() > 0.1f;
                break;
            case "Jump":
                jumpInput = action.ReadValue<float>() > 0.1f;
                break;
            case "Crouch":
                crouchInput = action.ReadValue<float>() > 0.1f;
                break;
            case "Zoom":
                zoomInput = action.ReadValue<float>() > 0.1f;
                break;
            case "Dash":
                dashInput = action.ReadValue<float>() > 0.1f;
                break;
        }
    }
}