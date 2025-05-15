using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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

    // ------------------ PLAYER STATE ------------------
    private bool isCrouching = false;
    private bool crouchAnimating = false;
    private bool isDashing = false;
    private bool isSliding = false;
    private bool wasAirborne = false;
    private bool jumpedFromGround;
    private bool isVaulting = false;
    private bool nearVaultable = false;
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
    private Coroutine zoomRoutine;

    // ------------------ CROUCH ------------------
    [Header("Crouch Settings")]
    public float crouchHeight = -0.8f;
    public float standHeight = 0f;
    public float crouchTime = 0.25f;
    private Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    private Vector3 standCenter = new Vector3(0, 0, 0);

    // ------------------ JUMP ------------------
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    private float fallStartY;
    private float minFallDistance = 1f;
    public event Action OnJumpPerformed; // event is invoked when jumping

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
    private Vector3 slideDirection;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 100, 100, 0);

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
    private Vector3 vaultTargetPosition;
    public float vaultBufferTime = 0.2f;
    private float lastJumpPressTime;

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public LayerMask whatIsLadder;

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

        if (wallFront && movementInput.y > 0.1f && wallLookAngle < maxWallLookAngle)
        {
            if (!climbing) climbing = true; // start climbing
            Vector3 climbDirection = (Vector3.up + orientation.forward * 0.2f).normalized; // forward nudge to "walk into" ladder
            //AudioManager.Instance.PlaySound(AudioManager.Instance.climbLadder);
            characterController.Move(climbDirection * climbSpeed * Time.deltaTime);
        }
        else if (climbing)
        {
            climbing = false;

            // start fall tracking when climbing ends
            wasAirborne = true;
            fallStartY = transform.position.y;
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
            targetLean -= movementInput.x * (maxLeanAngle / 6); // dampen
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
        bool grounded = characterController.isGrounded;

        if (grounded)
        {
            verticalVelocity = -1f;

            if (wantsToVault)
            {
                bool didVault = false;
                if (!isVaulting)
                    didVault = TryVault();

                if (!didVault && !isVaulting)
                {
                    verticalVelocity = jumpForce;
                    jumpedFromGround = true; // mark jump, but don't track fall yet
                }
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

        if (!grounded && !wasAirborne)
        {
            wasAirborne = true; // we just left the ground
            fallStartY = transform.position.y; // capture height
        }
        if (grounded && wasAirborne)
        {
            wasAirborne = false; // we just landed

            // check distance fallen
            float fallDistance = fallStartY - transform.position.y;
            if (fallDistance > minFallDistance || jumpedFromGround)
            {
                GetComponentInChildren<PlayerAudio>()?.PlayLandingSound();
            }

            jumpedFromGround = false;
        }
    }

    private void HandleCrouch()
    {
        if (crouchAnimating) return;
        if (crouchInput && sprintingInput && !isSliding && !isCrouching) { StartSlide(); return; }
        if (isCrouching != crouchInput) StartCoroutine(AnimateCameraCrouch(crouchInput));

        isCrouching = crouchInput;
    }

    private IEnumerator AnimateCameraCrouch(bool crouching)
    {
        crouchAnimating = true;
        float elapsed = 0f;
        float duration = crouchTime;

        Vector3 startPos = cameraAttachPoint.localPosition;
        float targetY = crouching ? defaultYPos + crouchHeight : defaultYPos;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);

        while (elapsed < duration)
        {
            cameraAttachPoint.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraAttachPoint.localPosition = targetPos;
        crouchAnimating = false;
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

        GetComponentInChildren<PlayerAudio>()?.PlaySlideSound();
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
        AudioManager.Instance.PlaySound(AudioManager.Instance.vault);

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

        // play dash sound
        GetComponentInChildren<PlayerAudio>()?.PlayDashSound();

        Vector3 dashDirection = moveDirection.normalized;
        if (dashDirection == Vector3.zero) // handle case where dash received but no movement direction
            dashDirection = transform.forward;

        float elapsed = 0f;
        //AudioManager.Instance.PlaySound(AudioManager.Instance.dash);
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
                {
                    lastJumpPressTime = Time.time;
                    OnJumpPerformed?.Invoke(); // notify listeners like PlayerAnimation
                }
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