using UnityEngine;
using _Scripts.Difficulty;

namespace _Scripts.Managers
{
    // Persists player preferences (volumes, starting difficulty) across sessions via PlayerPrefs.
    // Acts as the single source of truth — UI reads from here, not from AudioManager directly.
    public class OptionsManager : MonoBehaviour
    {
        public static OptionsManager Instance { get; private set; }

        // PlayerPrefs keys
        private const string KEY_BGM = "opt_bgm_volume";
        private const string KEY_SFX = "opt_sfx_volume";
        private const string KEY_DIFFICULTY = "opt_base_difficulty";

        [Header("Difficulty")]
        [Tooltip("The DifficultyProgression SO used in the scene. Options menu selects which threshold index to start from.")]
        [SerializeField] private DifficultyProgression difficultyProgression;

        // Current values — kept in memory so the UI can read them without hitting PlayerPrefs every frame
        public float BGMVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public int BaseDifficultyIndex { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
            ApplyAll();
        }

        // ── Public setters called by OptionsMenuUI ────────────────────────────

        public void SetBGMVolume(float value)
        {
            BGMVolume = Mathf.Clamp01(value);
            AudioManager.Instance.SetBGMVolume(BGMVolume);
            PlayerPrefs.SetFloat(KEY_BGM, BGMVolume);
        }

        public void SetSFXVolume(float value)
        {
            SFXVolume = Mathf.Clamp01(value);
            AudioManager.Instance.SetSFXVolume(SFXVolume);
            PlayerPrefs.SetFloat(KEY_SFX, SFXVolume);
        }

        // Index corresponds to the threshold index in DifficultyProgression.
        // 0 = Easy (first threshold), 1 = Normal, 2 = Hard, etc.
        public void SetBaseDifficulty(int index)
        {
            if (difficultyProgression == null || difficultyProgression.thresholds == null) return;
            BaseDifficultyIndex = Mathf.Clamp(index, 0, difficultyProgression.thresholds.Length - 1);
            PlayerPrefs.SetInt(KEY_DIFFICULTY, BaseDifficultyIndex);

            // Jump the DifficultyManager to the chosen threshold's start time
            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.SetBaseTime(difficultyProgression.thresholds[BaseDifficultyIndex].timeThreshold);
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void LoadAll()
        {
            BGMVolume = PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX, 1.0f);
            BaseDifficultyIndex = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);
        }

        private void ApplyAll()
        {
            AudioManager.Instance.SetBGMVolume(BGMVolume);
            AudioManager.Instance.SetSFXVolume(SFXVolume);
            // Difficulty is applied when the game scene loads and DifficultyManager is available
        }

        // Called by DifficultyManager after it initialises — applies the saved starting time.
        public void ApplyDifficultyToManager()
        {
            if (DifficultyManager.Instance == null || difficultyProgression == null) return;
            if (difficultyProgression.thresholds == null || difficultyProgression.thresholds.Length == 0) return;
            int safeIndex = Mathf.Clamp(BaseDifficultyIndex, 0, difficultyProgression.thresholds.Length - 1);
            DifficultyManager.Instance.SetBaseTime(difficultyProgression.thresholds[safeIndex].timeThreshold);
        }
    }
}
