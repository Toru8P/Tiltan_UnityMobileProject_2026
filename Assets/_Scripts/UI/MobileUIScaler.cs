using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Scripts.UI
{
    // Attach to the root Canvas in the scene.
    // Fixes two common mobile UI problems at runtime:
    //   1. Canvas set to Constant Pixel Size → everything is tiny on high-DPI phones.
    //   2. TMP font sizes below a readable threshold for small screens.
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public class MobileUIScaler : MonoBehaviour
    {
        [Header("Canvas Scaling")]
        [Tooltip("Reference resolution to design against. 1080x1920 is standard portrait mobile.")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

        [Tooltip("0 = match width, 1 = match height, 0.5 = balanced.")]
        [Range(0f, 1f)]
        [SerializeField] private float matchWidthOrHeight = 0.5f;

        [Header("Text Readability")]
        [Tooltip("Any TMP text smaller than this (in reference-resolution points) will be bumped up.")]
        [SerializeField] private float minFontSize = 26f;

        void Awake()
        {
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        // Wait one frame so that dynamic slots (created in Start) are present before we scan.
        IEnumerator Start()
        {
            yield return null;
            EnforceMinFontSizes();
        }

        private void EnforceMinFontSizes()
        {
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.fontSize < minFontSize)
                    tmp.fontSize = minFontSize;
            }
        }

        // Call this after dynamically spawning new UI (e.g. crafting recipe entries).
        public void RefreshTextSizes()
        {
            EnforceMinFontSizes();
        }
    }
}
