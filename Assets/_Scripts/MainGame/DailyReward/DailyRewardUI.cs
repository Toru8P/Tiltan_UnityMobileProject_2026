using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.DailyReward
{
    // Presents the "Daily Reward" popup: shows the pending reward and lets the player claim it, or
    // shows a countdown until the next reward unlocks. Opens itself automatically whenever
    // DailyRewardManager reports a reward is ready (e.g. right after the player opens the app from
    // the reminder notification).
    public class DailyRewardUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TextMeshProUGUI rewardNameText;
        [SerializeField] private TextMeshProUGUI rewardQuantityText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (claimButton != null) claimButton.onClick.AddListener(ClaimReward);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void Start()
        {
            Hide();

            if (DailyRewardManager.Instance == null) return;
            DailyRewardManager.Instance.OnRewardAvailable += Show;

            if (DailyRewardManager.Instance.IsRewardAvailable)
                Show();
        }

        private void OnDestroy()
        {
            if (DailyRewardManager.Instance != null)
                DailyRewardManager.Instance.OnRewardAvailable -= Show;
        }

        private void Update()
        {
            if (panel != null && panel.activeSelf)
                RefreshStatus();
        }

        // Opens the popup and refreshes its contents. Safe to call whether or not a reward is ready,
        // so a persistent "Daily Reward" button can also call this directly.
        public void Show()
        {
            if (panel == null || DailyRewardManager.Instance == null) return;
            panel.SetActive(true);
            RefreshReward();
            RefreshStatus();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void RefreshReward()
        {
            DailyRewardEntry reward = DailyRewardManager.Instance.GetCurrentReward();
            bool hasItem = reward.item != null;
            bool hasIcon = hasItem && reward.item.icon != null;

            if (rewardIcon != null)
            {
                rewardIcon.sprite = hasIcon ? reward.item.icon : null;
                rewardIcon.enabled = hasIcon;
            }
            if (rewardNameText != null) rewardNameText.text = hasItem ? reward.item.displayName : "Reward";
            if (rewardQuantityText != null) rewardQuantityText.text = $"x{Mathf.Max(1, reward.quantity)}";
        }

        private void RefreshStatus()
        {
            DailyRewardManager manager = DailyRewardManager.Instance;
            bool available = manager.IsRewardAvailable;

            if (claimButton != null) claimButton.interactable = available;
            if (statusText == null) return;

            statusText.text = available
                ? "Your daily reward is ready to claim!"
                : $"Next reward in {FormatCountdown(manager.TimeUntilNextReward)}";
        }

        private void ClaimReward()
        {
            if (DailyRewardManager.Instance == null || !DailyRewardManager.Instance.IsRewardAvailable) return;

            DailyRewardManager.Instance.ClaimReward();
            RefreshReward();
            RefreshStatus();
        }

        private static string FormatCountdown(System.TimeSpan remaining)
        {
            return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
