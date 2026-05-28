using System;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.Difficulty
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [SerializeField] private DifficultyProgression progression;
        [SerializeField] private bool autoProgress = true;

        [Header("Events")]
        public UnityEvent<DifficultySettings> OnDifficultyChanged;

        [Header("Runtime Info (Debug)")]
        [SerializeField] private float _currentTime;
        [SerializeField] private DifficultySettings _currentSettings;

        public float CurrentTime => _currentTime;
        public DifficultySettings CurrentSettings => _currentSettings;

        // Singleton setup. Also pre-applies the starting difficulty in Awake (not Start) so other scripts
        // can read it from their own Start() methods without race conditions.
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Move initial setup to Awake so it's ready for Start() of other components
                if (progression != null)
                {
                    UpdateDifficulty(0f);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Ticks the survival timer up every frame and re-checks which difficulty should be active.
        // Can be disabled via `autoProgress = false` for manual control.
        private void Update()
        {
            if (autoProgress)
            {
                _currentTime += Time.deltaTime;
                UpdateDifficulty(_currentTime);
            }
        }

        // Asks the progression asset which DifficultySettings matches the current time.
        // If it's a NEW setting (different from last frame), broadcasts the OnDifficultyChanged event
        // so every listener (spawner, zombies, HUD) can react.
        private void UpdateDifficulty(float time)
        {
            if (progression == null) return;

            DifficultySettings newSettings = progression.GetSettingsForTime(time);
            if (newSettings != _currentSettings)
            {
                _currentSettings = newSettings;
                OnDifficultyChanged?.Invoke(_currentSettings);
                Debug.Log($"Difficulty changed to: {_currentSettings.levelName}");
            }
        }

        // Reads the current difficulty's intensity curve at the current time.
        // Lets spawn rate ramp up smoothly *within* a single difficulty level, not just between them.
        public float GetCurrentIntensity()
        {
            if (_currentSettings == null) return 1f;
            return _currentSettings.spawnIntensityCurve.Evaluate(_currentTime);
        }

        // Resets the timer back to zero — useful when the player restarts a round.
        public void ResetTimer()
        {
            _currentTime = 0f;
            UpdateDifficulty(0f);
        }
    }
}
