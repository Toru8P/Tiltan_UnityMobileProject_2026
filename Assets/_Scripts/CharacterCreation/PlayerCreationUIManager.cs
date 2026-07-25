using System;
using _Scripts.MainGame;
using _Scripts.MainGame.SaveLoad;
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
        private const string OpenSaveSelectionKey = "open_save_selection_on_player_creation";

        public static void RequestSaveSelection()
        {
            PlayerPrefs.SetInt(OpenSaveSelectionKey, 1);
            PlayerPrefs.Save();
        }



        private void Start()
        {
            bool openSaveSelection = PlayerPrefs.GetInt(OpenSaveSelectionKey, 0) == 1;
            PlayerPrefs.DeleteKey(OpenSaveSelectionKey);
            PlayerPrefs.Save();

            if (openSaveSelection) ShowLoadGame();
            else ShowMain();
        }

        private void OnEnable()
        {
            // Actually perform the scene load when a slot's Load button fires the request.
            SaveSlotManager.OnLoadRequested += HandleLoadRequested;
        }

        private void OnDisable()
        {
            SaveSlotManager.OnLoadRequested -= HandleLoadRequested;
        }

        private void HandleLoadRequested(SaveSlotData data)
        {
            if (data == null) return;
            GameInitData.SetCustomization(new CharacterCustomization
            {
                PlayerName = string.IsNullOrWhiteSpace(data.characterName) ? "Hero" : data.characterName,
                SkinColorIndex = data.skinColorIndex,
                OutfitColorIndex = data.outfitColorIndex
            });
            // SaveLoadManager's own hook has already set the active slot; we just switch scenes.
            string scene = !string.IsNullOrEmpty(data.sceneName) ? data.sceneName : gameplaySceneName;
            SceneTransitionManager.LoadScene(scene);
        }

        public void ShowCharacterSelection()
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
            // Allocate a fresh slot so a new game never overwrites an existing save.
            int slot = SaveSlotManager.Instance != null ? SaveSlotManager.Instance.GetNextFreeSlot() : 0;
            if (slot < 0)
            {
                Debug.LogWarning($"Cannot create a new save: the maximum of {SaveSlotManager.MaxSaveSlots} save slots has been reached.");
                ShowLoadGame();
                return;
            }

            // Generate the world seed now and bake it into the new save so the world is reproducible.
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            SaveLoadManager.StartNewGame(slot, seed);

            // Create the slot's metadata up-front so it appears in the Load list and reserves the index.
            if (SaveSlotManager.Instance != null)
            {
                SaveSlotManager.Instance.SaveSlot(new SaveSlotData
                {
                    slotIndex = slot,
                    characterName = GameInitData.HasCustomization ? GameInitData.Customization.PlayerName : "New Character",
                    sceneName = gameplaySceneName,
                    skinColorIndex = GameInitData.HasCustomization ? GameInitData.Customization.SkinColorIndex : 0,
                    outfitColorIndex = GameInitData.HasCustomization ? GameInitData.Customization.OutfitColorIndex : 0,
                    saveDate = DateTime.UtcNow.ToString("o"),
                    playtimeSeconds = 0
                });
            }

            SceneTransitionManager.LoadScene(gameplaySceneName);
        }

        public void ReturnToSaveSelection()
        {
            SceneTransitionManager.LoadScene("PlayerCreation");
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
