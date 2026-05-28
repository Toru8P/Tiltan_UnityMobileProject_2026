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

        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(UpdateDifficultyUI);
            }
        }

        private float _updateTimer = 0f;
        private float _updateInterval = 0.1f;

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

        private void UpdateDifficultyUI(DifficultySettings settings)
        {
            if (difficultyText != null && settings != null)
            {
                difficultyText.text = $"Difficulty: {settings.levelName}";
                difficultyText.color = settings.levelColor;
            }
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
