using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.MainGame.Pool;
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
        [SerializeField] private LayerMask terrainMask;

        [Header("Probabilities")] [Range(0, 1)]
        public float treeChance = 0.25f; // Increased

        [Range(0, 1)] public float rockChance = 0.2f;
        [Range(0, 1)] public float bushChance = 0.1f;
        [Range(0, 1)] public float smallChance = 0.45f;

        [Range(2, 256)] public int resolution = 10;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private int renderDistance = 2;
        private Dictionary<Vector2Int, Chunk> terrainChunks = new Dictionary<Vector2Int, Chunk>();
        private List<Chunk> _activeChunks = new List<Chunk>();

        [SerializeField] private ObjectPool pool;


        public void Start()
        {
            ActivateChunk(GenerateOrGetChunkAt(-1, -1));
            ActivateChunk(GenerateOrGetChunkAt(-1, 0));
            ActivateChunk(GenerateOrGetChunkAt(-1, 1));

            ActivateChunk(GenerateOrGetChunkAt(0, -1));
            ActivateChunk(GenerateOrGetChunkAt(0, 0));
            ActivateChunk(GenerateOrGetChunkAt(0, 1));

            ActivateChunk(GenerateOrGetChunkAt(1, -1));
            ActivateChunk(GenerateOrGetChunkAt(1, 1));
            ActivateChunk(GenerateOrGetChunkAt(1, 0));
        }

        private void ActivateChunk(Chunk chunk)
        {
            if (_activeChunks.Contains(chunk)) return;
            chunk.Fill(pool);
            chunk.gameObject.SetActive(true);
            _activeChunks.Add(chunk);
        }

        private void DeactivateChunk(Chunk chunk)
        {
            chunk.gameObject.SetActive(false);
            chunk.Unfill(pool);
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
            chunkObj.transform.parent = this.transform;

            Chunk terrainChunk = chunkObj.AddComponent<Chunk>();
            terrainChunk.Setup(noiseSettings, width, height, Vector3.up, resolution);
            terrainChunk.SetPosition(this.transform.position + offset);
            terrainChunk.CreateMesh();
            terrainChunk.CreatePlayerZone(50f, () =>
            {
                Debug.Log("Player entered terrain face at (" + row + ", " + col + ")");
                UpdateFacesAroundPlayer(row, col);
            });
            terrainChunks[key] = terrainChunk;
            FillNewChunk(terrainChunk);
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
        }

        private void FillNewChunk(Chunk chunk)
        {
            int attempts = Random.Range(10, 25);

            for (int i = 0; i < attempts; i++)
            {
                float r = Random.Range(0f, treeChance + rockChance + bushChance + smallChance);
                GameObject[] list = null;

                if (r < treeChance) list = treePrefabs;
                else if (r < treeChance + rockChance) list = rockPrefabs;
                else if (r < treeChance + rockChance + bushChance) list = bushPrefabs;
                else list = smallNaturePrefabs;

                if (list == null || list.Length == 0) continue;

                int index = Random.Range(0, list.Length);

                // Random local X/Z inside the chunk extents
                float localX = Random.Range(-chunk.Width * 0.5f, chunk.Width * 0.5f);
                float localZ = Random.Range(-chunk.Height * 0.5f, chunk.Height * 0.5f);

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
                        localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                    });
                }
            }
        }
    }
}