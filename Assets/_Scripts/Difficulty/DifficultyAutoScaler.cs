using UnityEngine;

namespace _Scripts.Difficulty
{
    /// <summary>
    /// Add this to any GameObject with components implementing IDifficultyScalable 
    /// to ensure they stay updated when difficulty changes.
    /// </summary>
    public class DifficultyAutoScaler : MonoBehaviour
    {
        private IDifficultyScalable[] _scalables;

        private void Awake()
        {
            _scalables = GetComponentsInChildren<IDifficultyScalable>(true);
        }

        private void OnEnable()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.AddListener(OnDifficultyChanged);
                
                // Initialize
                if (DifficultyManager.Instance.CurrentSettings != null)
                {
                    OnDifficultyChanged(DifficultyManager.Instance.CurrentSettings);
                }
            }
        }

        private void OnDisable()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(OnDifficultyChanged);
            }
        }

        private void OnDifficultyChanged(DifficultySettings settings)
        {
            foreach (var scalable in _scalables)
            {
                scalable.ApplyDifficulty(settings);
            }
        }
    }
}
