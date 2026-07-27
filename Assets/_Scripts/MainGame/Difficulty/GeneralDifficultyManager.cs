using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.MainGame.Difficulty
{
    public enum DifficultyPhase
    {
        None = 0,
        Easy,
        Normal,
        Hard,
        Expert
    }

    [Serializable]
    public class DifficultyPhaseChangedEvent : UnityEvent<DifficultyPhase> { }

    [Serializable]
    public class DifficultyPhaseEntry
    {
        public DifficultyPhase phase;
        public float startTime;
    }

    public class GeneralDifficultyManager : MonoBehaviour
    {
        [SerializeField] private List<DifficultyPhaseEntry> difficultyEntries = new();
        [SerializeField] private DifficultyPhaseChangedEvent onDifficultyPhaseChanged = new();

        private DifficultyPhase _currentPhase = DifficultyPhase.None;
        private float _elapsedTime;

        public DifficultyPhase CurrentPhase => _currentPhase;
        public float ElapsedTime => _elapsedTime;
        public IReadOnlyList<DifficultyPhaseEntry> DifficultyEntries => difficultyEntries;

        public void SubscribeOnChange(UnityAction<DifficultyPhase> subscriber)
        {
            if (subscriber == null) return;

            subscriber.Invoke(_currentPhase);
            onDifficultyPhaseChanged.AddListener(subscriber);
            Debug.Log($"Subscribed with current phase: {_currentPhase}");
        }

        public void UnsubscribeOnChange(UnityAction<DifficultyPhase> subscriber)
        {
            if (subscriber == null) return;

            onDifficultyPhaseChanged.RemoveListener(subscriber);
            
            Debug.Log($"Unsubscribed with current phase: {_currentPhase}");
        }

        private void Start()
        {
            _elapsedTime = 0f;
            StartCoroutine(CheckPhaseRoutine());
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
        }

        private IEnumerator CheckPhaseRoutine()
        {
            while (true)
            {
                DifficultyPhase phase = GetPhaseForTime(_elapsedTime);

                if (phase != _currentPhase)
                {
                    _currentPhase = phase;
                    Debug.Log($"Phase changed to: {_currentPhase}");
                    onDifficultyPhaseChanged.Invoke(phase);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        public void RestoreElapsedTime(float elapsedTime)
        {
            _elapsedTime = Mathf.Max(0f, elapsedTime);
            _currentPhase = GetPhaseForTime(_elapsedTime);
        }
        public DifficultyPhase GetPhaseForTime(float time)
        {
            DifficultyPhase result = DifficultyPhase.Easy;
            float bestStartTime = float.MinValue;

            if (difficultyEntries == null || difficultyEntries.Count == 0)
                return result;

            for (int i = 0; i < difficultyEntries.Count; i++)
            {
                DifficultyPhaseEntry entry = difficultyEntries[i];

                if (time >= entry.startTime && entry.startTime > bestStartTime)
                {
                    result = entry.phase;
                    bestStartTime = entry.startTime;
                }
            }

            return result;
        }

        public void SortEntriesByTime()
        {
            if (difficultyEntries == null) return;
            difficultyEntries.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        }
    }
}