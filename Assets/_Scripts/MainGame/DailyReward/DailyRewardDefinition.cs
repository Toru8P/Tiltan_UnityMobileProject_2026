using System;
using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.DailyReward
{
    // A single day's worth of reward within a repeating daily-reward cycle.
    // Reuses the existing ItemData/inventory system so rewards can be currency-like resources or items.
    [Serializable]
    public struct DailyRewardEntry
    {
        public ItemData item;
        public int quantity;
    }

    // Designer-facing list of rewards granted on consecutive days. The cycle loops once every
    // entry has been claimed, so a 7-day list repeats weekly.
    [CreateAssetMenu(fileName = "NewDailyRewardDefinition", menuName = "Survival/Daily Reward Definition")]
    public class DailyRewardDefinition : ScriptableObject
    {
        [Tooltip("Rewards granted in order as the player claims consecutive days; the cycle repeats after the last entry.")]
        [SerializeField] private DailyRewardEntry[] rewards;

        public DailyRewardEntry[] Rewards => rewards;
    }
}
