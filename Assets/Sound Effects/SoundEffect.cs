public AudioSource audioSource;

public AudioClip jumpSound;
public AudioClip diveSound;

//Example player behavior each frame
void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Jump();
    }

    if (Input.GetMouseButtonDown(0))
    {
        Dive();
    }
}

void Jump()
{
    // Jump mechanics
    audioSource.PlayOneShot(jumpSound);
}
void Dive()
{
    // Dive mechanics
    audioSource.PlayOneShot(diveSound);
}