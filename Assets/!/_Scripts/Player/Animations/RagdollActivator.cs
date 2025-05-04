using UnityEngine;

public class RagdollActivator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody[] ragdollBodies;

    void Awake()
    {
        animator = GetComponent<Animator>();
        ragdollBodies = GetComponentsInChildren<Rigidbody>();

        // disable ragdoll at start
        SetRagdollState(false);
    }

    public void ActivateRagdoll()
    {
        animator.enabled = false;
        SetRagdollState(true);
    }

    void SetRagdollState(bool enabled)
    {
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = !enabled;
            if (rb.GetComponent<Collider>())
                rb.GetComponent<Collider>().enabled = enabled;
        }
    }
}