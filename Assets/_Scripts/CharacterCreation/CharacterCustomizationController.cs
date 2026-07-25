using _Scripts.MainGame.Player;
using UnityEngine.InputSystem;

using UnityEngine;
using TMPro;

using System.Collections.Generic;

namespace _Scripts.CharacterCreation
{
    // Main driver for the character creation scene.
    // Spawns the Player preview on Start, builds color swatches from the configured options,
    // and pushes every change live to the preview model via CharacterAppearanceApplicator.
    public class CharacterCustomizationController : MonoBehaviour
    {
        [Header("Player Preview")]
        [SerializeField] private GameObject playerPreviewPrefab;
        [SerializeField] private Transform previewPivot;

        [Header("Color Options")]
        [SerializeField] private Color[] skinColorOptions = { Color.white };
        [SerializeField] private Color[] outfitColorOptions = { Color.white };

        [Header("UI")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Transform skinSwatchContainer;
        [SerializeField] private Transform outfitSwatchContainer;
        [SerializeField] private ColorSwatchUI swatchPrefab;

        private CharacterCustomization _customization;
        private CharacterAppearanceApplicator _applicator;

        private readonly List<ColorSwatchUI> _skinSwatches = new();
        private readonly List<ColorSwatchUI> _outfitSwatches = new();

        private void Start()
        {
            _customization = new CharacterCustomization();
            GameInitData.SetCustomization(_customization);

            SpawnPreview();
            BuildSwatches();
        }

        private void SpawnPreview()
        {
            if (previewPivot == null || playerPreviewPrefab == null) return;

            foreach (Transform child in previewPivot)
                Destroy(child.gameObject);

            GameObject instance = Instantiate(playerPreviewPrefab, previewPivot);
            instance.transform.localPosition = Vector3.zero;
            Rigidbody previewRigidbody = instance.GetComponentInChildren<Rigidbody>();

            instance.transform.localRotation = Quaternion.identity;
            PlayerInput previewInput = instance.GetComponentInChildren<PlayerInput>();
            if (previewInput != null) previewInput.enabled = false;
            PlayerInputHandler previewInputHandler = instance.GetComponentInChildren<PlayerInputHandler>();
            if (previewInputHandler != null) previewInputHandler.enabled = false;
            PlayerMovementController previewMovement = instance.GetComponentInChildren<PlayerMovementController>();
            if (previewMovement != null) previewMovement.enabled = false;
            if (previewRigidbody != null)
            {
                previewRigidbody.isKinematic = true;
                previewRigidbody.useGravity = false;
                previewRigidbody.linearVelocity = Vector3.zero;
                previewRigidbody.angularVelocity = Vector3.zero;
            }

            _applicator = instance.GetComponentInChildren<CharacterAppearanceApplicator>();
            RefreshApplicator();
        }

        // Wired to TMP_InputField.onValueChanged in the inspector.
        public void OnNameChanged(string newName)
        {
            _customization.PlayerName = string.IsNullOrWhiteSpace(newName) ? "Hero" : newName;
            GameInitData.SetCustomization(_customization);
        }

        private void BuildSwatches()
        {
            BuildSwatchRow(skinSwatchContainer, _skinSwatches, skinColorOptions, OnSkinColorSelected);
            BuildSwatchRow(outfitSwatchContainer, _outfitSwatches, outfitColorOptions, OnOutfitColorSelected);
            HighlightSwatch(_skinSwatches, 0);
            HighlightSwatch(_outfitSwatches, 0);
        }

        private void BuildSwatchRow(Transform container, List<ColorSwatchUI> list,
            Color[] colors, System.Action<int> callback)
        {
            foreach (Transform child in container) Destroy(child.gameObject);
            list.Clear();

            if (swatchPrefab == null) return;

            for (int i = 0; i < colors.Length; i++)
            {
                var swatch = Instantiate(swatchPrefab, container);
                swatch.Init(colors[i], i, callback);
                list.Add(swatch);
            }
        }

        private void OnSkinColorSelected(int index)
        {
            _customization.SkinColorIndex = index;
            HighlightSwatch(_skinSwatches, index);
            GameInitData.SetCustomization(_customization);
            RefreshApplicator();
        }

        private void OnOutfitColorSelected(int index)
        {
            _customization.OutfitColorIndex = index;
            HighlightSwatch(_outfitSwatches, index);
            GameInitData.SetCustomization(_customization);
            RefreshApplicator();
        }

        private void HighlightSwatch(List<ColorSwatchUI> swatches, int selectedIndex)
        {
            for (int i = 0; i < swatches.Count; i++)
                swatches[i].SetSelected(i == selectedIndex);
        }

        private void RefreshApplicator()
        {
            if (_applicator == null) return;
            _applicator.ApplyCustomization(skinColorOptions, outfitColorOptions, _customization);
        }
    }
}
