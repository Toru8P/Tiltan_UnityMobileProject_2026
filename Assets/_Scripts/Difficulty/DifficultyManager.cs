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

        private void Update()
        {
            if (autoProgress)
            {
                _currentTime += Time.deltaTime;
                UpdateDifficulty(_currentTime);
            }
        }

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

        public float GetCurrentIntensity()
        {
            if (_currentSettings == null) return 1f;
            return _currentSettings.spawnIntensityCurve.Evaluate(_currentTime);
        }

        public void ResetTimer()
        {
            _currentTime = 0f;
            UpdateDifficulty(0f);
        }
    }
}
