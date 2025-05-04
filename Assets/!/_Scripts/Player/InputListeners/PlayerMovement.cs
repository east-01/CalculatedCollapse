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
    public bool sprintingInput;  // accessed by player animation (running)
    public bool zoomInput;       // accessed by player animation (aiming)
    public bool crouchInput;     // accessed by player animation (crouching)
    private bool jumpInput;      // 
    private Vector2 movementInput;
    private bool dashInput;
    private bool leanLeftInput;
    private bool leanRightInput;

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
    public float jumpForce = 10f;
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

    [Header("Player Lean Settings")]
    public float maxLeanAngle = 10f;
    public float leanSpeed = 5f;
    public float leanOffsetDistance = 1f; // how far the camera shifts when leaning
    private Vector3 defaultCamLocalPos;
    private float currentLean = 0f;

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

    [Header("Sliding")]
    public float slideSpeed;
    public float slideDuration;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 slideDirection;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 100, 100, 0);

    [Header("Vaulting")]
    public LayerMask vaultLayerMask;
    public float vaultCheckDistance = 1.5f;
    public float maxVaultHeight = 3f;
    public float minVaultHeight = 1f;
    public float vaultDuration = 0.3f;
    private bool isVaulting = false;
    private bool nearVaultable = false;
    private Vector3 vaultTargetPosition;
    // vaulting buffer for midair vaults and early jump input
    public float vaultBufferTime = 0.2f;
    private float lastJumpPressTime;

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
        defaultCamLocalPos = cameraAttachPoint.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDashes = maxDashes; // set initial dash count
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;        
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
        HandleSlide();
        CheckForVaultable();
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

    // tilts and/or move the camera based on horizontal input: Q+E or lateral movement when shift is held
    private void HandleCameraLean()
    {
        // when no input is polled, do nothing
        float targetLean = 0f;
        float targetOffsetX = 0f;

        // first, check for manual lean
        if (leanLeftInput)
        {
            targetLean = maxLeanAngle;           // lean
            targetOffsetX = -leanOffsetDistance; // shift
        }
        else if (leanRightInput)
        {
            targetLean = -maxLeanAngle;         // lean
            targetOffsetX = leanOffsetDistance; // shift
        }
        // otherwise, apply auto lean when sprinting and moving to the side
        else if (sprintingInput && Mathf.Abs(movementInput.x) > 0.1f)
        {
            targetLean -= movementInput.x * (maxLeanAngle / 4); // dampen
            targetOffsetX = movementInput.x * leanOffsetDistance * 0.5f;
        }

        // smooth lean rotation
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // smooth camera position offset
        Vector3 targetPosition = defaultCamLocalPos + new Vector3(targetOffsetX, 0, 0);
        cameraAttachPoint.localPosition = Vector3.Lerp(cameraAttachPoint.localPosition, targetPosition, Time.deltaTime * leanSpeed);

        // apply tilt
        Quaternion targetRotation = Quaternion.Euler(rotationX, 0, currentLean);
        cameraAttachPoint.localRotation = targetRotation;
    }

    private void HandleMovementInput()
    {
        float targetSpeed = isCrouching ? crouchSpeed : (sprintingInput ? sprintSpeed : walkSpeed);
        currentInput = new Vector2(targetSpeed * movementInput.y, targetSpeed * movementInput.x);
        moveDirection = (transform.forward * currentInput.x) + (transform.right * currentInput.y);
    }

    private void HandleGravityAndJumping()
    {
        bool wantsToVault = jumpInput || (Time.time - lastJumpPressTime <= vaultBufferTime);

        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
            if (wantsToVault)
            {
                bool didVault = false;
                if (!isVaulting)
                    didVault = TryVault();

                if (!didVault && !isVaulting)
                    verticalVelocity = jumpForce;
            }
        }
        else // in air
        {
            if (!climbing)
                verticalVelocity -= gravity * Time.deltaTime;

            // mid-air vault
            if (wantsToVault && !isVaulting)
                TryVault();
        }
    }

    private void HandleCrouch()
    {
        if (crouchAnimating) return;
        
        // Check for a slide
        if (crouchInput && sprintingInput && !isSliding && !isCrouching)
        {
            StartSlide();
            return;
        }


        float targetHeight = isCrouching ? crouchHeight : standHeight;
        Vector3 targetCenter = isCrouching ? crouchCenter : standCenter;

        if (characterController.height != targetHeight)
            StartCoroutine(AdjustCrouch(targetHeight, targetCenter));

        isCrouching = crouchInput; // continuously reflects current input (hold-to-crouch)
    }

    // Method to handle the sliding
    private void HandleSlide()
    {
        if (!isSliding)
            return;

        slideTimer += Time.deltaTime;
        float factor = slideCurve.Evaluate(slideTimer / slideDuration);

        Vector3 slideVelocity = slideDirection * slideSpeed * factor;
        characterController.Move(slideVelocity * Time.deltaTime);

        if (slideTimer >= slideDuration || slideVelocity.magnitude < 0.1f)
        {
            isSliding = false;
        }
    }
    
    private void StartSlide()
    {
        isSliding = true;
        slideTimer = 0f;
        slideDirection = transform.forward;

        isCrouching = true;
        StartCoroutine(AdjustCrouch(crouchHeight, crouchCenter));
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

    private void CheckForVaultable()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        RaycastHit hit;
        nearVaultable = false;

        if (Physics.Raycast(origin, direction, out hit, vaultCheckDistance, vaultLayerMask))
        {
            float obstacleTopY = hit.collider.bounds.max.y;
            float playerFeetY = transform.position.y;
            float relativeHeight = obstacleTopY - playerFeetY;

            if (relativeHeight >= minVaultHeight && relativeHeight <= maxVaultHeight)
            {
                nearVaultable = true;
                vaultTargetPosition = hit.point + transform.forward * 1.2f + Vector3.up * 0.5f;
            }
        }
    }

    private bool TryVault()
    {
        if (isVaulting || !nearVaultable)
            return false;

        // perform vault
        StartCoroutine(PerformVault(vaultTargetPosition));
        return true;
    }

    // handle the actual vault
    private IEnumerator PerformVault(Vector3 targetPosition)
    {
        isVaulting = true;
        characterController.enabled = false;

        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < vaultDuration)
        {
            transform.position = Vector3.Lerp(start, targetPosition, elapsed / vaultDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        characterController.enabled = true;
        isVaulting = false;
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

                if (jumpInput)
                    lastJumpPressTime = Time.time;
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
            case "Lean Left":
                leanLeftInput = action.ReadValue<float>() > 0.1f;
                break;
            case "Lean Right":
                leanRightInput = action.ReadValue<float>() > 0.1f;
                break;
        }
    }
}