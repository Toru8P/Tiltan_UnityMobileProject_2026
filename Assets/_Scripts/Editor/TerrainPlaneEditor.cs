using _Scripts.MainGame;
using _Scripts.MainGame.Terrain;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(Chunk))]
    public class TerrainPlaneEditor : UnityEditor.Editor
    {
        // Editor-only preview controls (not saved to scene/prefab)
        private int _previewResolution = 128;
        private bool _autoRefresh = true;
        private Texture2D _previewTexture;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Chunk plane = (Chunk)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Noise Preview (Editor Only)", EditorStyles.boldLabel);

            _previewResolution = EditorGUILayout.IntSlider("Resolution", _previewResolution, 16, 512);
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);

            if (GUILayout.Button("Regenerate Preview"))
            {
                RegeneratePreview(plane);
            }

            // Regenerate during Layout so Layout and Repaint produce the same controls
            if (_autoRefresh && Event.current.type == EventType.Layout)
            {
                RegeneratePreview(plane);
            }

            if (_previewTexture)
            {
                // use EditorGUILayout.GetControlRect to allocate a stable rect
                Rect rect = EditorGUILayout.GetControlRect(false, 256f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
            }
        }

        private void RegeneratePreview(Chunk plane)
        {
            NoiseSettings settings = GetNoiseSettings(plane);

            int res = Mathf.Max(16, _previewResolution);
            if (!_previewTexture || _previewTexture.width != res || _previewTexture.height != res)
            {
                _previewTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
                {
                    name = "TerrainNoisePreview",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            float halfW = plane.Width * 0.5f;
            float halfH = plane.Height * 0.5f;
            Vector3 origin = plane.transform.position;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = x / (float)(res - 1);
                    float v = y / (float)(res - 1);

                    float localX = Mathf.Lerp(-halfW, halfW, u);
                    float localZ = Mathf.Lerp(-halfH, halfH, v);

                    float worldX = origin.x + localX;
                    float worldZ = origin.z + localZ;

                    float h = NoiseGenerator.SampleHeight(worldX, worldZ, settings);
                    float a = Mathf.Max(0.0001f, settings.amplitude);
                    float n = Mathf.InverseLerp(-a, a, h);

                    _previewTexture.SetPixel(x, y, new Color(n, n, n, 1f));
                }
            }

            _previewTexture.Apply(false, false);
        }

        // Reads private serialized field "_noiseSettings" from Chunk without changing runtime API.
        private NoiseSettings GetNoiseSettings(Chunk plane)
        {
            SerializedObject so = new SerializedObject(plane);
            SerializedProperty settingsProp = so.FindProperty("_noiseSettings");

            if (settingsProp == null)
                return NoiseSettings.Default;

            NoiseSettings s = NoiseSettings.Default;
            s.scale = settingsProp.FindPropertyRelative("scale").floatValue;
            s.amplitude = settingsProp.FindPropertyRelative("amplitude").floatValue;
            s.octaves = settingsProp.FindPropertyRelative("octaves").intValue;
            s.persistence = settingsProp.FindPropertyRelative("persistence").floatValue;
            s.lacunarity = settingsProp.FindPropertyRelative("lacunarity").floatValue;
            s.seed = settingsProp.FindPropertyRelative("seed").intValue;
            return s;
        }

        private void OnDisable()
        {
            if (_previewTexture)
            {
                Object.DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }
    }
}