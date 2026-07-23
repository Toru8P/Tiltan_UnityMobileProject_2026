using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.MainGame.Pool;
using _Scripts.MainGame.SaveLoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.MainGame.Terrain
{
    public class Terrain : MonoBehaviour
    {
        [Header("Noise Settings")] [SerializeField]
        private NoiseSettings noiseSettings = NoiseSettings.Default;

        [Header("Generation Prefabs")] public GameObject[] treePrefabs;
        public GameObject[] rockPrefabs;
        public GameObject[] bushPrefabs;
        public GameObject[] smallNaturePrefabs;
        [SerializeField] private string terrainLayerName = "TerrainGround";

        [Header("Probabilities")] [Range(0, 1)]
        public float treeChance = 0.25f; // Increased

        [Range(0, 1)] public float rockChance = 0.2f;
        [Range(0, 1)] public float bushChance = 0.1f;
        [Range(0, 1)] public float smallChance = 0.45f;

        [Range(2, 256)] public int resolution = 10;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private int renderDistance = 2;

        [Header("Seed")]
        [Tooltip("World seed. The same seed reproduces identical terrain height AND object placement.")]
        [SerializeField] private int seed = 12345;
        [Tooltip("Pick a fresh random seed on Start instead of using the field above.")]
        [SerializeField] private bool randomizeSeed = false;

        [Header("Save / Load")]
        [Tooltip("DEBUG ONLY: force a brand-new terrain on Start, ignoring (and overwriting) any existing save.")]
        [SerializeField] private bool createNewTerrain = false;

        private bool _generated;
        private Dictionary<Vector2Int, Chunk> terrainChunks = new Dictionary<Vector2Int, Chunk>();
        private List<Chunk> _activeChunks = new List<Chunk>();

        // Populated only when loading from a save; maps a chunk's grid coords to its persisted contents.
        private Dictionary<Vector2Int, ChunkSaveData> _loadedChunkData;
        // Name -> prefab lookup, built lazily from the four prefab arrays, used to resolve saved objects.
        private Dictionary<string, GameObject> _prefabsByName;

        // The seed that actually produced the current world (after any randomization).
        public int Seed => seed;

        // Returns the shared save manager, or null (with a warning) if this scene isn't wired for saving.
        // Never throws, so terrain always generates even when the save system is missing.
        private SaveLoadManager SaveOrNull()
        {
            if (SingletonPoint.Instance == null)
            {
                Debug.LogWarning("[Terrain] No SingletonPoint in scene — terrain will generate but won't save/load.");
                return null;
            }
            if (SingletonPoint.Instance.SaveLoad == null)
            {
                Debug.LogWarning("[Terrain] SingletonPoint.SaveLoad is not assigned — terrain will generate but won't save/load.");
                return null;
            }
            return SingletonPoint.Instance.SaveLoad;
        }

        public void Start()
        {
            SaveLoadManager save = SaveOrNull();

            // General logic: if a saved terrain exists, load it. The debug flag forces a fresh one instead.
            if (!createNewTerrain && save != null && save.HasSaveFile && save.Current.hasTerrain)
            {
                Load(save.Current.terrain);
                return;
            }

            Generate(NextSeed());
            SaveToFile();
        }

        // Resolves the seed to generate with: a fresh random one, or the inspector value.
        private int NextSeed()
        {
            return randomizeSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;
        }

        // Public API: build the world from a given seed. Safe to call once; ignored if already generated.
        // Set generateOnStart = false and call this from a GameManager / save loader to inject a seed.
        public void Generate(int worldSeed)
        {
            if (_generated) return;
            _generated = true;

            seed = worldSeed;
            noiseSettings.seed = worldSeed;

            ActivateInitialChunks();
        }

        // Activates the 3x3 ring of chunks around the origin. Chunks not present in a loaded save
        // fall back to deterministic procedural fill, so this works for both new and loaded worlds.
        private void ActivateInitialChunks()
        {
            for (int row = -1; row <= 1; row++)
            {
                for (int col = -1; col <= 1; col++)
                {
                    ActivateChunk(GenerateOrGetChunkAt(row, col));
                }
            }
        }

        private void ActivateChunk(Chunk chunk)
        {
            if (_activeChunks.Contains(chunk)) return;
            chunk.Fill(SingletonPoint.Instance.ObjectPool);
            chunk.gameObject.SetActive(true);
            _activeChunks.Add(chunk);
        }

        private void DeactivateChunk(Chunk chunk)
        {
            chunk.gameObject.SetActive(false);
            chunk.Unfill(SingletonPoint.Instance.ObjectPool);
            _activeChunks.Remove(chunk);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 2f);
            Gizmos.color = Color.red;
            foreach (Chunk terrainChunk in terrainChunks.Values.Where(terrainChunk => terrainChunk != null))
            {
                Gizmos.DrawWireSphere(terrainChunk.Center() + new Vector3(0, 2, 0), 0.2f);
            }
        }

        private Chunk GenerateOrGetChunkAt(int row, int col)
        {
            Vector2Int key = new Vector2Int(row, col);
            if (terrainChunks.TryGetValue(key, out Chunk at)) return at; // Already generated

            Vector3 offset = new Vector3(col * width, 0, row * height);
            GameObject chunkObj = new GameObject($"TerrainFace_{row}_{col}");
            chunkObj.layer = LayerMask.NameToLayer(terrainLayerName);
            chunkObj.transform.parent = this.transform;

            Chunk terrainChunk = chunkObj.AddComponent<Chunk>();
            terrainChunk.Setup(noiseSettings, width, height, Vector3.up, resolution);
            terrainChunk.SetPosition(this.transform.position + offset);
            terrainChunk.CreateMesh();
            terrainChunk.CreatePlayerZone(50f, () =>
            {
                UpdateFacesAroundPlayer(row, col);
            });
            terrainChunks[key] = terrainChunk;

            if (_loadedChunkData != null && _loadedChunkData.TryGetValue(key, out ChunkSaveData saved))
            {
                // This chunk was persisted — restore its exact contents instead of re-scattering.
                RestoreChunk(terrainChunk, saved);
            }
            else
            {
                // Derive a stable per-chunk seed so scatter is identical every run for a given world seed.
                FillNewChunk(terrainChunk, new SeededRandom(SeedUtility.Combine(seed, row, col)));
            }

            chunkObj.SetActive(false);
            return terrainChunk;
        }

        private void UpdateFacesAroundPlayer(int row, int col)
        {
            List<Chunk> chunksToDeactivate = new List<Chunk>(_activeChunks);
            List<Chunk> chunksToActivate = new List<Chunk>();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    int checkRow = row + z;
                    int checkCol = col + x;
                    Chunk chunk = GenerateOrGetChunkAt(checkRow, checkCol);
                    if (Mathf.Abs(x) <= renderDistance && Mathf.Abs(z) <= renderDistance)
                    {
                        if (chunksToDeactivate.Contains(chunk))
                        {
                            chunksToDeactivate.Remove(chunk);
                        }
                        else
                        {
                            chunksToActivate.Add(chunk);
                        }
                    }
                    else
                    {
                        if (chunksToDeactivate.Contains(chunk)) continue;
                        chunksToDeactivate.Add(chunk);
                    }
                }
            }

            chunksToActivate.ForEach(ActivateChunk);
            chunksToDeactivate.ForEach(DeactivateChunk);

            // Persist the world every time the player crosses into a new chunk.
            SaveToFile();
        }

        private void FillNewChunk(Chunk chunk, SeededRandom rng)
        {
            int attempts = rng.Range(10, 25);

            for (int i = 0; i < attempts; i++)
            {
                float r = rng.Range(0f, treeChance + rockChance + bushChance + smallChance);
                GameObject[] list = null;

                if (r < treeChance) list = treePrefabs;
                else if (r < treeChance + rockChance) list = rockPrefabs;
                else if (r < treeChance + rockChance + bushChance) list = bushPrefabs;
                else list = smallNaturePrefabs;

                if (list == null || list.Length == 0) continue;

                int index = rng.Range(0, list.Length);

                // Random local X/Z inside the chunk extents
                float localX = rng.Range(-chunk.Width * 0.5f, chunk.Width * 0.5f);
                float localZ = rng.Range(-chunk.Height * 0.5f, chunk.Height * 0.5f);

                MeshCollider mc = chunk.MeshCollider;
                if (mc == null || mc.sharedMesh == null) continue;

                // Compute a safe world-space start Y above the mesh top
                float startY = mc.bounds.max.y + 1.0f; // margin above the top
                Vector3 worldOrigin = new Vector3(chunk.Position.x + localX, startY, chunk.Position.z + localZ);

                // Cast down along the chunk's up axis (handles rotated chunks)
                Vector3 dir = -chunk.transform.up;
                Ray ray = new Ray(worldOrigin, dir);

                if (mc.Raycast(ray, out RaycastHit hit, 200f))
                {
                    Vector3 localPos = chunk.transform.InverseTransformPoint(hit.point);
                    chunk.AddObjectData(new ObjectData
                    {
                        prefab = list[index],
                        localPosition = localPos,
                        localRotation = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f)
                    });
                }
            }
        }

        public TerrainSaveData BuildSaveData()
        {
            TerrainSaveData data = new TerrainSaveData
            {
                seed = seed,
                width = width,
                height = height,
                resolution = resolution,
                noiseSettings = noiseSettings
            };

            foreach (KeyValuePair<Vector2Int, Chunk> kvp in terrainChunks)
            {
                Chunk chunk = kvp.Value;
                if (chunk == null) continue;

                ChunkSaveData chunkData = new ChunkSaveData { row = kvp.Key.x, col = kvp.Key.y };
                foreach (ObjectData od in chunk.objects)
                {
                    chunkData.objects.Add(new SerializableObjectData
                    {
                        prefabName = od.prefab != null ? od.prefab.name : string.Empty,
                        localPosition = od.localPosition,
                        localRotation = od.localRotation
                    });
                }

                data.chunks.Add(chunkData);
            }

            return data;
        }

        // Writes the terrain section into the shared save and persists the file. No-op if unwired.
        [ContextMenu("Save Terrain")]
        public void SaveToFile()
        {
            SaveLoadManager save = SaveOrNull();
            if (save == null) return;

            save.Current.terrain = BuildSaveData();
            save.Current.hasTerrain = true;
            save.Save();
        }

        public void Load(TerrainSaveData data)
        {
            if (_generated || data == null) return;
            _generated = true;

            seed = data.seed;
            width = data.width;
            height = data.height;
            resolution = data.resolution;
            noiseSettings = data.noiseSettings;
            noiseSettings.seed = data.seed;

            _loadedChunkData = new Dictionary<Vector2Int, ChunkSaveData>();
            foreach (ChunkSaveData chunkData in data.chunks)
            {
                _loadedChunkData[new Vector2Int(chunkData.row, chunkData.col)] = chunkData;
            }

            // Recreate every persisted chunk so the saved world exists in full...
            foreach (ChunkSaveData chunkData in data.chunks)
            {
                GenerateOrGetChunkAt(chunkData.row, chunkData.col);
            }

            // ...then activate the ring around the origin.
            ActivateInitialChunks();
        }

        // Restores a chunk's contents from its saved data, resolving prefab names via the registry.
        private void RestoreChunk(Chunk chunk, ChunkSaveData data)
        {
            EnsurePrefabRegistry();

            foreach (SerializableObjectData so in data.objects)
            {
                if (string.IsNullOrEmpty(so.prefabName)) continue;
                if (!_prefabsByName.TryGetValue(so.prefabName, out GameObject prefab) || prefab == null)
                {
                    Debug.LogWarning($"[Terrain] Saved prefab '{so.prefabName}' not found in prefab arrays; skipping.");
                    continue;
                }

                chunk.AddObjectData(new ObjectData
                {
                    prefab = prefab,
                    localPosition = so.localPosition,
                    localRotation = so.localRotation
                });
            }
        }

        // Builds a name -> prefab lookup from the four prefab arrays (once).
        private void EnsurePrefabRegistry()
        {
            if (_prefabsByName != null) return;
            _prefabsByName = new Dictionary<string, GameObject>();
            RegisterPrefabs(treePrefabs);
            RegisterPrefabs(rockPrefabs);
            RegisterPrefabs(bushPrefabs);
            RegisterPrefabs(smallNaturePrefabs);
        }

        private void RegisterPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null) return;
            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null) _prefabsByName[prefab.name] = prefab;
            }
        }

        // Deletes the WHOLE combined save file (terrain + inventory + anything else). Debug helper.
        [ContextMenu("Delete Game Save File (All)")]
        private void DeleteSaveFile()
        {
            SaveOrNull()?.Delete();
            Debug.Log("[Terrain] Deleted world save for active slot.");
        }
    }
}