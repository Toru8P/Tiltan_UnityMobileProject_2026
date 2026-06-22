using UnityEngine;
using UnityEngine.Audio;

namespace _Scripts.MainGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Mixer Settings")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _bgmGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        public void PlayBGM(AudioClip clip)
        {
            if (!_bgmSource) return;
            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            _bgmSource.outputAudioMixerGroup = _bgmGroup;
            _bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (!_sfxSource) return;
            _sfxSource.outputAudioMixerGroup = _sfxGroup;
            _sfxSource.PlayOneShot(clip);
        }

        public void SetBGMVolume(float volume)
        {
            if (_audioMixer)
            {
                // Assuming volume is 0 to 1, map to -80 to 20 dB
                float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _audioMixer.SetFloat("BGMVolume", dB);
            }
        }

        public void SetSFXVolume(float volume)
        {
            if (_audioMixer)
            {
                float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _audioMixer.SetFloat("SFXVolume", dB);
            }
        }
    }
}
