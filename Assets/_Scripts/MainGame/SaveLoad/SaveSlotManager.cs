using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using _Scripts.CharacterCreation;
using Newtonsoft.Json;
using UnityEngine;


namespace _Scripts.MainGame.SaveLoad
{
    // Handles reading and writing save slot JSON files.
    // Save files are stored in Application.persistentDataPath/Saves/
    // Screenshot thumbnails are stored alongside as PNG files.
    public class SaveSlotManager : MonoBehaviour
    {
        public static SaveSlotManager Instance { get; private set; }

        // Gameplay scene invokes this delegate when a load is confirmed.
        // Student A should assign their scene-loading logic here.
        public const int MaxSaveSlots = 3;


        public static Action<SaveSlotData> OnLoadRequested;

        private const string SaveFolder = "Saves";
        private const string SlotFilePrefix = "slot_";
        private const string SlotFileExtension = ".json";
        // World-state files live in the same folder as slot_N_world.json; used to tell them apart from metadata.
        private const string WorldFileSuffix = "_world.json";

        // Shared by the world-state save system so slot metadata and world files live together.
        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolder);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static SaveSlotManager GetOrCreate()
        {
            if (Instance != null) return Instance;

            GameObject managerObject = new GameObject("SaveManager");
            return managerObject.AddComponent<SaveSlotManager>();
        }


        public void SaveSlot(SaveSlotData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveSlotManager] Cannot save a null slot.");
                return;
            }

            Directory.CreateDirectory(SaveDirectory);
            string path = SlotPath(data.slotIndex);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveSlotManager] Saved slot {data.slotIndex} metadata to {path}.");
        }

        public void UpdateCustomization(int slotIndex, CharacterCustomization customization)
        {
            if (customization == null) return;

            SaveSlotData data = LoadAllSlots().Find(slot => slot.slotIndex == slotIndex);
            if (data == null) return;

            data.characterId = string.IsNullOrWhiteSpace(data.characterId) ? customization.CharacterId : data.characterId;
            data.characterName = customization.PlayerName;
            data.skinColorIndex = customization.SkinColorIndex;
            data.outfitColorIndex = customization.OutfitColorIndex;
            data.saveDate = DateTime.UtcNow.ToString("o");
            SaveSlot(data);
        }

        public IEnumerator SaveSlotWithScreenshot(SaveSlotData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveSlotManager] Cannot capture a screenshot for a null slot.");
                yield break;
            }

            List<Canvas> hiddenCanvases = HideCanvasesForScreenshot();
            List<Renderer> hiddenRenderers = HideTaggedRenderersForScreenshot();

            try
            {
                yield return new WaitForEndOfFrame();

                string thumbName = $"thumb_{data.slotIndex}.png";
                string thumbPath = Path.Combine(SaveDirectory, thumbName);
                Directory.CreateDirectory(SaveDirectory);

                Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(thumbPath, texture.EncodeToPNG());
                Destroy(texture);

                data.thumbnailFileName = thumbName;
                data.saveDate = DateTime.UtcNow.ToString("o");
                SaveSlot(data);
            }
            finally
            {
                RestoreCanvases(hiddenCanvases);
                RestoreRenderers(hiddenRenderers);
            }
        }

        private static List<Canvas> HideCanvasesForScreenshot()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Canvas> hiddenCanvases = new List<Canvas>();

            foreach (Canvas canvas in canvases)
            {
                if (!canvas.enabled) continue;
                canvas.enabled = false;
                hiddenCanvases.Add(canvas);
            }

            return hiddenCanvases;
        }

        private static List<Renderer> HideTaggedRenderersForScreenshot()
        {
            List<Renderer> hiddenRenderers = new List<Renderer>();
            string[] excludedTags = { "Player", "Enemy" };

            foreach (string tag in excludedTags)
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject taggedObject in taggedObjects)
                {
                    Renderer[] renderers = taggedObject.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (!renderer.enabled || hiddenRenderers.Contains(renderer)) continue;
                        renderer.enabled = false;
                        hiddenRenderers.Add(renderer);
                    }
                }
            }

            return hiddenRenderers;
        }

        private static void RestoreCanvases(List<Canvas> canvases)
        {
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null) canvas.enabled = true;
            }
        }

        private static void RestoreRenderers(List<Renderer> renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null) renderer.enabled = true;
            }
        }

        // --- Read ---

        public List<SaveSlotData> LoadAllSlots()
        {
            var result = new List<SaveSlotData>();
            if (!Directory.Exists(SaveDirectory)) return result;

            foreach (string file in Directory.GetFiles(SaveDirectory, $"{SlotFilePrefix}*{SlotFileExtension}"))
            {
                // World-state files (slot_N_world.json) share the slot_*.json pattern — they are not slot metadata.
                if (file.EndsWith(WorldFileSuffix)) continue;

                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<SaveSlotData>(json);
                    if (data != null) result.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSlotManager] Failed to read {file}: {e.Message}");
                }
            }

            result.Sort((a, b) => b.slotIndex.CompareTo(a.slotIndex));
            return result;
        }

        public IEnumerator LoadThumbnail(string fileName, Action<Texture2D> callback)
        {
            string path = Path.Combine(SaveDirectory, fileName);
            if (!File.Exists(path)) { callback?.Invoke(null); yield break; }

            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            callback?.Invoke(tex);
        }

        // --- Delete ---

        // Removes everything that belongs to a slot: metadata, thumbnail, and the world save.
        public void DeleteSlot(int slotIndex)
        {
            string meta = SlotPath(slotIndex);
            if (File.Exists(meta)) File.Delete(meta);

            string thumb = Path.Combine(SaveDirectory, $"thumb_{slotIndex}.png");
            if (File.Exists(thumb)) File.Delete(thumb);

            string world = SaveLoadManager.WorldPath(slotIndex);
            if (File.Exists(world)) File.Delete(world);
        }

        // Returns the lowest slot index not used by any existing slot (metadata OR world file),
        // so a new game always lands on a fresh slot instead of overwriting an old one.
        public int GetNextFreeSlot()
        {
            var used = new HashSet<int>();

            foreach (SaveSlotData data in LoadAllSlots())
                used.Add(data.slotIndex);

            if (Directory.Exists(SaveDirectory))
            {
                foreach (string file in Directory.GetFiles(SaveDirectory, $"{SlotFilePrefix}*{WorldFileSuffix}"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string mid = name.Substring(SlotFilePrefix.Length);
                    int underscore = mid.IndexOf('_');
                    if (underscore > 0 && int.TryParse(mid.Substring(0, underscore), out int idx))
                        used.Add(idx);
                }
            }

            if (used.Count >= MaxSaveSlots)
                return -1;

            int slot = 0;
            while (used.Contains(slot)) slot++;
            return slot;
        }

        // --- Load trigger ---

        public void RequestLoad(SaveSlotData data)
        {
            OnLoadRequested?.Invoke(data);
        }

        private string SlotPath(int index) =>
            Path.Combine(SaveDirectory, $"{SlotFilePrefix}{index}{SlotFileExtension}");
    }
}
