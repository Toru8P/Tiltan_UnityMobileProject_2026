using System;
using System.Collections.Generic;
using _Scripts.MainGame.Pool;
using UnityEngine;

namespace _Scripts.MainGame.Terrain
{
    public class Chunk : MonoBehaviour
    {
        public List<ObjectData> objects = new List<ObjectData>();
        private Dictionary<ObjectData, GameObject> _activeObjects = new Dictionary<ObjectData, GameObject>();
        
        private NoiseSettings _noiseSettings = NoiseSettings.Default;
        
        private Vector3 _position;
        private int _width;
        private int _height;
        private Vector3 _localUp;
        private int _resolution;
        
        private BoxCollider _playerTrigger;
        private MeshCollider _meshCollider;
        
        public event System.Action OnPlayerEnter;

        // Stores the tile's size and resolution. Called right after the Chunk component is added.
        public void Setup(NoiseSettings noiseSettings, int width, int height, Vector3 localUp, int resolution = 10)
        {
            _width = width;
            _height = height;
            _localUp = localUp;

            _resolution = resolution;
            
            _noiseSettings = noiseSettings;
            
            _position = Vector3.zero;
        }

        public void Fill(ObjectPool pool)
        {
            foreach (ObjectData objectData in objects)
            {
                GameObject instance = pool.Get(objectData.prefab);
                instance.transform.SetParent(this.transform);
                instance.transform.localPosition = objectData.localPosition;
                instance.transform.localRotation = objectData.localRotation;
                instance.SetActive(true);

                _activeObjects[objectData] = instance;
            }
        }

        public void Unfill(ObjectPool pool)
        {
            foreach (KeyValuePair<ObjectData, GameObject> keyValuePair in _activeObjects)
            {
                pool.Return(keyValuePair.Value);
            }
            _activeObjects.Clear();
        }

        public void AddObjectData(ObjectData objectData)
        {
            objects.Add(objectData);
        }

        // Sets where this tile lives in world space.
        public void SetPosition(Vector3 position)
        {
            _position = position;
            transform.position = position;
        }

        // Editor-only: draws the tile's outline and corners in the Scene view so you can verify size and placement.
        void OnDrawGizmosSelected()
        {
            Vector3 center = Center();
            Vector3 left = center - (_width / 2f) * Vector3.right;
            Vector3 right = center + (_width / 2f) * Vector3.right;
            Vector3 forward = center + (_height / 2f) * Vector3.forward;
            Vector3 back = center - (_height / 2f) * Vector3.forward;
            Vector3 leftForward = center - (_width / 2f) * Vector3.right + (_height / 2f) * Vector3.forward;
            Vector3 rightForward = center + (_width / 2f) * Vector3.right + (_height / 2f) * Vector3.forward;
            Vector3 leftBack = center - (_width / 2f) * Vector3.right - (_height / 2f) * Vector3.forward;
            Vector3 rightBack = center + (_width / 2f) * Vector3.right - (_height / 2f) * Vector3.forward;

            Gizmos.color = Color.blueViolet;
            Gizmos.DrawWireSphere(center, 0.2f);
            Gizmos.color = Color.blue;
            
            Gizmos.DrawCube(left, Vector3.one * 0.1f);
            Gizmos.DrawCube(right, Vector3.one * 0.1f);
            Gizmos.DrawCube(forward, Vector3.one * 0.1f);
            Gizmos.DrawCube(back, Vector3.one * 0.1f);
            Gizmos.DrawCube(leftForward, Vector3.one * 0.1f);
            Gizmos.DrawCube(rightForward, Vector3.one * 0.1f);
            Gizmos.DrawCube(leftBack, Vector3.one * 0.1f);
            Gizmos.DrawCube(rightBack, Vector3.one * 0.1f);
            
            Gizmos.DrawLine(left, leftBack);
            Gizmos.DrawLine(left, leftForward);
            Gizmos.DrawLine(right, rightBack);
            Gizmos.DrawLine(right, rightForward);
            Gizmos.DrawLine(forward, leftForward);
            Gizmos.DrawLine(forward, rightForward);
            Gizmos.DrawLine(back, leftBack);
            Gizmos.DrawLine(back, rightBack);
        }


        // Returns the world-space center of this tile.
        public Vector3 Center()
        {
            return _position;
        }

        // Adds a tall, invisible trigger box sitting on top of this tile.
        // When the player enters it, the onPlayerEnter callback fires (used to detect tile crossings).
        public void CreatePlayerZone(float tall = 50f, Action onPlayerEnter = null)
        {
            if (_playerTrigger) return; // Already created
            _playerTrigger = this.gameObject.AddComponent<BoxCollider>();
            _playerTrigger.size = new Vector3(_width, tall,_height);
            _playerTrigger.center = new Vector3(0, tall/2f, 0);
            _playerTrigger.isTrigger = true;
            
            this.OnPlayerEnter += () => onPlayerEnter?.Invoke();
        }

        // Unity calls this automatically when something enters the trigger.
        // If it's the player, fire the OnPlayerEnter event so the parent Terrain can update tiles.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) OnPlayerEnter?.Invoke();
        }

        // Procedurally builds a flat rectangular mesh at runtime.
        // Step 1: create a grid of vertices (resolution + 1 in each direction) with UVs.
        // Step 2: connect them into triangles (two triangles per grid cell = a quad).
        // Step 3: assign the mesh to a MeshFilter/MeshRenderer/MeshCollider, with a URP Lit material.
        public void CreateMesh()
        {
            int res = Mathf.Max(1, _resolution);
            int vertsX = res + 1;
            int vertsZ = res + 1;
            
            int vertCount = vertsX * vertsZ;
            int triCount = res * res * 6;
            
            float halfW = _width * 0.5f;
            float halfH = _height * 0.5f;
            
            float stepX = (float)_width / res;
            float stepZ = (float)_height / res;
            
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[triCount];
            
            for (int z = 0; z < vertsZ; z++)
            {
                for (int x = 0; x < vertsX; x++)
                {
                    int idx = z * vertsX + x;
                    float vx = -halfW + x * stepX;
                    float vz = -halfH + z * stepZ;
                    
                    float worldX = transform.position.x + vx;
                    float worldZ = transform.position.z + vz;
                    float y = NoiseGenerator.SampleHeight(worldX, worldZ, _noiseSettings);

                    vertices[idx] = new Vector3(vx, y, vz);
                    uvs[idx] = new Vector2((float)x / res, (float)z / res);
                }
            }
            
            int t = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int topLeft = z * vertsX + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * vertsX + x;
                    int bottomRight = bottomLeft + 1;

                    // first tri
                    triangles[t++] = topLeft;
                    triangles[t++] = bottomLeft;
                    triangles[t++] = topRight;

                    // second tri
                    triangles[t++] = topRight;
                    triangles[t++] = bottomLeft;
                    triangles[t++] = bottomRight;
                }
            }
            
            Mesh mesh = new Mesh
            {
                name = "TerrainPlane_Mesh",
                vertices = vertices,
                triangles = triangles,
                uv = uvs
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            MeshFilter mf = GetComponent<MeshFilter>();
            if (!mf) mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (!mr) mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            MeshCollider mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            _meshCollider = mc;
        }

        public Vector3 Position => _position;
        public int Width => _width;
        public int Height => _height;
        public MeshCollider MeshCollider => _meshCollider;
    }
    
    [Serializable]
    public class ObjectData
    {
        public GameObject prefab;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}