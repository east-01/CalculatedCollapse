using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(NetworkedAudioController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IInputListener
{
    // ------------------ COMPONENTS ------------------
    private NetworkedAudioController audioController;
    private CharacterController characterController;

    // ------------------ INPUT FLAGS ------------------
    public bool sprintingInput;  // accessed by player animation (running)
    public bool zoomInput;       // accessed by player animation (aiming)
    public bool crouchInput;     // accessed by player animation (crouching)
    private bool jumpInput;
    private bool dashInput;
    private bool leanLeftInput;
    private bool leanRightInput;
    private Vector2 movementInput;
    private readonly Dictionary<string, Action<InputAction>> inputHandlers = new();

    // ------------------ PLAYER STATE ------------------
    private bool isCrouching = false;
    private bool isDashing = false;
    private bool isSliding = false;
    private bool wasAirborne = false;
    private bool jumpedFromGround;
    private bool isVaulting = false;
    private bool climbing;

    // ------------------ MOVEMENT ------------------
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = 30f;
    private Vector3 moveDirection;
    private Vector2 currentInput;
    private float verticalVelocity;

    // ------------------ CAMERA ------------------
    [Header("Camera")]
    [SerializeField] private Transform cameraAttachPoint; // Camera bob + pitch
    [SerializeField] private Camera playerCamera;         // Actual camera (used for zoom/FOV)
    private float defaultFOV;
    private float defaultYPos;
    private float rotationX = 0f;
    private Vector3 defaultCamLocalPos;

    // ------------------ CAMERA SETTINGS ------------------
    [Header("Camera Look")]
    public float lookSpeedX = 2f;
    public float lookSpeedY = 2f;
    public float upperLookLimit = 80f;
    public float lowerLookLimit = 80f;

    [Header("Head Bob Settings")]
    public float walkBobSpeed = 10f;
    public float walkBobAmount = 0.05f;
    public float sprintBobSpeed = 14f;
    public float sprintBobAmount = 0.1f;
    public float crouchBobSpeed = 8f;
    public float crouchBobAmount = 0.025f;
    private float bobTimer;

    [Header("Zoom Settings")]
    public float zoomFOV = 40f;
    public float zoomTime = 0.1f;

    [Header("Landing Camera Dip")]
    public float landingDipAmount = 0.2f;
    public float landingDipSpeed = 10f;
    public float landingRecoverySpeed = 6f;

    private float landingDipOffset = 0f;

    // ------------------ CROUCH ------------------
    [Header("Crouch Settings")]
    public float crouchHeight = 1.2f;
    public float standHeight = 2.0f;
    public float crouchTime = 0.25f;
    private float desiredCameraY;
    public float crouchViewOffset = -0.8f; // update camera height
    public bool IsCrouching => isCrouching; // used by PlayerAnimation
    private Vector3 standCenter = new Vector3(0, 1f, 0);
    private Vector3 crouchCenter = new Vector3(0, 0.6f, 0);

    // ------------------ JUMP ------------------
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    private float fallStartY;
    private float minFallDistance = 1f;
    public event Action OnJumpPerformed; // event is invoked when jumping

    [Header("Jump Forgiveness")] // coyote time
    public float coyoteTime = 0.2f;
    private float lastGroundedTime;

    // ------------------ LEAN ------------------
    [Header("Player Lean Settings")]
    public float maxLeanAngle = 10f;
    public float leanSpeed = 5f;
    public float leanOffsetDistance = 1f;
    private float currentLean = 0f;

    // ------------------ DASH ------------------
    [Header("Dash Settings")]
    public int maxDashes = 3;
    public float dashForce = 30f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private int currentDashes;
    private float lastDashTime;

    // ------------------ SLIDING ------------------
    [Header("Sliding")]
    public float slideSpeed;
    public float slideDuration;
    private float slideTimer = 0f;
    private bool slideReleased = true;
    private Vector3 slideDirection;
    public bool IsSliding => isSliding; // accessed by playerAnimation
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 100, 100, 0);

    [Header("Slide Camera Pitch")]
    public float slidePitchAngle = 4f;
    public float slidePitchSpeed = 5f;
    private float currentSlidePitch = 0f;

    [Header("Slide FOV Boost")]
    public float slideFOV = 75f;
    public float slideFOVSpeed = 8f;
    private float currentFOV;

    // ------------------ CLIMBING ------------------
    [Header("Climbing")]
    public float climbSpeed;
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;
    private RaycastHit frontWallHit;
    private bool wallFront;

    // ------------------ VAULTING ------------------
    [Header("Vaulting")]
    public LayerMask vaultLayerMask;
    public float vaultCheckDistance = 1.5f;
    public float maxVaultHeight = 3f;
    public float minVaultHeight = 1f;
    public float vaultDuration = 0.3f;
    public float vaultBufferTime = 0.2f;
    private float lastJumpPressTime;

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public LayerMask whatIsLadder;

    private void Awake()
    {
        SetupInputHandlers();
        audioController = GetComponent<NetworkedAudioController>();
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (cameraAttachPoint == null)
            Debug.LogError("CameraAttachPoint must be assigned in the inspector!");

        defaultFOV = playerCamera.fieldOfView;
        defaultYPos = cameraAttachPoint.localPosition.y;
        defaultCamLocalPos = cameraAttachPoint.localPosition;

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

        HandleCameraLook();
        UpdateCameraHeight();
        HandleInput();    // taking inputs
        HandleMovement(); // executing movement
        HandleClimbing();
        UpdateFOV();
        HandleHeadBob();
        HandleSlide();

        float targetPitch = isSliding ? slidePitchAngle : 0f;
        currentSlidePitch = Mathf.Lerp(currentSlidePitch, targetPitch, Time.deltaTime * slidePitchSpeed);

        landingDipOffset = Mathf.Lerp(landingDipOffset, 0f, Time.deltaTime * landingRecoverySpeed);
    }

    private void HandleInput()
    {
        CalculateMovementDirection();  // WASD
        HandleJumpAndFall();    // jumping
        HandleDashTrigger();    // dashing
        HandleCrouch();         // crouching
    }

    // ------------------ MOVEMENT ------------------
    private void HandleMovement()
    {
        if (!climbing)
            moveDirection.y = verticalVelocity;
        else
            moveDirection.y = 0f; // climbing function will handle Y velocity

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void CalculateMovementDirection()
    {
        float targetSpeed = isCrouching ? crouchSpeed : (sprintingInput ? sprintSpeed : walkSpeed);
        currentInput = new Vector2(targetSpeed * movementInput.y, targetSpeed * movementInput.x);
        moveDirection = (transform.forward * currentInput.x) + (transform.right * currentInput.y);
    }

    // ------------------ CLIMBING ------------------
    private void HandleClimbing()
    {
        bool canClimb = DetectClimbableWall();

        if (canClimb && movementInput.y > 0.1f)
        {
            if (!climbing) StartClimbing();
            MoveWhileClimbing();
        }
        else if (climbing)
        {
            StopClimbing();
        }
    }

    private bool DetectClimbableWall()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsLadder); // ladder check
        if (!wallFront) return false;

        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);
        return wallLookAngle < maxWallLookAngle;
    }

    private void StartClimbing()
    {
        climbing = true;
    }

    private void StopClimbing()
    {
        climbing = false;

        // begin fall tracking after dismount
        wasAirborne = true;
        fallStartY = transform.position.y;
    }

    private void MoveWhileClimbing()
    {
        Vector3 climbDirection = (Vector3.up + orientation.forward * 0.2f).normalized;  // nudge forward
        characterController.Move(climbDirection * climbSpeed * Time.deltaTime);         // go up
    }

    // ------------------ CAMERA LOGIC ------------------
    private void HandleCameraLook()
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
            targetLean -= movementInput.x * (maxLeanAngle / 10); // dampen
            targetOffsetX = movementInput.x * leanOffsetDistance * 0.5f;
        }

        // smooth lean rotation
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // smooth camera position offset
        Vector3 targetPosition = defaultCamLocalPos + new Vector3(targetOffsetX, 0, 0);
        cameraAttachPoint.localPosition = Vector3.Lerp(cameraAttachPoint.localPosition, targetPosition, Time.deltaTime * leanSpeed);

        // apply tilt
        Quaternion targetRotation = Quaternion.Euler(rotationX + currentSlidePitch, 0, currentLean);
        cameraAttachPoint.localRotation = targetRotation;
    }

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;
        if (movementInput == Vector2.zero) return;

        bobTimer += Time.deltaTime * (isCrouching ? crouchBobSpeed : sprintingInput ? sprintBobSpeed : walkBobSpeed);
        float bobAmount = isCrouching ? crouchBobAmount : sprintingInput ? sprintBobAmount : walkBobAmount;
        float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;

        float baseY = defaultYPos + desiredCameraY;
        float finalY = baseY + bobOffsetY;

        if (cameraAttachPoint != null) cameraAttachPoint.localPosition = new Vector3( cameraAttachPoint.localPosition.x, finalY, cameraAttachPoint.localPosition.z);
    }

    private void UpdateFOV()
    {
        float targetFOV;

        if (isSliding)
            targetFOV = slideFOV;
        else if (zoomInput)
            targetFOV = zoomFOV;
        else
            targetFOV = defaultFOV;
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * slideFOVSpeed);
        playerCamera.fieldOfView = currentFOV;
    }

    private void UpdateCameraHeight()
    {
        if (cameraAttachPoint == null) return;

        Vector3 current = cameraAttachPoint.localPosition;
        float targetY = defaultYPos + desiredCameraY - landingDipOffset;
        float newY = Mathf.Lerp(current.y, targetY, Time.deltaTime * 10f); // smooth transition
        cameraAttachPoint.localPosition = new Vector3(current.x, newY, current.z);
    }

    // ------------------ JUMP & FALL ------------------
    private void HandleJumpAndFall()
    {
        bool grounded = characterController.isGrounded;

        if (grounded)
        {
            HandleGroundedJump();
            lastGroundedTime = Time.time;
        }
        else 
        {
            HandleAirborneMovement();
        }

        UpdateFallState(grounded);
    }

    private void HandleGroundedJump()
    {
        verticalVelocity = -1f;
        bool wantsToJump = jumpInput || (Time.time - lastJumpPressTime <= vaultBufferTime);
        bool coyoteJump = Time.time - lastGroundedTime <= coyoteTime;

        if (wantsToJump && coyoteJump)
        {
            if (!isVaulting && DetectVaultable(out Vector3 target))
                StartCoroutine(VaultRoutine(target));
            else if (!isVaulting)
            {
                verticalVelocity = jumpForce;
                jumpedFromGround = true;
            }
        }
    }

    private void HandleAirborneMovement()
    {
        if (!climbing)
            verticalVelocity -= gravity * Time.deltaTime;

        if (!isVaulting && DetectVaultable(out Vector3 target))
            StartCoroutine(VaultRoutine(target));
    }

    private void UpdateFallState(bool grounded)
    {
        if (!grounded && !wasAirborne)
        {
            wasAirborne = true;
            fallStartY = transform.position.y;
        }

        if (grounded && wasAirborne)
        {
            wasAirborne = false;
            float fallDistance = fallStartY - transform.position.y;

            if (fallDistance > minFallDistance || jumpedFromGround)
            {
                GetComponentInChildren<PlayerAudio>()?.PlayLandingSound();
                landingDipOffset = landingDipAmount;
            }

            jumpedFromGround = false;
        }
    }

    // ------------------ CROUCHING ------------------
    private void HandleCrouch()
    {
        if (!crouchInput || !sprintingInput) // reset slide trigger when inputs are released
            slideReleased = true;

        if (crouchInput && sprintingInput && !isSliding && !isCrouching && slideReleased) // trigger slide on fresh key press
        {
            StartSlide();
            slideReleased = false; // block re-slide
            return;
        }

        bool targetCrouch = crouchInput && !isSliding; // conditions to crouch
        if (targetCrouch != isCrouching)
        {
            isCrouching = targetCrouch;

            if (!isSliding) // dont override camera height during slide
                desiredCameraY = isCrouching ? crouchViewOffset : 0f;
        }
    }

    // ------------------ SLIDING ------------------
    private void StartSlide()
    {
        currentFOV = slideFOV;

        isSliding = true;
        slideTimer = 0f;
        slideDirection = transform.forward;

        isCrouching = true;
        desiredCameraY = crouchViewOffset; // keep camera low while sliding


        StartCoroutine(AdjustCrouch(crouchHeight, crouchCenter)); // still needed for collider

        GetComponentInChildren<PlayerAudio>()?.PlaySlideSound(); // sound
    }

    private void HandleSlide()
    {
        if (!isSliding) return;

        slideTimer += Time.deltaTime;
        float normalizedTime = slideTimer / slideDuration;
        float curveFactor = slideCurve.Evaluate(normalizedTime);

        Vector3 slideVelocity = slideDirection * slideSpeed * curveFactor;
        characterController.Move(slideVelocity * Time.deltaTime);

        if (slideTimer >= slideDuration || slideVelocity.magnitude < 0.1f)
        {
            EndSlide();
        }
    }

    private void EndSlide()
    {
        isSliding = false;
        isCrouching = true;
        desiredCameraY = 0f;

        StartCoroutine(AdjustCrouch(standHeight, standCenter));
    }

    // ------------------ VAULTING ------------------
    private bool DetectVaultable(out Vector3 targetPosition)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;
        targetPosition = Vector3.zero;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, vaultCheckDistance, vaultLayerMask))
        {
            float obstacleTopY = hit.collider.bounds.max.y;
            float playerFeetY = transform.position.y;
            float relativeHeight = obstacleTopY - playerFeetY;

            if (relativeHeight >= minVaultHeight && relativeHeight <= maxVaultHeight)
            {
                targetPosition = hit.point + transform.forward * 1.2f + Vector3.up * 0.5f;
                return true;
            }
        }

        return false;
    }

    // ------------------ DASH ------------------
    private void HandleDashTrigger()
    {
        if (!dashInput || isDashing || Time.time < lastDashTime + dashCooldown || currentDashes <= 0) return;
        
        StartCoroutine(DashRoutine());
    }

    private Vector3 GetDashDirection()
    {
        Vector3 dir = moveDirection.normalized;
        return dir == Vector3.zero ? transform.forward : dir; // handle case where dash input received but no movement input
    }

    public void RefillDash(int amount) // TODO: powerups across the map will call this function
    {
        currentDashes = Mathf.Clamp(currentDashes + amount, 0, maxDashes);
    }

    // ------------------ COROUTINES ------------------
    private IEnumerator VaultRoutine(Vector3 targetPosition) // handle the actual vault
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

    private IEnumerator AdjustCrouch(float targetHeight, Vector3 targetCenter)
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f)) yield break;

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
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        currentDashes--;
        lastDashTime = Time.time;

        GetComponentInChildren<PlayerAudio>()?.PlayDashSound();  // play dash sound

        Vector3 dashDirection = GetDashDirection();

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            characterController.Move(dashDirection * dashForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    // ------------------ INPUT BINDINGS ------------------
    private void SetupInputHandlers() // setup mappings from input action names to handlers
    {
        inputHandlers["Move"] = action => movementInput = action.ReadValue<Vector2>();
        inputHandlers["Sprint"] = action => sprintingInput = action.ReadValue<float>() > 0.1f;
        inputHandlers["Jump"] = action =>
        {
            jumpInput = action.ReadValue<float>() > 0.1f;
            if (jumpInput)
            {
                lastJumpPressTime = Time.time;
                OnJumpPerformed?.Invoke();
            }
        };
        inputHandlers["Crouch"] = action => crouchInput = action.ReadValue<float>() > 0.1f;
        inputHandlers["Zoom"] = action => zoomInput = action.ReadValue<float>() > 0.1f;
        inputHandlers["Dash"] = action => dashInput = action.ReadValue<float>() > 0.1f;
        inputHandlers["Lean Left"] = action => leanLeftInput = action.ReadValue<float>() > 0.1f;
        inputHandlers["Lean Right"] = action => leanRightInput = action.ReadValue<float>() > 0.1f;
    }

    public void InputEvent(InputAction.CallbackContext context) { }

    public void InputPoll(InputAction action)
    {
        if (inputHandlers.TryGetValue(action.name, out var handler))
            handler(action);
    }
}