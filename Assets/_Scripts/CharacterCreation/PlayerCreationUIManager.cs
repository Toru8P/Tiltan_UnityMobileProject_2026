using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.CharacterCreation
{
    // Panel switcher for the PlayerCreation scene.
    // Main panel = customization; secondary panels = Load Game and New Game confirm.
    public class PlayerCreationUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loadGamePanel;
        [SerializeField] private GameObject newGameConfirmPanel;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "TerrainTest";

        private void Start()
        {
            ShowMain();
        }

        public void ShowMain()
        {
            SetPanels(main: true, loadGame: false, confirm: false);
        }

        public void ShowLoadGame()
        {
            SetPanels(main: false, loadGame: true, confirm: false);
        }

        public void ShowNewGameConfirm()
        {
            SetPanels(main: false, loadGame: false, confirm: true);
        }

        public void ConfirmNewGame()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void CancelNewGame()
        {
            ShowMain();
        }

        private void SetPanels(bool main, bool loadGame, bool confirm)
        {
            if (mainPanel) mainPanel.SetActive(main);
            if (loadGamePanel) loadGamePanel.SetActive(loadGame);
            if (newGameConfirmPanel) newGameConfirmPanel.SetActive(confirm);
        }
    }
}
