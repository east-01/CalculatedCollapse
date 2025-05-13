using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource sfxSource;

    [Header("Player")]
    public AudioClip walk;
    public AudioClip run;
    public AudioClip dash;
    public AudioClip slide;
    public AudioClip vault;
    public AudioClip climbLadder;
    public AudioClip sprint;
    public AudioClip death;

    [Header("Combat")]
    public AudioClip shoot;
    public AudioClip reload;
    public AudioClip grenadeThrow;
    public AudioClip grenadeExplosion;
    public AudioClip bulletHit;
    public AudioClip bulletImpact;

    [Header("Extras")]
    public AudioClip wallBreak;
    public AudioClip menuSelect;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
