using System.IO;
using _Scripts.CharacterCreation;
using _Scripts.MainGame.Difficulty;
using _Scripts.MainGame.Player;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    public class SaveLoadManager : MonoBehaviour
    {
        [SerializeField] private float saveInterval = 15f;

        private static int _activeSlot;
        private GameSave _current;
        private int _loadedSlot = -1;
        private bool _dirty;
        private float _timer;

        public int ActiveSlot => _activeSlot;
        public static string WorldPath(int slot) => Path.Combine(SaveSlotManager.SaveDirectory, $"slot_{slot}_world.json");
        public string CurrentWorldPath => WorldPath(_activeSlot);
        public bool HasSaveFile => File.Exists(CurrentWorldPath);

        public GameSave Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookLoadRequests()
        {
            SaveSlotManager.OnLoadRequested += data =>
            {
                if (data != null) _activeSlot = data.slotIndex;
            };
        }

        public static void SetActiveSlot(int slot)
        {
            _activeSlot = Mathf.Max(0, slot);
        }

        public static void StartNewGame(int slot, int seed)
        {
            _activeSlot = Mathf.Max(0, slot);
            GameSave save = new GameSave();
            save.terrain.seed = seed;
            Directory.CreateDirectory(SaveSlotManager.SaveDirectory);
            File.WriteAllText(WorldPath(_activeSlot), SaveSerialization.Serialize(save));
        }

        private void Start()
        {
            EnsureLoaded();
            RestoreState();
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

        public void MarkDirty()
        {
            _dirty = true;
        }

        private void Update()
        {
            if (!_dirty) return;
            _timer += Time.deltaTime;
            if (_timer >= saveInterval) Flush();
        }

        public void Flush()
        {
            _timer = 0f;
            if (!_dirty) return;
            Save();
        }

        public void Save()
        {
            CaptureState();
            _dirty = false;
            _timer = 0f;
            WriteToDisk();
        }

        public void LoadSlot(int slot)
        {
            SetActiveSlot(slot);
            _current = null;
            EnsureLoaded();
            RestoreState();
        }

        private void CaptureState()
        {
            EnsureLoaded();
            CharacterCustomization customization = GameInitData.GetCustomizationOrSaved();
            if (customization != null)
            {
                _current.characterId = customization.CharacterId;
                _current.characterName = customization.PlayerName;
                _current.skinColorIndex = customization.SkinColorIndex;
                _current.outfitColorIndex = customization.OutfitColorIndex;
            }

            PlayerStatsController stats = FindFirstObjectByType<PlayerStatsController>();
            if (stats == null) return;
            _current.hasPlayer = true;
            _current.player.position = stats.transform.position;
            _current.player.rotationY = stats.transform.eulerAngles.y;
            stats.WriteSaveData(_current.player);

            GeneralDifficultyManager difficulty = FindFirstObjectByType<GeneralDifficultyManager>();
            if (difficulty != null) _current.playtimeSeconds = difficulty.ElapsedTime;
        }

        private void RestoreState()
        {
            EnsureLoaded();
            CharacterCustomization customization = new CharacterCustomization
            {
                CharacterId = _current.characterId,
                PlayerName = string.IsNullOrWhiteSpace(_current.characterName) ? "Hero" : _current.characterName,
                SkinColorIndex = _current.skinColorIndex,
                OutfitColorIndex = _current.outfitColorIndex
            };
            if (_current.hasPlayer && !string.IsNullOrWhiteSpace(_current.characterName)) GameInitData.SetCustomization(customization);

            if (_current.hasPlayer)
            {
                PlayerStatsController stats = FindFirstObjectByType<PlayerStatsController>();
                if (stats != null)
                {
                    stats.transform.SetPositionAndRotation(_current.player.position, Quaternion.Euler(0f, _current.player.rotationY, 0f));
                    stats.ReadSaveData(_current.player);
                }
            }

            GeneralDifficultyManager difficulty = FindFirstObjectByType<GeneralDifficultyManager>();
            if (difficulty != null) difficulty.RestoreElapsedTime(_current.playtimeSeconds);
        }

        private void WriteToDisk()
        {
            EnsureLoaded();
            Directory.CreateDirectory(SaveSlotManager.SaveDirectory);
            File.WriteAllText(CurrentWorldPath, SaveSerialization.Serialize(_current));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        public void Delete()
        {
            if (File.Exists(CurrentWorldPath)) File.Delete(CurrentWorldPath);
            _current = new GameSave();
            _loadedSlot = _activeSlot;
            _dirty = false;
            _timer = 0f;
        }
    }
}
