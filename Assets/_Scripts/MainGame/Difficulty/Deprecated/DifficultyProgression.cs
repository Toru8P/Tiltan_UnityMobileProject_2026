using System;
using UnityEngine;

namespace _Scripts.MainGame.Difficulty.Deprecated
{
    [Serializable]
    public struct DifficultyThreshold
    {
        public float timeThreshold;
        public DifficultySettings settings;
    }

    [CreateAssetMenu(fileName = "NewDifficultyProgression", menuName = "Difficulty/Progression")]
    public class DifficultyProgression : ScriptableObject
    {
        public DifficultyThreshold[] thresholds;
        
        // Given the current survival time, returns which DifficultySettings should be active.
        // Walks the sorted list and picks the highest threshold the time has passed.
        // Example: if thresholds are 0s/60s/180s and time=120s → returns the 60s settings.
        public DifficultySettings GetSettingsForTime(float time)
        {
            if (thresholds == null || thresholds.Length == 0) return null;

            DifficultySettings current = thresholds[0].settings;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (time >= thresholds[i].timeThreshold)
                {
                    current = thresholds[i].settings;
                }
                else
                {
                    break;
                }
            }
            return current;
        }
    }
}
