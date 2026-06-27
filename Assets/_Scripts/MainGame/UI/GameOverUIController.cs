using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.MainGame.UI
{
    public class GameOverUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject contentPanel;
        [SerializeField] private TextMeshProUGUI scoreValueText;
        [SerializeField] private TextMeshProUGUI timeValueText;
        [SerializeField] private TextMeshProUGUI difficultyValueText;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (contentPanel != null)
                contentPanel.SetActive(false);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
        }

        public void Show(double score, string time, string difficulty)
        {
            if (contentPanel != null)
                contentPanel.SetActive(true);

            if (scoreValueText != null)
                scoreValueText.text = ((int)score).ToString(); // Still display as int

            if (timeValueText != null)
                timeValueText.text = time;

            if (difficultyValueText != null)
                difficultyValueText.text = difficulty;
        }

        private void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}