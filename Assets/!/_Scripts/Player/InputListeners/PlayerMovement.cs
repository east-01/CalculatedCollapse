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

    [Header("Camera")]
    [SerializeField] private Transform cameraAttachPoint; // Camera bob + pitch
    [SerializeField] private Camera playerCamera; // Actual camera (used for zoom/FOV)
    private float defaultFOV;

    // Input values
    private Vector2 movementInput;
    private bool sprintingInput;
    private bool jumpInput;
    private bool crouchInput;
    private bool zoomInput;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = 30f;

    [Header("Crouch Settings")]
    public float crouchHeight = 0.5f;
    public float standHeight = 2f;
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
    public float zoomFOV = 30f;
    public float zoomTime = 0.3f;
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
    }

    private void Update()
    {
        if (!Application.isFocused) return;

        HandleLook();
        HandleMovementInput();

        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
            if (!lastJump && jumpInput)
            {
                verticalVelocity = jumpForce;
            }

            if (crouchInput && !crouchAnimating)
            {
                StartCoroutine(CrouchStand());
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveDirection.y = verticalVelocity;
        characterController.Move(moveDirection * Time.deltaTime);

        HandleZoom();
        HandleHeadBob();

        lastJump = jumpInput;
    }

    private void HandleMovementInput()
    {
        float targetSpeed = isCrouching ? crouchSpeed : (sprintingInput ? sprintSpeed : walkSpeed);
        currentInput = new Vector2(targetSpeed * movementInput.y, targetSpeed * movementInput.x);
        moveDirection = (transform.forward * currentInput.x) + (transform.right * currentInput.y);
    }

    private void HandleLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationX -= mouseDelta.y * lookSpeedY * Time.deltaTime;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        if (cameraAttachPoint != null)
            cameraAttachPoint.localRotation = Quaternion.Euler(rotationX, 0, 0);

        transform.Rotate(Vector3.up * mouseDelta.x * lookSpeedX * Time.deltaTime);
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

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
            yield break;

        crouchAnimating = true;

        float elapsed = 0f;
        float startHeight = characterController.height;
        float targetHeight = isCrouching ? standHeight : crouchHeight;
        Vector3 startCenter = characterController.center;
        Vector3 targetCenter = isCrouching ? standCenter : crouchCenter;

        while (elapsed < crouchTime)
        {
            characterController.height = Mathf.Lerp(startHeight, targetHeight, elapsed / crouchTime);
            characterController.center = Vector3.Lerp(startCenter, targetCenter, elapsed / crouchTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;
        isCrouching = !isCrouching;
        crouchAnimating = false;
    }

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;
        if (movementInput == Vector2.zero) return;

        bobTimer += Time.deltaTime * (isCrouching ? crouchBobSpeed : sprintingInput ? sprintBobSpeed : walkBobSpeed);
        float bobAmount = isCrouching ? crouchBobAmount : sprintingInput ? sprintBobAmount : walkBobAmount;

        cameraAttachPoint.localPosition = new Vector3(
            cameraAttachPoint.localPosition.x,
            defaultYPos + Mathf.Sin(bobTimer) * bobAmount,
            cameraAttachPoint.localPosition.z
        );
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
        }
    }
}