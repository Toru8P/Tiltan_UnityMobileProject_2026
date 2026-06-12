using UnityEngine;
using UnityEditor;
using System.IO;

namespace _Scripts.Editor
{
    public static class PrefabToIconConverter
    {
        private const int DefaultMaxSize = 128;

        [MenuItem("Assets/Icon from Prefab", false, 10)]
        public static void CreateIcon()
        {
            GameObject[] selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

            if (selectedPrefabs.Length == 0)
            {
                Debug.LogWarning("No prefabs selected in the Project window.");
                return;
            }

            foreach (GameObject prefab in selectedPrefabs)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(prefab))
                {
                    SaveIcon(prefab, DefaultMaxSize);
                }
            }
        }

        private static void SaveIcon(GameObject prefab, int maxSize)
        {
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
            {
                if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                {
                    Debug.LogWarning($"Preview for {prefab.name} is still loading. Please try again in a moment.");
                }
                else
                {
                    Debug.LogError($"Could not generate preview for {prefab.name}. Prefab might not have a renderer or be too complex.");
                }
                return;
            }

            // Compute target size preserving aspect ratio, clamped to maxSize
            int srcW = preview.width;
            int srcH = preview.height;
            if (srcW <= 0 || srcH <= 0)
            {
                Debug.LogError($"Invalid preview size for {prefab.name}");
                return;
            }

            float scale = Mathf.Min(1f, maxSize / (float)Mathf.Max(srcW, srcH));
            int targetW = Mathf.Max(1, Mathf.RoundToInt(srcW * scale));
            int targetH = Mathf.Max(1, Mathf.RoundToInt(srcH * scale));

            // Render to a temporary RenderTexture at the target size
            RenderTexture tmp = RenderTexture.GetTemporary(
                targetW,
                targetH,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(preview, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;

            // Use RGBA32 (8 bits per channel) for higher quality.
            Texture2D readableTexture = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            // Encode as PNG to preserve transparency
            byte[] bytes = readableTexture.EncodeToPNG();
            string dirPath = "Assets/Sprites/GeneratedIcons";
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string filePath = Path.Combine(dirPath, prefab.name + "_Icon.png");
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.ImportAsset(filePath);

            // Set as Sprite and optimize importer for mobile
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;

                // Reduce max size and enable compressed/crunched textures for smaller builds
                importer.maxTextureSize = Mathf.Max(32, maxSize);
                importer.textureCompression = TextureImporterCompression.Compressed;

#if UNITY_2017_3_OR_NEWER
                // Crunch helps reduce build size for compressed textures (works for some platforms/formats)
                importer.crunchedCompression = true;
                importer.compressionQuality = 50; // 0-100 (lower = smaller)
#endif

                // Per-platform overrides: use ETC2 (Android) and ASTC (iOS) if available
                var androidSettings = new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = importer.maxTextureSize,
                    format = TextureImporterFormat.ETC2_RGBA8
                };
                importer.SetPlatformTextureSettings(androidSettings);

                var iPhoneSettings = new TextureImporterPlatformSettings
                {
                    name = "iPhone",
                    overridden = true,
                    maxTextureSize = importer.maxTextureSize,
                    format = TextureImporterFormat.ASTC_4x4
                };
                importer.SetPlatformTextureSettings(iPhoneSettings);

                importer.SaveAndReimport();
            }

            Debug.Log($"Icon saved for {prefab.name} at {filePath} (size {targetW}x{targetH}, RGBA4444)");
        }
    }
}
