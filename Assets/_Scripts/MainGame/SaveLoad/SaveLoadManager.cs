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
        // Which slot's world we read/write. Static so it survives the menu -> gameplay scene change.
        private static int _activeSlot;

        private GameSave _current;
        private int _loadedSlot = -1;

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

        // Points the manager at a specific slot (e.g. from the new-game flow) and drops any cached state.
        public void SetActiveSlot(int slot)
        {
            _activeSlot = slot;
            _current = null;
            _loadedSlot = -1;
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

        // Persists the active slot's world state to disk.
        public void Save()
        {
            EnsureLoaded();
            Directory.CreateDirectory(SaveSlotManager.SaveDirectory);
            File.WriteAllText(CurrentWorldPath, SaveSerialization.Serialize(_current));
        }

        // Deletes the active slot's world file and resets in-memory state.
        public void Delete()
        {
            if (File.Exists(CurrentWorldPath)) File.Delete(CurrentWorldPath);
            _current = new GameSave();
            _loadedSlot = _activeSlot;
        }
    }
}
