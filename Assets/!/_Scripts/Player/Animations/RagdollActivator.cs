using UnityEngine;

public class RagdollActivator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody[] ragdollBodies;

    void Awake()
    {
        animator = GetComponent<Animator>();
        ragdollBodies = GetComponentsInChildren<Rigidbody>(true); // includeInactive: true

        SetRagdollState(false); // disable ragdoll at start
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
            if (rb != null)
            {
                rb.gameObject.SetActive(enabled); // activate/deactivate child
                rb.isKinematic = !enabled;

                var col = rb.GetComponent<Collider>();
                if (col) col.enabled = enabled;
            }
        }
    }
}