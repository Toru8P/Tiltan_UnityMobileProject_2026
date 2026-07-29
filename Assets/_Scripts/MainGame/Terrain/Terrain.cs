using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.MainGame.Pool;
using _Scripts.MainGame.SaveLoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.MainGame.Terrain
{
    // Runs early so the world is generated/loaded before systems that depend on it (e.g. the player
    // restoring its saved position, which needs the ground under it to be streamed in).
    [DefaultExecutionOrder(-100)]
    public class Terrain : MonoBehaviour
    {
        [Header("Noise Settings")] [SerializeField]
        private NoiseSettings noiseSettings = NoiseSettings.Default;

        [Serializable]
        public class OreSpawnEntry
        {
            public string oreName;
            public GameObject prefab;
            [Min(0)] public int attemptsPerChunk = 1;
            [Range(0f, 1f)] public float spawnChance = 0.25f;
        }
        [Header("Generation Prefabs")]
        public GameObject[] treePrefabs;
        public GameObject[] rockPrefabs;
        public GameObject[] bushPrefabs;
        public GameObject[] smallNaturePrefabs;
        [SerializeField] private string terrainLayerName = "TerrainGround";

        [Header("Probabilities")]
        [Range(0f, 1f)] public float treeChance = 0.25f;
        [Range(0f, 1f)] public float rockChance = 0.2f;
        [Range(0f, 1f)] public float bushChance = 0.1f;
        [Range(0f, 1f)] public float smallChance = 0.45f;

        [Header("Ore Generation")]
        [Tooltip("Each entry performs its own number of placement rolls per chunk. Spawn chance is evaluated per roll.")]
        public OreSpawnEntry[] oreSpawnEntries;
        [Min(0f)] public float oreMinimumSpacing = 3f;
        [Range(0f, 1f)] public float oreDensityMultiplier = 0.35f;


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

        // Fired after the player crosses into a new chunk (and the world is saved), so other
        // systems can persist their own state on the same cadence.
        public event Action PlayerChangedChunk;

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

            // A new-game slot has a world file carrying the chosen seed (but no terrain yet) — use it.
            // Otherwise (debug reset, or no save system) fall back to the inspector/random seed.
            int worldSeed = (!createNewTerrain && save != null && save.HasSaveFile)
                ? save.Current.terrain.seed
                : NextSeed();

            Generate(worldSeed);
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
                // This chunk was persisted — restore its exact contents, then backfill ores once if needed.
                RestoreChunk(terrainChunk, saved);
                EnsureOresGenerated(terrainChunk, new SeededRandom(SeedUtility.Combine(seed, row, col) ^ 0x4F5245));
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
            UpdateChunksAround(row, col);

            // Let other systems (e.g. player position) stage their save section first...
            PlayerChangedChunk?.Invoke();

            // ...then persist the whole world once, every time the player crosses into a new chunk.
            SaveToFile();
        }

        // Activates chunks within render distance of (row, col) and deactivates the rest. No saving.
        private void UpdateChunksAround(int row, int col)
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
        }

        // Re-streams chunks around a world position (e.g. a player restored far from origin on load),
        // so the ground under them is active. Does not save.
        public void ActivateAroundWorld(Vector3 worldPos)
        {
            if (TryGetChunkCoords(worldPos, out int row, out int col))
            {
                UpdateChunksAround(row, col);
            }
        }

        // Converts a world-space position into the (row, col) of the chunk whose footprint contains it.
        // Chunks are CENTERED at transform.position + (col*width, 0, row*height) and span width x height,
        // so the containing cell is found by shifting the local position half a chunk and flooring —
        // a plain RoundToInt misclassifies points sitting on a chunk border. This is the exact inverse
        // of the placement done in GenerateOrGetChunkAt.
        private bool TryGetChunkCoords(Vector3 worldPos, out int row, out int col)
        {
            row = 0;
            col = 0;
            if (width <= 0 || height <= 0) return false;

            Vector3 local = worldPos - transform.position;
            col = Mathf.FloorToInt((local.x + width * 0.5f) / width);
            row = Mathf.FloorToInt((local.z + height * 0.5f) / height);
            return true;
        }

        private void FillNewChunk(Chunk chunk, SeededRandom rng)
        {
            int attempts = rng.Range(10, 25);

            for (int i = 0; i < attempts; i++)
            {
                float r = rng.Range(0f, treeChance + rockChance + bushChance + smallChance);
                GameObject[] list;

                if (r < treeChance) list = treePrefabs;
                else if (r < treeChance + rockChance) list = rockPrefabs;
                else if (r < treeChance + rockChance + bushChance) list = bushPrefabs;
                else list = smallNaturePrefabs;

                if (list == null || list.Length == 0) continue;
                TryAddScatteredObject(chunk, list[rng.Range(0, list.Length)], rng);
            }

            if (oreSpawnEntries == null) return;

            EnsureOresGenerated(chunk, rng);
        }

        private void EnsureOresGenerated(Chunk chunk, SeededRandom rng)
        {
            if (oreSpawnEntries == null || oreSpawnEntries.Length == 0) return;
            if (chunk.objects.Exists(objectData => IsOrePrefab(objectData.prefab))) return;

            foreach (OreSpawnEntry ore in oreSpawnEntries)
            {
                if (ore == null || ore.prefab == null || ore.attemptsPerChunk <= 0)
                    continue;

                for (int i = 0; i < ore.attemptsPerChunk; i++)
                {
                    if (rng.Range(0f, 1f) <= ore.spawnChance * oreDensityMultiplier)
                        TryAddOreObject(chunk, ore.prefab, rng);
                }
            }
        }

        private bool IsOrePrefab(GameObject prefab)
        {
            if (prefab == null || oreSpawnEntries == null) return false;

            foreach (OreSpawnEntry ore in oreSpawnEntries)
            {
                if (ore != null && ore.prefab == prefab)
                    return true;
            }

            return false;
        }

        private bool TryAddOreObject(Chunk chunk, GameObject prefab, SeededRandom rng)
        {
            if (chunk == null || prefab == null) return false;

            MeshCollider meshCollider = chunk.MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null) return false;

            float localX = rng.Range(-chunk.Width * 0.5f, chunk.Width * 0.5f);
            float localZ = rng.Range(-chunk.Height * 0.5f, chunk.Height * 0.5f);
            Vector3 localPosition = new Vector3(localX, 0f, localZ);
            float minimumSpacingSqr = oreMinimumSpacing * oreMinimumSpacing;

            foreach (ObjectData objectData in chunk.objects)
            {
                if (!IsOrePrefab(objectData.prefab)) continue;

                Vector3 existingPosition = objectData.localPosition;
                existingPosition.y = 0f;
                if ((existingPosition - localPosition).sqrMagnitude < minimumSpacingSqr)
                    return false;
            }

            float startY = meshCollider.bounds.max.y + 1.0f;
            Vector3 worldOrigin = new Vector3(chunk.Position.x + localX, startY, chunk.Position.z + localZ);
            Ray ray = new Ray(worldOrigin, -chunk.transform.up);
            if (!meshCollider.Raycast(ray, out RaycastHit hit, 200f)) return false;

            chunk.AddObjectData(new ObjectData
            {
                prefab = prefab,
                localPosition = chunk.transform.InverseTransformPoint(hit.point),
                localRotation = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f)
            });
            return true;
        }

        private bool TryAddScatteredObject(Chunk chunk, GameObject prefab, SeededRandom rng)
        {
            if (chunk == null || prefab == null) return false;

            MeshCollider meshCollider = chunk.MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null) return false;

            float localX = rng.Range(-chunk.Width * 0.5f, chunk.Width * 0.5f);
            float localZ = rng.Range(-chunk.Height * 0.5f, chunk.Height * 0.5f);
            float startY = meshCollider.bounds.max.y + 1.0f;
            Vector3 worldOrigin = new Vector3(chunk.Position.x + localX, startY, chunk.Position.z + localZ);
            Ray ray = new Ray(worldOrigin, -chunk.transform.up);

            if (!meshCollider.Raycast(ray, out RaycastHit hit, 200f)) return false;

            chunk.AddObjectData(new ObjectData
            {
                prefab = prefab,
                localPosition = chunk.transform.InverseTransformPoint(hit.point),
                localRotation = Quaternion.Euler(0f, rng.Range(0f, 360f), 0f)
            });
            return true;
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
            save.MarkDirty(); // staged in memory; SaveLoadManager flushes to disk on its interval
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

            // ...then activate the ring around the player's saved position (not the origin), so the
            // ground is streamed in exactly where the restored player will land instead of leaving
            // them over inactive chunks and falling through. Falls back to the origin ring if no
            // player position was persisted.
            SaveLoadManager save = SaveOrNull();
            if (save != null && save.Current.hasPlayer)
            {
                ActivateAroundWorld(save.Current.player.position);
            }
            else
            {
                ActivateInitialChunks();
            }

            SaveToFile();
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
            if (oreSpawnEntries != null)
            {
                foreach (OreSpawnEntry ore in oreSpawnEntries)
                {
                    if (ore != null && ore.prefab != null)
                        _prefabsByName[ore.prefab.name] = ore.prefab;
                }
            }
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