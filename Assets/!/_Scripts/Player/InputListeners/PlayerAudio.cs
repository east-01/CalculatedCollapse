using UnityEngine;

/// <summary>
/// Handles audio like footsteps based on animation events.
/// </summary>

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private NetworkedAudioController audioController;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float walkCooldown = 0.4f;
    [SerializeField] private float runCooldown = 0.3f;

    private float lastFootstepTime = -1f;

    private void Awake()
    {
        // if not assigned manually, try to find it in parent
        if (audioController == null)
        {
            audioController = GetComponentInParent<NetworkedAudioController>();
            if (audioController == null)
                Debug.LogError("PlayerAudio: NetworkedAudioController not found!");
        }

        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
            if (characterController == null)
                Debug.LogError("PlayerAudio: CharacterController not found!");
        }
    }

    // called from animation event
    public void OnRunFootstep()
    {
        PlayFootstep(runCooldown);
    }

    // called from animation event
    public void OnWalkFootstep()
    {
        PlayFootstep(walkCooldown);
    }

    [SerializeField] private string[] footstepIDs = { "walk1", "walk2", "walk3" };
    private void PlayFootstep(float cooldown)
    {
        if (characterController == null || audioController == null)
            return;

        if (!characterController.isGrounded)
            return;
        if (Time.time - lastFootstepTime < cooldown)
            return;

        lastFootstepTime = Time.time;

        string chosenID = footstepIDs[Random.Range(0, footstepIDs.Length)];
        audioController.PlaySound(chosenID);
    }

    [SerializeField] private string[] landingIDs = { "land1", "land2", "land3" };
    public void PlayLandingSound()
    {
        if (audioController == null)
            return;

        if (landingIDs == null || landingIDs.Length == 0)
            return;

        string chosenID = landingIDs[Random.Range(0, landingIDs.Length)];
        audioController.PlaySound(chosenID, 0.4f);
    }

    [SerializeField] private string dashSoundID = "dash";
    public void PlayDashSound()
    {
        if (audioController != null)
            audioController.PlaySound(dashSoundID, 0.4f);
    }

    [SerializeField] private string slideSoundID = "slide";
    public void PlaySlideSound()
    {
        if (audioController != null)
            audioController.PlaySound(slideSoundID, 0.4f);
    }
}