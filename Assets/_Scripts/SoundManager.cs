using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip grassFootstep;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Generic SFX player
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip, volume);
    }

    // Specific methods you can call anywhere
    public void PlayFootstep()
    {
        PlaySFX(grassFootstep, 0.8f);
    }

    public void PlayAttack()
    {
        PlaySFX(attackSound, 1f);
    }

    public void PlayHit()
    {
        PlaySFX(hitSound, 1f);
    }

    public void PlayDeath()
    {
        PlaySFX(deathSound, 1f);
    }
}
