using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.MainGame
{
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private const string ObjectName = "SceneTransitionManager";
        private const float DefaultFadeDuration = 0.35f;

        private static SceneTransitionManager _instance;
        private CanvasGroup _canvasGroup;
        private Coroutine _transitionCoroutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (_instance != null) return;

            GameObject transitionObject = new GameObject(ObjectName);
            _instance = transitionObject.AddComponent<SceneTransitionManager>();
            DontDestroyOnLoad(transitionObject);
        }

        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            EnsureInstance();
            _instance.BeginLoad(sceneName);
        }

        public static void FadeIn()
        {
            EnsureInstance();
            _instance.StartFade(0f, 1f, DefaultFadeDuration);
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            CreateInstance();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
        }

        private void BeginLoad(string sceneName)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            yield return FadeRoutine(0f, 1f, DefaultFadeDuration);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null) yield break;

            yield return operation;
            yield return null;
            yield return FadeRoutine(1f, 0f, DefaultFadeDuration);
            _transitionCoroutine = null;
        }

        private void StartFade(float from, float to, float duration)
        {
            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(FadeRoutine(from, to, duration));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            CreateOverlay();
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = from;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = to;
            _canvasGroup.blocksRaycasts = to > 0.01f;
            _transitionCoroutine = null;
        }

        private void CreateOverlay()
        {
            if (_canvasGroup != null) return;

            GameObject canvasObject = new GameObject("FadeCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("FadeOverlay");
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;
            _canvasGroup = imageObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}