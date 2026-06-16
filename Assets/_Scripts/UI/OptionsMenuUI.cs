using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Scripts.Managers;

namespace _Scripts.UI
{
    // Attach to the Options Menu panel GameObject.
    //
    // Inspector wiring checklist:
    //   bgmSlider        → the BGM volume Slider
    //   sfxSlider        → the SFX volume Slider
    //   difficultyButtons → array of buttons in order: Easy, Normal, Hard (must match DifficultyProgression.thresholds order)
    //   optionsPanel     → this panel's root GameObject (so Show/Hide work)
    //   activeButtonColor   → highlight color for the selected difficulty button
    //   inactiveButtonColor → default color for unselected buttons
    public class OptionsMenuUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button closeButton;

        [Header("Audio Sliders")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Difficulty Buttons (Easy → Hard, must match DifficultyProgression order)")]
        [SerializeField] private Button[] difficultyButtons;

        [Header("Button Colors")]
        [SerializeField] private Color activeButtonColor = new Color(0.29f, 0.87f, 0.50f);
        [SerializeField] private Color inactiveButtonColor = new Color(0.18f, 0.18f, 0.18f);

        void Start()
        {
            SetupSliders();
            SetupDifficultyButtons();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            // Panel starts hidden; open it from the main menu or pause screen
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        // ── Panel open / close ────────────────────────────────────────────────

        public void Show()
        {
            if (optionsPanel != null) optionsPanel.SetActive(true);
            // Sync sliders to current saved values each time the panel opens
            SyncSlidersToOptions();
            HighlightDifficultyButton(OptionsManager.Instance.BaseDifficultyIndex);
        }

        public void Hide()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (optionsPanel == null)
            {
                Debug.LogError("Options Menu Error: optionsPanel is null!");
                return;
            }
            
            bool newState = !optionsPanel.activeSelf;
            Debug.Log($"Options Menu Toggle: Setting active to {newState}");
            
            if (newState) Show();
            else Hide();
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        private void SetupSliders()
        {
            if (bgmSlider != null)
            {
                bgmSlider.minValue = 0f;
                bgmSlider.maxValue = 1f;
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            }

            SyncSlidersToOptions();
        }

        private void SetupDifficultyButtons()
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                if (difficultyButtons[i] == null) continue;
                int index = i; // capture for closure
                difficultyButtons[i].onClick.AddListener(() => OnDifficultySelected(index));
            }
            HighlightDifficultyButton(OptionsManager.Instance != null ? OptionsManager.Instance.BaseDifficultyIndex : 0);
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnBGMChanged(float value)
        {
            OptionsManager.Instance?.SetBGMVolume(value);
        }

        private void OnSFXChanged(float value)
        {
            OptionsManager.Instance?.SetSFXVolume(value);
        }

        private void OnDifficultySelected(int index)
        {
            OptionsManager.Instance?.SetBaseDifficulty(index);
            HighlightDifficultyButton(index);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SyncSlidersToOptions()
        {
            if (OptionsManager.Instance == null) return;
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(OptionsManager.Instance.BGMVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(OptionsManager.Instance.SFXVolume);
        }

        private void HighlightDifficultyButton(int selectedIndex)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                if (difficultyButtons[i] == null) continue;
                difficultyButtons[i].image.color = i == selectedIndex ? activeButtonColor : inactiveButtonColor;

                var label = difficultyButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.fontStyle = i == selectedIndex ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }
}
