using System.Collections;
using _Scripts.Managers;

using _Scripts.CharacterCreation;
using _Scripts.MainGame.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.UI
{
    public class OptionsMenuUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveAndQuitButton;

        [Header("Audio Bars")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        private void Awake()
        {
            if (optionsPanel == null)
                BuildFallbackMenu();
        }

        private void Start()
        {
            SetupSlider(bgmSlider, OnBGMChanged);
            SetupSlider(sfxSlider, OnSFXChanged);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.AddListener(SaveAndQuit);

            Hide();
        }

        public void Show()
        {
            if (optionsPanel == null)
                BuildFallbackMenu();
            if (optionsPanel == null) return;

            optionsPanel.SetActive(true);
            SyncAudioBars();
        }

        public void Hide()
        {
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void Toggle()
        {
            if (optionsPanel == null)
                BuildFallbackMenu();
            if (optionsPanel == null)
            {
                Debug.LogError("Options Menu Error: no options panel could be created.");
                return;
            }

            if (optionsPanel.activeSelf) Hide();
            else Show();
        }

        public void SaveAndQuit()
        {
            StartCoroutine(SaveAndQuitRoutine());
        }

        private IEnumerator SaveAndQuitRoutine()
        {
            SaveLoadManager saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
            Hide();
            yield return new WaitForEndOfFrame();

            SaveSlotManager slotManager = SaveSlotManager.GetOrCreate();
            saveLoadManager?.Save();

            SaveSlotData data = slotManager.LoadAllSlots().Find(slot => slot.slotIndex == (saveLoadManager != null ? saveLoadManager.ActiveSlot : 0));
            if (data == null)
                data = new SaveSlotData { slotIndex = saveLoadManager != null ? saveLoadManager.ActiveSlot : 0, sceneName = "TerrainTest" };

            if (saveLoadManager != null)
            {
                data.playtimeSeconds = saveLoadManager.Current.playtimeSeconds;
                data.sceneName = "TerrainTest";
            }

            if (GameInitData.HasCustomization)
            {
                data.characterId = GameInitData.Customization.CharacterId;
                data.characterName = GameInitData.Customization.PlayerName;
                data.skinColorIndex = GameInitData.Customization.SkinColorIndex;
                data.outfitColorIndex = GameInitData.Customization.OutfitColorIndex;
            }

            Debug.Log($"[OptionsMenuUI] Saving slot {data.slotIndex}.");
            yield return slotManager.SaveSlotWithScreenshot(data);

            PlayerPrefs.Save();
            PlayerCreationUIManager.RequestSaveSelection();
            SceneTransitionManager.LoadScene("PlayerCreation");
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(callback);
        }

        private void OnBGMChanged(float value)
        {
            OptionsManager.Instance?.SetBGMVolume(value);
        }

        private void OnSFXChanged(float value)
        {
            OptionsManager.Instance?.SetSFXVolume(value);
        }

        private void SyncAudioBars()
        {
            if (OptionsManager.Instance == null) return;
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(OptionsManager.Instance.BGMVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(OptionsManager.Instance.SFXVolume);
        }

        private void BuildFallbackMenu()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject panelObject = new GameObject("OptionsPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 430f);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.97f);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObject.transform.SetParent(panelObject.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(40f, 40f);
            contentRect.offsetMax = new Vector2(-40f, -40f);
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateLabel(contentObject.transform, "OPTIONS", 32f);
            bgmSlider = CreateAudioBar(contentObject.transform, "BGM Volume");
            sfxSlider = CreateAudioBar(contentObject.transform, "SFX Volume");
            saveAndQuitButton = CreateButton(contentObject.transform, "Save and Quit");
            closeButton = CreateButton(contentObject.transform, "Close");
            optionsPanel = panelObject;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string value, float fontSize)
        {
            GameObject labelObject = new GameObject(value, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            LayoutElement element = labelObject.AddComponent<LayoutElement>();
            element.minHeight = fontSize + 14f;
            return label;
        }

        private static Slider CreateAudioBar(Transform parent, string name)
        {
            CreateLabel(parent, name, 20f);
            GameObject sliderObject = new GameObject(name + "Bar", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.fillRect = CreateBarFill(sliderObject.transform);
            slider.direction = Slider.Direction.LeftToRight;
            LayoutElement element = sliderObject.AddComponent<LayoutElement>();
            element.minHeight = 32f;
            return slider;
        }

        private static RectTransform CreateBarFill(Transform parent)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(parent, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillObject.GetComponent<Image>().color = new Color(0.2f, 0.65f, 1f, 1f);
            return fillRect;
        }

        private static Button CreateButton(Transform parent, string value)
        {
            GameObject buttonObject = new GameObject(value + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
            CreateLabel(buttonObject.transform, value, 20f);
            LayoutElement element = buttonObject.AddComponent<LayoutElement>();
            element.minHeight = 44f;
            return buttonObject.GetComponent<Button>();
        }
    }
}