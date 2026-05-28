using System;
using UnityEngine;

namespace _Scripts.Difficulty
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
