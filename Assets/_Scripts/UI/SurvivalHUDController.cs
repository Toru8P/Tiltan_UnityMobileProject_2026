using UnityEngine;
using TMPro;
using _Scripts.Difficulty;

namespace _Scripts.UI
{
    public class SurvivalHUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI difficultyText;

        [Header("Settings")]
        [SerializeField] private float scoreBaseMultiplier = 10f;

        private float _survivalTime;
        private int _score;
        private int _lastDisplayedScore = -1;

        // Resets the HUD state and subscribes to the difficulty change event so the difficulty label
        // updates whenever the difficulty changes (instead of polling for it).
        private void Start()
        {
            _survivalTime = 0f;
            _score = 0;
            _lastDisplayedScore = -1;

            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.AddListener(UpdateDifficultyUI);
                if (DifficultyManager.Instance.CurrentSettings != null)
                {
                    UpdateDifficultyUI(DifficultyManager.Instance.CurrentSettings);
                }
            }
            
            UpdateHUD();
        }

        // Unsubscribe from the event when the HUD is destroyed — keeps the event clean and avoids null callbacks.
        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(UpdateDifficultyUI);
            }
        }

        private float _updateTimer = 0f;
        private float _updateInterval = 0.1f;

        // Throttled update — runs the HUD refresh only 10x per second (not every frame).
        // UI text doesn't need to update at 60Hz, and skipping work saves performance.
        // Reads the survival time from the DifficultyManager and computes score = time * baseMultiplier * difficultyMultiplier.
        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer < _updateInterval) return;
            _updateTimer = 0f;

            if (DifficultyManager.Instance != null)
            {
                _survivalTime = DifficultyManager.Instance.CurrentTime;
                
                float multiplier = scoreBaseMultiplier;
                if (DifficultyManager.Instance.CurrentSettings != null)
                {
                    multiplier *= DifficultyManager.Instance.CurrentSettings.scoreMultiplier;
                }
                
                _score = Mathf.FloorToInt(_survivalTime * multiplier);
            }
            
            UpdateHUD();
        }

        // Writes the timer and score to the UI text fields.
        // Score is rounded to the nearest 100 to stop the digits from flickering every frame,
        // and we only update the text string when the rounded value actually changed (small allocation win).
        private void UpdateHUD()
        {
            if (timeText != null)
                timeText.text = FormatTime(_survivalTime);

            if (scoreText != null)
            {
                int displayedScore = (_score / 100) * 100;
                if (displayedScore != _lastDisplayedScore)
                {
                    scoreText.text = $"Score: {displayedScore}";
                    _lastDisplayedScore = displayedScore;
                }
            }
        }

        // Event handler — fires when DifficultyManager broadcasts a difficulty change.
        // Updates the on-screen difficulty label name and color to match the new tier (e.g., red for "Hard").
        private void UpdateDifficultyUI(DifficultySettings settings)
        {
            if (difficultyText != null && settings != null)
            {
                difficultyText.text = $"Difficulty: {settings.levelName}";
                difficultyText.color = settings.levelColor;
            }
        }

        // Converts a raw second count into a "MM:SS" formatted string for the timer display.
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
