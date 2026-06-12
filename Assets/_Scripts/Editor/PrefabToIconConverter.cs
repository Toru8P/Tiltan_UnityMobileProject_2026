using UnityEngine;
using UnityEditor;
using System.IO;

namespace _Scripts.Editor
{
    public static class PrefabToIconConverter
    {
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
                    SaveIcon(prefab);
                }
            }
        }

        private static void SaveIcon(GameObject prefab)
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

            // Convert to readable Texture2D
            RenderTexture tmp = RenderTexture.GetTemporary(
                preview.width,
                preview.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(preview, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            Texture2D readableTexture = new Texture2D(preview.width, preview.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableTexture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            byte[] bytes = readableTexture.EncodeToPNG();
            string dirPath = "Assets/Sprites/GeneratedIcons";
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
            
            string filePath = Path.Combine(dirPath, prefab.name + "_Icon.png");
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.ImportAsset(filePath);

            // Set as Sprite
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Debug.Log($"Icon saved for {prefab.name} at {filePath}");
        }
    }
}
