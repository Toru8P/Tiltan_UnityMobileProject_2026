using _Scripts.MainGame.Difficulty;
using TMPro;
using UnityEngine;

namespace _Scripts.MainGame.UI
{
    public class SurvivalHUDController : MonoBehaviour
    {
        public static SurvivalHUDController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GeneralDifficultyManager difficultyManager;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI difficultyText;

        [Header("Settings")]
        [SerializeField] private float scoreBaseMultiplier = 10f;

        private float _survivalTime;
        private double _score;
        private double _bonusScore;
        private int _lastDisplayedScore = -1;
        private DifficultyPhase _currentPhase = DifficultyPhase.None;

        private float _updateTimer;
        private const float UpdateInterval = 0.1f;

        public float SurvivalTime => _survivalTime;
        public double Score => _score;
        public DifficultyPhase CurrentPhase => _currentPhase;

        private bool _isStopped = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _survivalTime = 0f;
            _score = 0;
            _bonusScore = 0;
            _lastDisplayedScore = -1;
            _isStopped = false;

            if (difficultyManager != null)
                difficultyManager.SubscribeOnChange(OnPhaseChanged);

            UpdateHUD();
        }

        public void AddScore(double amount)
        {
            if (_isStopped) return;
            _bonusScore += amount;
            UpdateHUD();
        }

        public void Stop()
        {
            _isStopped = true;
        }

        private void OnDestroy()
        {
            if (difficultyManager != null)
                difficultyManager.UnsubscribeOnChange(OnPhaseChanged);
        }

        private void Update()
        {
            if (_isStopped) return;

            _survivalTime += Time.deltaTime;

            _updateTimer += Time.deltaTime;
            if (_updateTimer < UpdateInterval) return;
            _updateTimer = 0f;

            _score = (_survivalTime * scoreBaseMultiplier * GetPhaseMultiplier()) + _bonusScore;
            UpdateHUD();
        }

        private void OnPhaseChanged(DifficultyPhase phase)
        {
            _currentPhase = phase;

            if (difficultyText == null) return;
            difficultyText.text = $"Difficulty: {phase}";
            difficultyText.color = GetPhaseColor(phase);
        }

        private void UpdateHUD()
        {
            if (timeText != null)
                timeText.text = TimeFormatter.FormatTime(_survivalTime);

            if (scoreText != null)
            {
                string formattedScore = ScoreFormatter.FormatScore(_score);
                scoreText.text = $"Score: {formattedScore}";
            }
        }

        private float GetPhaseMultiplier()
        {
            return _currentPhase switch
            {
                DifficultyPhase.Easy => 1f,
                DifficultyPhase.Normal => 1.5f,
                DifficultyPhase.Hard => 2f,
                DifficultyPhase.Expert => 3f,
                _ => 1f
            };
        }

        private Color GetPhaseColor(DifficultyPhase phase)
        {
            return phase switch
            {
                DifficultyPhase.Easy => Color.green,
                DifficultyPhase.Normal => Color.yellow,
                DifficultyPhase.Hard => new Color(1f, 0.5f, 0f),
                DifficultyPhase.Expert => Color.red,
                _ => Color.white
            };
        }
    }
}
