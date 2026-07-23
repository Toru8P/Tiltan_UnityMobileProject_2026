using System.IO;
using _Scripts.CharacterCreation;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    // In-game save/load: serializes world state (terrain + inventory) for the active save slot,
    // stored next to the slot metadata that SaveSlotManager owns (persistentDataPath/Saves/).
    //
    // The active slot is chosen by the menu (SaveSlotManager.OnLoadRequested) or the new-game flow
    // (SetActiveSlot). Terrain and Inventory read/write only their own section of Current, then call Save().
    public class SaveLoadManager : MonoBehaviour
    {
        [Tooltip("Seconds between real disk writes. Changes are staged in memory (cheap) and flushed to " +
                 "disk on this interval, so frequent updates don't cause hitches.")]
        [SerializeField] private float saveInterval = 15f;

        // Which slot's world we read/write. Static so it survives the menu -> gameplay scene change.
        private static int _activeSlot;

        private GameSave _current;
        private int _loadedSlot = -1;

        // Set when the cached save has unwritten changes; flushed to disk on the interval.
        private bool _dirty;
        private float _timer;

        public int ActiveSlot => _activeSlot;

        public static string WorldPath(int slot) => Path.Combine(SaveSlotManager.SaveDirectory, $"slot_{slot}_world.json");
        public string CurrentWorldPath => WorldPath(_activeSlot);

        // True if the active slot already has a world file on disk.
        public bool HasSaveFile => File.Exists(CurrentWorldPath);

        // The shared in-memory world state for the active slot. Lazily loaded on first access.
        public GameSave Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        // As soon as the menu requests a load, record the chosen slot — before the gameplay scene starts.
        // Registered once per app launch; the actual scene transition stays the load-screen's responsibility.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookLoadRequests()
        {
            SaveSlotManager.OnLoadRequested += data =>
            {
                if (data != null) _activeSlot = data.slotIndex;
            };
        }

        // Points the save system at a specific slot (e.g. from the new-game flow) before the gameplay
        // scene loads. Static because the menu scene has no gameplay SaveLoadManager instance.
        // Any live instance reloads automatically on next access (EnsureLoaded compares slots).
        public static void SetActiveSlot(int slot)
        {
            _activeSlot = slot;
        }

        // Begins a new game on the given slot with a chosen seed. Writes a fresh world file that records
        // the seed but has no terrain yet (hasTerrain = false), so the gameplay scene generates exactly
        // that world. Static so it can run from the menu scene, which has no SaveLoadManager instance.
        public static void StartNewGame(int slot, int seed)
        {
            _activeSlot = slot;

            GameSave save = new GameSave();
            save.terrain.seed = seed;

            Directory.CreateDirectory(SaveSlotManager.SaveDirectory);
            File.WriteAllText(WorldPath(slot), SaveSerialization.Serialize(save));
        }

        private void EnsureLoaded()
        {
            if (_current != null && _loadedSlot == _activeSlot) return;
            _loadedSlot = _activeSlot;

            string path = CurrentWorldPath;
            _current = File.Exists(path)
                ? SaveSerialization.Deserialize<GameSave>(File.ReadAllText(path)) ?? new GameSave()
                : new GameSave();
        }

        // Stages changes: the cached save (Current) is written to disk on the next interval, not now.
        // Use this for frequent updates (chunk crossings, inventory changes) to avoid per-change hitches.
        public void MarkDirty()
        {
            _dirty = true;
        }

        private void Update()
        {
            if (!_dirty) return;

            _timer += Time.deltaTime;
            if (_timer >= saveInterval)
                Flush();
        }

        // Writes the cached save to disk immediately if there are pending changes, and resets the timer.
        // Called on the interval, and on pause/quit so nothing is lost.
        public void Flush()
        {
            _timer = 0f;
            if (!_dirty) return;
            _dirty = false;
                WriteToDisk();
        }

        // Forces an immediate write regardless of the dirty flag (explicit "save now").
        public void Save()
        {
            _dirty = false;
            _timer = 0f;
            WriteToDisk();
        }

        private void WriteToDisk()
        {
            EnsureLoaded();
            Directory.CreateDirectory(SaveSlotManager.SaveDirectory);
            File.WriteAllText(CurrentWorldPath, SaveSerialization.Serialize(_current));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush(); // mobile: app backgrounded — persist now
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        // Deletes the active slot's world file and resets in-memory state.
        public void Delete()
        {
            if (File.Exists(CurrentWorldPath)) File.Delete(CurrentWorldPath);
            _current = new GameSave();
            _loadedSlot = _activeSlot;
            _dirty = false; // don't let a pending flush recreate the file we just deleted
            _timer = 0f;
        }
    }
}
