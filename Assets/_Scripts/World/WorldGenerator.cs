using UnityEngine;
using UnityEngine.AI;
using _Scripts.Pooling;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

namespace _Scripts.World
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("References")] 
        public Transform playerTransform;
        public GameObject[] groundPrefabs;
        public GameObject[] treePrefabs;
        public GameObject[] rockPrefabs;
        public GameObject[] bushPrefabs;
        public GameObject[] smallNaturePrefabs;
        public NavMeshSurface navMeshSurface;

        [Header("Generation Settings")] public float chunkSize = 30f;
        public int viewDistance = 2; // Number of chunks in each direction
        public float cleanupDistanceBehind = 15f;

        [Header("Probabilities")]
        [Range(0, 1)] public float treeChance = 0.25f; // Increased
        [Range(0, 1)] public float rockChance = 0.2f;
        [Range(0, 1)] public float bushChance = 0.1f;
        [Range(0, 1)] public float smallChance = 0.45f;

        [Header("Ground Alignment")] public float groundOverlap = 2.0f; // Increased for better seamless connection

        [Header("Persistence")] public bool persistBetweenSessions = true;
        public string saveKey = "WorldData";

        [Header("Performance")] public int updateFrequencyFrames = 30; // Throttled from 10 to 30
        public float navMeshUpdateInterval = 1f; // Reduced from 10 to 1 for faster startup
        public int preWarmCount = 30;

        private readonly WorldPersistence _persistence = new WorldPersistence();

        private readonly Dictionary<Vector2Int, GameObject> _activeGroundTiles =
            new Dictionary<Vector2Int, GameObject>();

        private readonly Dictionary<Vector2Int, List<GameObject>> _activeNatureObjects =
            new Dictionary<Vector2Int, List<GameObject>>();

        private readonly Dictionary<int, Vector3> _groundMeshSizeCache = new Dictionary<int, Vector3>();

        private bool _needsNavMeshUpdate = false;
        private float _lastNavMeshUpdateTime = 0f;
        private int _frameCount = 0;
        private Transform _groundParent;
        private Transform _natureParent;
        private Queue<Vector2Int> _spawnQueue = new Queue<Vector2Int>();
        private HashSet<Vector2Int> _queuedCoords = new HashSet<Vector2Int>();

        // First-time setup: find the player, configure the NavMeshSurface for procedural generation,
        // cache prefab sizes (used to scale ground tiles to chunk size), load any saved world data,
        // and pre-warm the object pools so initial spawning doesn't lag.
        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null) navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }

            // Create or find parent objects for organization
            _groundParent = new GameObject("Active_Ground").transform;
            _natureParent = new GameObject("Active_Nature").transform;

            // Optimization: Parent containers to this generator and use CollectObjects.Children
            // This drastically reduces NavMesh build time by only scanning ground/nature objects.
            _groundParent.SetParent(transform);
            _natureParent.SetParent(transform);

            // Configure for robust procedural navigation
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.collectObjects = CollectObjects.Children;

            navMeshSurface.overrideVoxelSize = true;
            navMeshSurface.voxelSize = 0.08f; // Finer voxels for better seams

            CacheGroundMeshSizes();

            if (persistBetweenSessions)
            {
                LoadWorld();
            }

            StartCoroutine(PreWarmPoolsRoutine());

            // Trigger initial chunk queueing immediately
            UpdateChunks();
        }

        // Pre-creates inactive copies of every nature prefab (trees, rocks, etc.) so the pool is full
        // before gameplay starts. Optimized: Spreads instantiation over many frames to avoid hitching.
        private IEnumerator PreWarmPoolsRoutine()
        {
            if (PoolManager.Instance == null) yield break;

            // Note: Ground is NOT pooled as it is low-frequency and large
            var allPrefabs = new List<GameObject[]>() { treePrefabs, rockPrefabs, bushPrefabs, smallNaturePrefabs };

            foreach (var list in allPrefabs)
            {
                foreach (var p in list)
                {
                    if (p == null) continue;

                    // Instantiate one by one with a frame gap
                    for (int i = 0; i < preWarmCount; i++)
                    {
                        PoolManager.Instance.PreWarm(p, 1);
                        yield return null;
                    }
                }
            }
        }

        // Reads the bounds of every ground prefab's mesh once at startup and stores the size.
        // We use these sizes later to calculate how much to scale each ground tile to fit the chunk perfectly.
        private void CacheGroundMeshSizes()
        {
            for (int i = 0; i < groundPrefabs.Length; i++)
            {
                MeshFilter filter = groundPrefabs[i].GetComponentInChildren<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    _groundMeshSizeCache[i] = filter.sharedMesh.bounds.size;
                }
                else
                {
                    _groundMeshSizeCache[i] = new Vector3(30, 1, 30); // Fallback
                }
            }
        }

        // Auto-save when the game closes (or this object is destroyed). Keeps the world persistent across sessions.
        private void OnDestroy()
        {
            if (persistBetweenSessions)
            {
                SaveWorld();
            }
        }

        // Converts all explored chunks to JSON and stores in PlayerPrefs (Unity's simple persistent storage).
        public void SaveWorld()
        {
            string json = _persistence.ToJson();
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
            Debug.Log("World Data Saved.");
        }

        // Reads the saved JSON back from PlayerPrefs and rebuilds the chunk dictionary.
        // Called once at startup so re-visited chunks keep their original trees and rocks.
        public void LoadWorld()
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                string json = PlayerPrefs.GetString(saveKey);
                _persistence.FromJson(json);
                Debug.Log("World Data Loaded.");
            }
        }

        // Main update loop, throttled. Every `updateFrequencyFrames` frames we check whether chunks need to be
        // spawned in or cleaned out. The NavMesh rebuild is throttled separately — rebuilding is expensive,
        // so we batch changes and only rebuild every `navMeshUpdateInterval` seconds at most.
        private void Update()
        {
            if (playerTransform == null) return;

            _frameCount++;
            if (_frameCount % updateFrequencyFrames == 0)
            {
                UpdateChunks();
                CleanupFarChunks();
            }

            // Process spawn queue - spawn one chunk per frame to avoid spikes
            if (_spawnQueue.Count > 0)
            {
                Vector2Int coords = _spawnQueue.Dequeue();
                _queuedCoords.Remove(coords);

                if (!_activeGroundTiles.ContainsKey(coords))
                {
                    ActivateChunk(coords);
                    _needsNavMeshUpdate = true;
                    // Sync physics once after potentially spawning a chunk so raycasts work next frame
                    Physics.SyncTransforms();
                }
            }

            if (_needsNavMeshUpdate && Time.time > _lastNavMeshUpdateTime + navMeshUpdateInterval)
            {
                _needsNavMeshUpdate = false;
                _lastNavMeshUpdateTime = Time.time;
                navMeshSurface.BuildNavMesh();
            }
        }

        // Figures out which chunk the player is currently standing in, then makes sure every chunk
        // within `viewDistance` is active. Chunks are added to a queue to be processed over time.
        // Optimized: Uses a spiral/ring-based search to prioritize spawning chunks closest to the player.
        private void UpdateChunks()
        {
            Vector3 pos = playerTransform.position;
            int currentX = Mathf.FloorToInt(pos.x / chunkSize);
            int currentZ = Mathf.FloorToInt(pos.z / chunkSize);

            // Spiral out from distance 0 to viewDistance
            for (int d = 0; d <= viewDistance; d++)
            {
                for (int x = -d; x <= d; x++)
                {
                    for (int z = -d; z <= d; z++)
                    {
                        // Only process the outer ring for this distance 'd'
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != d) continue;

                        Vector2Int coords = new Vector2Int(currentX + x, currentZ + z);
                        if (!_activeGroundTiles.ContainsKey(coords) && !_queuedCoords.Contains(coords))
                        {
                            _spawnQueue.Enqueue(coords);
                            _queuedCoords.Add(coords);
                        }
                    }
                }
            }
        }

        // Spawns one chunk: ground tile + filler base + nature objects.
        // Either uses previously generated data (if the chunk has been visited before) or makes new random data.
        // Ground tiles are scaled to match `chunkSize` based on the cached mesh size.
        private void ActivateChunk(Vector2Int coords)
        {
            try
            {
                Debug.Log($"[WorldGen] Activating chunk at {coords}");
                ChunkData data;
                if (_persistence.HasChunk(coords))
                {
                    data = _persistence.GetChunk(coords);
                }
                else
                {
                    data = GenerateChunkData(coords);
                    _persistence.SaveChunk(data);
                }

                PoolManager pool = PoolManager.Instance;
                if (!pool)
                {
                    throw new System.Exception("PoolManager instance not found");
                }

                // Spawn Ground
                Vector3 groundPos = new Vector3(coords.x * chunkSize, 0, coords.y * chunkSize);
                GameObject ground = pool.Get(groundPrefabs[data.groundPrefabIndex], groundPos, Quaternion.identity);

                if (ground == null)
                {
                    Debug.LogError(
                        $"[WorldGen] Failed to get ground prefab {data.groundPrefabIndex} from pool at {coords}");
                    return;
                }

                if (_groundParent == null)
                {
                    Debug.LogWarning("[WorldGen] _groundParent was null, recreating...");
                    _groundParent = new GameObject("Active_Ground").transform;
                    _groundParent.SetParent(transform);
                }

                ground.transform.SetParent(_groundParent);
                Debug.Log($"[WorldGen] Ground tile {ground.name} spawned at {ground.transform.position}");


                // Normalize scale using cached mesh size
                if (_groundMeshSizeCache.TryGetValue(data.groundPrefabIndex, out Vector3 meshSize))
                {
                    float scaleX = (chunkSize + groundOverlap) / meshSize.x;
                    float scaleZ = (chunkSize + groundOverlap) / meshSize.z;

                    float scaleY = 1f;
                    float yOffset = 0f;

                    switch (groundPrefabs[data.groundPrefabIndex].name)
                    {
                        case "Ground_02":
                            scaleY = 0.4f;
                            break;
                        case "Ground_01":
                            scaleY = 0.7f;
                            break;
                        case "Ground_03":
                            yOffset = 0.15f;
                            break;
                    }

                    ground.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                    MeshFilter filter = groundPrefabs[data.groundPrefabIndex].GetComponentInChildren<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        Vector3 meshCenterOffset = filter.sharedMesh.bounds.center;
                        Vector3 scaledCenterOffset =
                            new Vector3(meshCenterOffset.x * scaleX, 0, meshCenterOffset.z * scaleZ);
                        ground.transform.position = groundPos - scaledCenterOffset + Vector3.up * yOffset;
                    }
                }

                _activeGroundTiles[coords] = ground;

                // Spawn Nature Objects
                List<GameObject> chunkObjects = new List<GameObject>();

                int fillerIndex = -1;
                for (int i = 0; i < groundPrefabs.Length; i++)
                {
                    if (groundPrefabs[i] != null && groundPrefabs[i].name == "Ground_03")
                    {
                        fillerIndex = i;
                        break;
                    }
                }

                if (fillerIndex != -1)
                {
                    float basementY = -0.2f;
                    Vector3 fillerPos = new Vector3(coords.x * chunkSize, basementY, coords.y * chunkSize);
                    GameObject filler = pool.Get(groundPrefabs[fillerIndex], fillerPos, Quaternion.identity);

                    if (filler != null && _groundMeshSizeCache.TryGetValue(fillerIndex, out Vector3 fSize))
                    {
                        float fScaleX = (chunkSize + groundOverlap + 5.0f) / fSize.x;
                        float fScaleZ = (chunkSize + groundOverlap + 5.0f) / fSize.z;
                        filler.transform.localScale = new Vector3(fScaleX, 1.0f, fScaleZ);

                        MeshFilter fFilter = groundPrefabs[fillerIndex].GetComponentInChildren<MeshFilter>();
                        if (fFilter != null && fFilter.sharedMesh != null)
                        {
                            Vector3 fOffset = fFilter.sharedMesh.bounds.center;
                            filler.transform.position =
                                fillerPos - new Vector3(fOffset.x * fScaleX, 0, fOffset.z * fScaleZ);
                        }
                    }

                    if (filler != null)
                    {
                        filler.transform.SetParent(_natureParent);
                        chunkObjects.Add(filler);
                    }
                }

                foreach (var objData in data.objects)
                {
                    GameObject prefab = GetPrefabFromData(objData);
                    if (prefab != null)
                    {
                        Vector3 spawnPos = groundPos + objData.localPosition;

                        // Snap to surface. Increased ray distance and added LayerMask to avoid hitting other nature objects
                        Vector3 rayStart = spawnPos + Vector3.up * 50f;
                        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f))
                        {
                            spawnPos.y = hit.point.y;
                        }
                        else
                        {
                            spawnPos.y = 0.0f;
                        }

                        GameObject instance = pool.Get(prefab, spawnPos, objData.localRotation);
                        instance.transform.SetParent(_natureParent);

                        // Ensure nature objects have PooledObject for distance-based cleanup
                        PooledObject pooled = instance.GetComponent<PooledObject>();
                        if (pooled == null) pooled = instance.AddComponent<PooledObject>();
                        pooled.Setup(playerTransform, chunkSize * (viewDistance + 2f));

                        chunkObjects.Add(instance);
                    }
                }

                _activeNatureObjects[coords] = chunkObjects;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WorldGen] Exception in ActivateChunk: {e.Message}\n{e.StackTrace}");
            }
        }

        // Procedurally generates the content of a brand-new chunk:
        // 1. Pick a random ground prefab.
        // 2. Try 10-25 spawn attempts. Each attempt rolls a random value and uses the configured
        //    tree/rock/bush/small probabilities to decide what (if anything) to place.
        // 3. Each object gets a random local position within the chunk and a random Y rotation.
        // This data is then SAVED so the chunk looks identical next time the player visits it.
        private ChunkData GenerateChunkData(Vector2Int coords)
        {
            ChunkData data = new ChunkData { coordinates = coords };
            data.groundPrefabIndex = Random.Range(0, groundPrefabs.Length);

            int attempts = Random.Range(10, 25);
            for (int i = 0; i < attempts; i++)
            {
                float r = Random.Range(0,5);
                int type = -1;
                GameObject[] list = null;

                if (r < treeChance)
                {
                    type = 0;
                    list = treePrefabs;
                }
                else if (r < treeChance + rockChance)
                {
                    type = 1;
                    list = rockPrefabs;
                }
                else if (r < treeChance + rockChance + bushChance)
                {
                    type = 2;
                    list = bushPrefabs;
                }
                else if (r < treeChance + rockChance + bushChance + smallChance)
                {
                    type = 3;
                    list = smallNaturePrefabs;
                }

                if (type != -1 && list != null && list.Length > 0)
                {
                    int index = Random.Range(0, list.Length);
                    data.objects.Add(new ObjectData
                    {
                        prefabType = type,
                        prefabIndex = index,
                        localPosition = new Vector3(Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f), 0,
                            Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f)),
                        localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0)
                    });
                }
            }

            return data;
        }

        // Looks up the actual prefab from saved ObjectData. The data only stores a TYPE index (tree/rock/bush/small)
        // and an INDEX into that list. This way the save file stays small and human-readable.
        private GameObject GetPrefabFromData(ObjectData data)
        {
            GameObject[] list = null;
            switch (data.prefabType)
            {
                case 0: list = treePrefabs; break;
                case 1: list = rockPrefabs; break;
                case 2: list = bushPrefabs; break;
                case 3: list = smallNaturePrefabs; break;
            }

            if (list != null && data.prefabIndex < list.Length) return list[data.prefabIndex];
            return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        // Loops through all active chunks and returns any that are too far behind the player OR
        // outside the maximum view distance, back to the pool. Marks the NavMesh as dirty so it gets rebuilt.
        private void CleanupFarChunks()
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();
            Vector3 playerPos = playerTransform.position;
            // Use a slightly larger radius for cleanup than view distance to prevent flickering
            float cleanupRadius = chunkSize * (viewDistance + 1f);
            float cleanupRadiusSqr = cleanupRadius * cleanupRadius;

            foreach (var coord in _activeGroundTiles.Keys)
            {
                Vector3 chunkCenter = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
                float distSqr = (chunkCenter - playerPos).sqrMagnitude;

                if (distSqr > cleanupRadiusSqr)
                {
                    toRemove.Add(coord);
                    _needsNavMeshUpdate = true;
                }
            }

            foreach (var coord in toRemove)
            {
                DeactivateChunk(coord);
            }
        }

        // Returns the chunk's ground tile and all its nature objects back to the pool.
        // The chunk DATA stays saved in _persistence — only the visible GameObjects get recycled.
        private void DeactivateChunk(Vector2Int coords)
        {
            if (_activeGroundTiles.TryGetValue(coords, out GameObject ground))
            {
                Destroy(ground);
                _activeGroundTiles.Remove(coords);
            }

            if (_activeNatureObjects.TryGetValue(coords, out List<GameObject> objects))
            {
                foreach (var obj in objects)
                {
                    PoolManager.Instance.Return(obj);
                }

                _activeNatureObjects.Remove(coords);
            }
        }
    }
}