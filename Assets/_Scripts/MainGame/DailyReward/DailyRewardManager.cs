using System;
using System.Collections;
using _Scripts.MainGame.Inventory;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace _Scripts.MainGame.DailyReward
{
    // Drives the "come back tomorrow" loop: schedules a local mobile notification for whenever the
    // next reward unlocks, and lets DailyRewardUI grant/claim that reward once the player returns.
    // Progress (last claim time, streak day) persists in PlayerPrefs, independent of save slots, since
    // the reward cadence should keep counting down even if the player starts a new game.
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        private const string AndroidChannelId = "daily_reward_channel";
        private const string AndroidChannelGroupId = "daily_reward_group";
        private const string LastClaimTicksPrefKey = "DailyReward_LastClaimUtcTicks";
        private const string StreakIndexPrefKey = "DailyReward_StreakIndex";
        private const string PendingAndroidNotificationIdPrefKey = "DailyReward_PendingAndroidNotificationId";
        private const string PendingIOSNotificationIdPrefKey = "DailyReward_PendingIOSNotificationIdentifier";
        private static readonly TimeSpan MinimumScheduleDelay = TimeSpan.FromSeconds(1);

        [Header("Reward Setup")]
        [SerializeField] private DailyRewardDefinition rewardDefinition;
        [Tooltip("Hours the player must wait between claims (24 = classic daily reward cadence).")]
        [SerializeField] private double rewardIntervalHours = 24d;

#if UNITY_ANDROID || UNITY_IOS
        [Header("Notification Content")]
        [SerializeField] private string notificationTitle = "Daily Reward Ready!";
        [SerializeField] private string notificationBody = "Your daily reward is waiting for you. Come back and claim it!";
#endif

        // Raised once at startup if a reward is already available to claim (e.g. the player opened
        // the app in response to the reminder notification).
        public event Action OnRewardAvailable;
        // Raised whenever a reward is successfully claimed, with the entry that was granted.
        public event Action<DailyRewardEntry> OnRewardClaimed;

        public DailyRewardDefinition RewardDefinition => rewardDefinition;
        public TimeSpan RewardInterval => TimeSpan.FromHours(rewardIntervalHours);

        // True once enough time has elapsed since the last claim, or the player has never claimed before.
        public bool IsRewardAvailable => TimeUntilNextReward <= TimeSpan.Zero;

        // How long until the next reward unlocks. Zero (or negative) once it is available.
        public TimeSpan TimeUntilNextReward
        {
            get
            {
                if (!HasClaimedBefore) return TimeSpan.Zero;
                DateTime nextClaimUtc = LastClaimUtc + RewardInterval;
                TimeSpan remaining = nextClaimUtc - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        private bool HasClaimedBefore => PlayerPrefs.HasKey(LastClaimTicksPrefKey);

        private DateTime LastClaimUtc
        {
            get
            {
                string ticks = PlayerPrefs.GetString(LastClaimTicksPrefKey, "0");
                return long.TryParse(ticks, out long parsedTicks) ? new DateTime(parsedTicks, DateTimeKind.Utc) : DateTime.UtcNow;
            }
            set => PlayerPrefs.SetString(LastClaimTicksPrefKey, value.Ticks.ToString());
        }

        private int StreakIndex
        {
            get => PlayerPrefs.GetInt(StreakIndexPrefKey, 0);
            set => PlayerPrefs.SetInt(StreakIndexPrefKey, value);
        }

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RegisterAndroidNotificationChannel();
            StartCoroutine(RequestNotificationPermissionThenArm());
        }

        private void OnApplicationPause(bool paused)
        {
            // Re-arm the reminder whenever the app is backgrounded so the notification always
            // reflects the freshest countdown, even if the player claimed just before switching apps.
            if (!paused || IsRewardAvailable) return;
            CancelPendingNotification();
            ScheduleNotification(TimeUntilNextReward);
        }

        // Requests OS notification permission (required on Android 13+ and iOS), then either fires
        // OnRewardAvailable immediately or arms the reminder notification for later.
        private IEnumerator RequestNotificationPermissionThenArm()
        {
#if UNITY_ANDROID
            var permissionRequest = new PermissionRequest();
            while (permissionRequest.Status == PermissionStatus.RequestPending)
                yield return null;
#elif UNITY_IOS
            using (var authorizationRequest = new AuthorizationRequest(
                AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true))
            {
                while (!authorizationRequest.IsFinished)
                    yield return null;
            }
#else
            yield return null;
#endif

            // The app is in the foreground right now, so clear any reminder that was pending.
            CancelPendingNotification();

            if (IsRewardAvailable)
                OnRewardAvailable?.Invoke();
            else
                ScheduleNotification(TimeUntilNextReward);
        }

        // Returns the reward the player will receive for the current streak day, without granting it.
        public DailyRewardEntry GetCurrentReward()
        {
            if (rewardDefinition == null || rewardDefinition.Rewards == null || rewardDefinition.Rewards.Length == 0)
                return default;

            int dayIndex = StreakIndex % rewardDefinition.Rewards.Length;
            return rewardDefinition.Rewards[dayIndex];
        }

        // Grants the current day's reward to the player's inventory, advances the streak, resets the
        // countdown, and re-arms the reminder notification for the next cycle.
        public DailyRewardEntry ClaimReward()
        {
            if (!IsRewardAvailable)
            {
                Debug.LogWarning("[DailyReward] ClaimReward was called before the reward became available.");
                return default;
            }

            DailyRewardEntry reward = GetCurrentReward();
            if (reward.item != null && InventoryManager.Instance != null)
                InventoryManager.Instance.AddItem(reward.item, Mathf.Max(1, reward.quantity));

            LastClaimUtc = DateTime.UtcNow;
            StreakIndex++;
            PlayerPrefs.Save();

            CancelPendingNotification();
            ScheduleNotification(RewardInterval);

            OnRewardClaimed?.Invoke(reward);
            return reward;
        }

        private void RegisterAndroidNotificationChannel()
        {
#if UNITY_ANDROID
            var group = new AndroidNotificationChannelGroup
            {
                Id = AndroidChannelGroupId,
                Name = "Daily Rewards"
            };
            AndroidNotificationCenter.RegisterNotificationChannelGroup(group);

            var channel = new AndroidNotificationChannel
            {
                Id = AndroidChannelId,
                Name = "Daily Reward",
                Importance = Importance.Default,
                Description = "Reminders to claim your daily reward.",
                Group = AndroidChannelGroupId
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
        }

        // Schedules the local reminder to fire after `delay`, replacing any previous pending id.
        private void ScheduleNotification(TimeSpan delay)
        {
            if (delay < MinimumScheduleDelay) delay = MinimumScheduleDelay;

#if UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = notificationTitle,
                Text = notificationBody,
                FireTime = DateTime.Now + delay
            };
            int notificationId = AndroidNotificationCenter.SendNotification(notification, AndroidChannelId);
            PlayerPrefs.SetInt(PendingAndroidNotificationIdPrefKey, notificationId);
#elif UNITY_IOS
            string identifier = Guid.NewGuid().ToString();
            var notification = new iOSNotification
            {
                Identifier = identifier,
                Title = notificationTitle,
                Body = notificationBody,
                ShowInForeground = false,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = delay,
                    Repeats = false
                }
            };
            iOSNotificationCenter.ScheduleNotification(notification);
            PlayerPrefs.SetString(PendingIOSNotificationIdPrefKey, identifier);
#endif
        }

        // Cancels whatever reminder notification is currently pending, if any.
        private void CancelPendingNotification()
        {
#if UNITY_ANDROID
            if (PlayerPrefs.HasKey(PendingAndroidNotificationIdPrefKey))
            {
                AndroidNotificationCenter.CancelScheduledNotification(PlayerPrefs.GetInt(PendingAndroidNotificationIdPrefKey));
                PlayerPrefs.DeleteKey(PendingAndroidNotificationIdPrefKey);
            }
#elif UNITY_IOS
            if (PlayerPrefs.HasKey(PendingIOSNotificationIdPrefKey))
            {
                iOSNotificationCenter.RemoveScheduledNotification(PlayerPrefs.GetString(PendingIOSNotificationIdPrefKey));
                PlayerPrefs.DeleteKey(PendingIOSNotificationIdPrefKey);
            }
#endif
        }
    }
}
