using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Mixer Settings")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (_bgmSource == null) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.outputAudioMixerGroup = _bgmGroup;
        _bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (_sfxSource == null) return;
        _sfxSource.outputAudioMixerGroup = _sfxGroup;
        _sfxSource.PlayOneShot(clip);
    }

    public void SetBGMVolume(float volume)
    {
        if (_audioMixer != null)
        {
            // Assuming volume is 0 to 1, map to -80 to 20 dB
            float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            _audioMixer.SetFloat("BGMVolume", dB);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (_audioMixer != null)
        {
            float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            _audioMixer.SetFloat("SFXVolume", dB);
        }
    }
}
