using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private RagdollActivator ragdollActivator;
    private Vector3 lastPosition;

    [SerializeField] private PlayerMovement playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        ragdollActivator = GetComponent<RagdollActivator>();
        lastPosition = transform.position;

        //PlayDeath(); // TEMP test
    }

    void Update()
    {
        HandleMovementAnimation();
    }

    void HandleMovementAnimation()
    {
        // movement direction animation (walking/running/crouching left, right, forward, back)
        Vector3 currentPosition = transform.position;
        Vector3 delta = transform.InverseTransformDirection(currentPosition - lastPosition); // global -> local movement (relative to player)
        delta.y = 0; // ignore vertical movement

        float moveX = delta.x / Time.deltaTime;
        float moveZ = delta.z / Time.deltaTime;

        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveZ", moveZ);
        animator.SetBool("IsRunning", playerMovement.sprintingInput);
        animator.SetBool("IsAiming", playerMovement.zoomInput);
        animator.SetBool("IsCrouching", playerMovement.crouchInput);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool("Jump", true);
            StartCoroutine(ResetJumpFlag());
        }

        lastPosition = transform.position;
    }

    IEnumerator ResetJumpFlag()
    {
        yield return null;
        animator.SetBool("Jump", false);
    }

    // death logic
    public void PlayDeath()
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Standing_React_Death_Backward")) return;

        animator.SetTrigger("IsDead");
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        float clipLength = animator.runtimeAnimatorController.animationClips
            .First(c => c.name == "Standing_React_Death_Backward").length;

        yield return new WaitForSeconds(clipLength);
        ragdollActivator.ActivateRagdoll();
    }
}