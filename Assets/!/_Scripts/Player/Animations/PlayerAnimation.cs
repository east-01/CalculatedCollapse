using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Vector3 lastPosition;

    [SerializeField] private PlayerMovement playerMovement;

    [Header("Head Look Settings")]
    public Transform headBone; // robot's head bone
    public Transform cameraAttachPoint;
    public float headRotationSpeed = 5f;
    public float maxHeadTurnAngle = 70f;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // movement direction
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - lastPosition;
        delta.y = 0; // ignore vertical movement

        Vector3 localDelta = transform.InverseTransformDirection(delta); // global -> local movement (relative to player)
        float moveX = localDelta.x / Time.deltaTime;
        float moveZ = localDelta.z / Time.deltaTime;
        float moveSpeed = new Vector3(localDelta.x, 0, localDelta.z).magnitude / Time.deltaTime;

        // animator controller
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool("Jump", true);
            StartCoroutine(ResetJumpFlag());
        }
        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveZ", moveZ);
        animator.SetBool("IsRunning", playerMovement.sprintingInput);
        animator.SetBool("IsAiming", playerMovement.zoomInput);
        animator.SetBool("IsCrouching", playerMovement.crouchInput);

        // death animations (only the beginning of the animation)
        animator.SetBool("IsDead", true);
        animator.SetFloat("deathType", Random.Range(0,0)); // TODO: or based on cause of death

        // then we switch to ragdoll physics (after the initial fall - this is better for realism)

        lastPosition = currentPosition;
    }

    IEnumerator ResetJumpFlag()
    {
        yield return null; // 1 frame later
        animator.SetBool("Jump", false);
    }
}