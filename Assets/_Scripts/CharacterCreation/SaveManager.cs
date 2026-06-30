using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace _Scripts.CharacterCreation
{
    // Handles reading and writing save slot JSON files.
    // Save files are stored in Application.persistentDataPath/Saves/
    // Screenshot thumbnails are stored alongside as PNG files.
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        // Gameplay scene invokes this delegate when a load is confirmed.
        // Student A should assign their scene-loading logic here.
        public static Action<SaveSlotData> OnLoadRequested;

        private const string SaveFolder = "Saves";
        private const string SlotFilePrefix = "slot_";
        private const string SlotFileExtension = ".json";

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolder);

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

        // --- Write ---

        public void SaveSlot(SaveSlotData data)
        {
            Directory.CreateDirectory(SaveDirectory);
            string path = SlotPath(data.slotIndex);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public IEnumerator SaveSlotWithScreenshot(SaveSlotData data)
        {
            yield return new WaitForEndOfFrame();

            string thumbName = $"thumb_{data.slotIndex}.png";
            string thumbPath = Path.Combine(SaveDirectory, thumbName);
            Directory.CreateDirectory(SaveDirectory);

            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(thumbPath, texture.EncodeToPNG());
            Destroy(texture);

            data.thumbnailFileName = thumbName;
            data.saveDate = DateTime.UtcNow.ToString("o");
            SaveSlot(data);
        }

        // --- Read ---

        public List<SaveSlotData> LoadAllSlots()
        {
            var result = new List<SaveSlotData>();
            if (!Directory.Exists(SaveDirectory)) return result;

            foreach (string file in Directory.GetFiles(SaveDirectory, $"{SlotFilePrefix}*{SlotFileExtension}"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonConvert.DeserializeObject<SaveSlotData>(json);
                    if (data != null) result.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Failed to read {file}: {e.Message}");
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

        public void DeleteSlot(int slotIndex)
        {
            string path = SlotPath(slotIndex);
            if (File.Exists(path)) File.Delete(path);
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
