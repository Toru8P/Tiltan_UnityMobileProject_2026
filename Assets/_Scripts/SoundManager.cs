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

    // Singleton setup: only one SoundManager should exist.
    // If a second one is created (e.g., scene reload), destroy it.
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Generic sound effect player. Slightly randomizes pitch each play so repeated sounds feel less robotic.
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip, volume);
    }

    // Convenience wrappers — anywhere in the game can just call SoundManager.Instance.PlayFootstep() etc.
    public void PlayFootstep()
    {
        PlaySFX(grassFootstep, 0.8f);
    }

    // Plays the player's attack swing sound.
    public void PlayAttack()
    {
        PlaySFX(attackSound, 1f);
    }

    // Plays the "got hit" sound.
    public void PlayHit()
    {
        PlaySFX(hitSound, 1f);
    }

    // Plays the death sound.
    public void PlayDeath()
    {
        PlaySFX(deathSound, 1f);
    }
}
