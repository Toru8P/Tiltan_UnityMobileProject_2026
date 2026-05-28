using UnityEngine;
using UnityEngine.AI;
using _Scripts.Pooling;
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

        [Header("Generation Settings")]
        public float chunkSize = 30f;
        public int viewDistance = 2; // Number of chunks in each direction
        public float cleanupDistanceBehind = 15f; 
        
        [Header("Probabilities")]
        [Range(0, 1)] public float treeChance = 0.05f;
        [Range(0, 1)] public float rockChance = 0.15f;
        [Range(0, 1)] public float bushChance = 0.2f;
        [Range(0, 1)] public float smallChance = 0.4f;

        [Header("Ground Alignment")]
        public float groundOverlap = 1.0f; // Extra scale to ensure seamless connection

        [Header("Persistence")]
        public bool persistBetweenSessions = true;
        public string saveKey = "WorldData";

        [Header("Performance")]
        public int updateFrequencyFrames = 10;
        public float navMeshUpdateInterval = 2f;
        public int preWarmCount = 30;

        private readonly WorldPersistence _persistence = new WorldPersistence();
        private readonly Dictionary<Vector2Int, GameObject> _activeGroundTiles = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, List<GameObject>> _activeNatureObjects = new Dictionary<Vector2Int, List<GameObject>>();
        private readonly Dictionary<int, Vector3> _groundMeshSizeCache = new Dictionary<int, Vector3>();
        
        private bool _needsNavMeshUpdate = false;
        private float _lastNavMeshUpdateTime = 0f;
        private int _frameCount = 0;

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

            // Configure for robust procedural navigation
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.collectObjects = CollectObjects.All;
            // Higher step height and slope for procedural seams
            navMeshSurface.overrideVoxelSize = true;
            navMeshSurface.voxelSize = 0.1f;
            
            CacheGroundMeshSizes();

            if (persistBetweenSessions)
            {
                LoadWorld();
            }

            PreWarmPools();
        }

        private void PreWarmPools()
        {
            if (PoolManager.Instance == null) return;
            
            foreach (var p in groundPrefabs) PoolManager.Instance.PreWarm(p, preWarmCount);
            foreach (var p in treePrefabs) PoolManager.Instance.PreWarm(p, preWarmCount);
            foreach (var p in rockPrefabs) PoolManager.Instance.PreWarm(p, preWarmCount);
            foreach (var p in bushPrefabs) PoolManager.Instance.PreWarm(p, preWarmCount);
            foreach (var p in smallNaturePrefabs) PoolManager.Instance.PreWarm(p, preWarmCount);
        }

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

        private void OnDestroy()
        {
            if (persistBetweenSessions)
            {
                SaveWorld();
            }
        }

        public void SaveWorld()
        {
            string json = _persistence.ToJson();
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
            Debug.Log("World Data Saved.");
        }

        public void LoadWorld()
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                string json = PlayerPrefs.GetString(saveKey);
                _persistence.FromJson(json);
                Debug.Log("World Data Loaded.");
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            _frameCount++;
            if (_frameCount % updateFrequencyFrames == 0)
            {
                UpdateChunks();
                CleanupFarChunks();
            }

            if (_needsNavMeshUpdate && Time.time > _lastNavMeshUpdateTime + navMeshUpdateInterval)
            {
                _needsNavMeshUpdate = false;
                _lastNavMeshUpdateTime = Time.time;
                navMeshSurface.BuildNavMesh();
            }
        }

        private void UpdateChunks()
        {
            Vector3 pos = playerTransform.position;
            int currentX = Mathf.FloorToInt(pos.x / chunkSize);
            int currentZ = Mathf.FloorToInt(pos.z / chunkSize);

            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    Vector2Int coords = new Vector2Int(currentX + x, currentZ + z);
                    
                    if (!_activeGroundTiles.ContainsKey(coords))
                    {
                        Vector3 chunkCenter = new Vector3(coords.x * chunkSize, 0, coords.y * chunkSize);
                        Vector3 toChunk = chunkCenter - pos;
                        
                        if (Vector3.Dot(toChunk, playerTransform.forward) > -chunkSize * 1.5f)
                        {
                            ActivateChunk(coords);
                            _needsNavMeshUpdate = true;
                        }
                    }
                }
            }
        }

        private void ActivateChunk(Vector2Int coords)
        {
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

            // Spawn Ground
            Vector3 groundPos = new Vector3(coords.x * chunkSize, 0, coords.y * chunkSize);
            GameObject ground = PoolManager.Instance.Get(groundPrefabs[data.groundPrefabIndex], groundPos, Quaternion.identity);
            
            // Normalize scale using cached mesh size
            if (_groundMeshSizeCache.TryGetValue(data.groundPrefabIndex, out Vector3 meshSize))
            {
                // Force XZ to chunkSize + overlap for a solid connection
                float scaleX = (chunkSize + groundOverlap) / meshSize.x;
                float scaleZ = (chunkSize + groundOverlap) / meshSize.z;
                
                float scaleY = 1f;
                float yOffset = 0f;

                // Ground_02 is a tall hill (height ~5.5). We flatten it to ensure pathfinding connectivity.
                if (groundPrefabs[data.groundPrefabIndex].name == "Ground_02")
                {
                    scaleY = 0.5f; 
                }
                
                // Ground_03 has a lower base. Align it with the others.
                if (groundPrefabs[data.groundPrefabIndex].name == "Ground_03")
                {
                    yOffset = 0.15f;
                }

                ground.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                
                // Adjust position to center the mesh on the chunk grid
                // mesh.bounds.center is the offset of the mesh from the pivot
                MeshFilter filter = groundPrefabs[data.groundPrefabIndex].GetComponentInChildren<MeshFilter>();
                Vector3 meshCenterOffset = filter.sharedMesh.bounds.center;
                
                // We want the mesh center to be at the chunk center
                // So pivot should be at groundPos - scaledCenterOffset
                Vector3 scaledCenterOffset = new Vector3(meshCenterOffset.x * scaleX, 0, meshCenterOffset.z * scaleZ);
                ground.transform.position = groundPos - scaledCenterOffset + Vector3.up * yOffset;
            }

            _activeGroundTiles[coords] = ground;

            // Spawn Nature Objects
            List<GameObject> chunkObjects = new List<GameObject>();

            // SPAWN FILLER GROUND (Basement layer to hide gaps)
            int fillerIndex = -1;
            for (int i = 0; i < groundPrefabs.Length; i++)
            {
                if (groundPrefabs[i].name == "Ground_03") { fillerIndex = i; break; }
            }
            if (fillerIndex != -1)
            {
                // Place it slightly below the main ground
                float basementY = -0.4f;
                Vector3 fillerPos = new Vector3(coords.x * chunkSize, basementY, coords.y * chunkSize);
                GameObject filler = PoolManager.Instance.Get(groundPrefabs[fillerIndex], fillerPos, Quaternion.identity);
                
                if (_groundMeshSizeCache.TryGetValue(fillerIndex, out Vector3 fSize))
                {
                    // Scale basement to be wider than the chunk (chunkSize + 2) to bridge any diagonal gaps
                    float fScaleX = (chunkSize + groundOverlap + 2.0f) / fSize.x;
                    float fScaleZ = (chunkSize + groundOverlap + 2.0f) / fSize.z;
                    filler.transform.localScale = new Vector3(fScaleX, 0.1f, fScaleZ); // Very flat
                    
                    MeshFilter fFilter = groundPrefabs[fillerIndex].GetComponentInChildren<MeshFilter>();
                    Vector3 fOffset = fFilter.sharedMesh.bounds.center;
                    filler.transform.position = fillerPos - new Vector3(fOffset.x * fScaleX, 0, fOffset.z * fScaleZ);
                }
                chunkObjects.Add(filler);
            }

            foreach (var objData in data.objects)
            {
                GameObject prefab = GetPrefabFromData(objData);
                if (prefab != null)
                {
                    Vector3 worldPos = groundPos + objData.localPosition;
                    GameObject instance = PoolManager.Instance.Get(prefab, worldPos, objData.localRotation);
                    
                    // Add cleanup component
                    PooledObject pooled = instance.GetComponent<PooledObject>();
                    if (pooled == null) pooled = instance.AddComponent<PooledObject>();
                    pooled.Setup(playerTransform, cleanupDistanceBehind + chunkSize);
                    
                    chunkObjects.Add(instance);
                }
            }
            _activeNatureObjects[coords] = chunkObjects;
        }

        private ChunkData GenerateChunkData(Vector2Int coords)
        {
            ChunkData data = new ChunkData { coordinates = coords };
            data.groundPrefabIndex = Random.Range(0, groundPrefabs.Length);

            int attempts = Random.Range(5, 15);
            for (int i = 0; i < attempts; i++)
            {
                float r = Random.value;
                int type = -1;
                GameObject[] list = null;

                if (r < treeChance) { type = 0; list = treePrefabs; }
                else if (r < treeChance + rockChance) { type = 1; list = rockPrefabs; }
                else if (r < treeChance + rockChance + bushChance) { type = 2; list = bushPrefabs; }
                else if (r < treeChance + rockChance + bushChance + smallChance) { type = 3; list = smallNaturePrefabs; }

                if (type != -1 && list != null && list.Length > 0)
                {
                    int index = Random.Range(0, list.Length);
                    data.objects.Add(new ObjectData
                    {
                        prefabType = type,
                        prefabIndex = index,
                        localPosition = new Vector3(Random.Range(0, chunkSize), 0, Random.Range(0, chunkSize)),
                        localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0)
                    });
                }
            }

            return data;
        }

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
        private void CleanupFarChunks()
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();
            Vector3 playerPos = playerTransform.position;
            float maxDistSqr = Mathf.Pow(chunkSize * (viewDistance + 1.5f), 2);

            foreach (var coord in _activeGroundTiles.Keys)
            {
                Vector3 chunkCenter = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
                Vector3 toChunk = chunkCenter - playerPos;
                
                // If it's behind the player
                if (Vector3.Dot(toChunk, playerTransform.forward) < -cleanupDistanceBehind - chunkSize)
                {
                    toRemove.Add(coord);
                    _needsNavMeshUpdate = true;
                }
                // Or way too far in any direction
                else if (toChunk.sqrMagnitude > maxDistSqr)
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

        private void DeactivateChunk(Vector2Int coords)
        {
            if (_activeGroundTiles.TryGetValue(coords, out GameObject ground))
            {
                PoolManager.Instance.Return(ground);
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