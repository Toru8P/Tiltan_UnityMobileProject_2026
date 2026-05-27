using UnityEngine;
using TMPro;

namespace _Scripts.UI
{
    public class SurvivalHUDController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI difficultyText;

        [Header("Settings")]
        [SerializeField] private float scoreMultiplier = 10f;

        private float _survivalTime;
        private int _score;
        private int _lastDisplayedScore = -1;

        private void Start()
        {
            _survivalTime = 0f;
            _score = 0;
            _lastDisplayedScore = -1;
            UpdateHUD();
        }

        private void Update()
        {
            _survivalTime += Time.deltaTime;
            _score = Mathf.FloorToInt(_survivalTime * scoreMultiplier);
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

            if (difficultyText != null)
            {
                string difficulty = GetDifficulty(_survivalTime);
                difficultyText.text = $"Difficulty: {difficulty}";
                difficultyText.color = GetDifficultyColor(difficulty);
            }
        }

        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        private string GetDifficulty(float time)
        {
            if (time < 30f) return "Easy";
            if (time < 60f) return "Medium";
            if (time < 120f) return "Hard";
            if (time < 240f) return "Extreme";
            if (time < 480f) return "Hell";
            if (time < 960f) return "Nightmare";
            return "Death Guaranteed";
        }

        private Color GetDifficultyColor(string difficulty)
        {
            switch (difficulty)
            {
                case "Easy": return Color.green;
                case "Medium": return Color.yellow;
                case "Hard": return new Color(1f, 0.5f, 0f); // Orange
                case "Extreme": return Color.red;
                case "Hell": return new Color(0.5f, 0f, 0f); // Dark Red
                case "Nightmare": return Color.magenta;
                case "Death Guaranteed": return Color.black;
                default: return Color.white;
            }
        }
    }
}
