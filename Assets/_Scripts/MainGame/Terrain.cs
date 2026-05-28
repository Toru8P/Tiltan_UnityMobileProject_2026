using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.MainGame
{
    public class Terrain : MonoBehaviour
    {
        [Range(2, 256)] public int resolution = 10;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private int renderDistance = 2;
        private Dictionary<Vector2Int, TerrainPlane> terrainFaces = new Dictionary<Vector2Int, TerrainPlane>();
        private List<TerrainPlane> _activePlanes = new List<TerrainPlane>();

        // Builds the starting 3x3 grid of terrain tiles around the player and activates all of them.
        public void Start()
        {
            ActivePlane(GenerateOrGetPlaneAt(-1, -1));
            ActivePlane(GenerateOrGetPlaneAt(-1, 0));
            ActivePlane(GenerateOrGetPlaneAt(-1, 1));

            ActivePlane(GenerateOrGetPlaneAt(0, -1));
            ActivePlane(GenerateOrGetPlaneAt(0, 0));
            ActivePlane(GenerateOrGetPlaneAt(0, 1));

            ActivePlane(GenerateOrGetPlaneAt(1, -1));
            ActivePlane(GenerateOrGetPlaneAt(1, 1));
            ActivePlane(GenerateOrGetPlaneAt(1, 0));
        }

        // Turns a tile on (visible + collidable) and tracks it as active.
        // Skips if it's already active, so we don't double-add to the list.
        private void ActivePlane(TerrainPlane plane)
        {
            if (_activePlanes.Contains(plane)) return;
            plane.gameObject.SetActive(true);
            _activePlanes.Add(plane);
        }

        // Turns a tile off and removes it from the active list. Saves performance — disabled tiles aren't rendered.
        private void DeactivatePlane(TerrainPlane plane)
        {
            plane.gameObject.SetActive(false);
            _activePlanes.Remove(plane);
        }

        // Editor-only visualization: draws a green sphere at the terrain's origin
        // and red dots at each generated tile's center. Helps you see the grid in the Scene view.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 2f);
            Gizmos.color = Color.red;
            foreach (TerrainPlane terrainPlane in terrainFaces.Values.Where(terrainPlane => terrainPlane != null))
            {
                Gizmos.DrawWireSphere(terrainPlane.Center() + new Vector3(0, 2, 0), 0.2f);
            }
        }

        // Returns the tile at grid coords (row, col). If it doesn't exist yet,
        // creates a new GameObject, attaches a TerrainPlane, builds its mesh, and adds a trigger zone
        // that calls UpdateFacesAroundPlayer when the player enters this tile.
        private TerrainPlane GenerateOrGetPlaneAt(int row, int col)
        {
            Vector2Int key = new Vector2Int(row, col);
            if (terrainFaces.TryGetValue(key, out TerrainPlane at)) return at; // Already generated

            Vector3 offset = new Vector3(col * width, 0, row * height);
            GameObject planeObj = new GameObject($"TerrainFace_{row}_{col}");
            planeObj.transform.parent = this.transform;

            TerrainPlane terrainFace = planeObj.AddComponent<TerrainPlane>();
            terrainFace.Setup(width, height, Vector3.up);
            terrainFace.SetPosition(this.transform.position + offset);
            terrainFace.CreateMesh();
            terrainFace.CreatePlayerZone(50f, () =>
            {
                Debug.Log("Player entered terrain face at (" + row + ", " + col + ")");
                UpdateFacesAroundPlayer(row, col);
            });
            terrainFaces[key] = terrainFace;
            planeObj.SetActive(false);
            return terrainFace;
        }

        // Called when the player enters a tile. Activates every tile within renderDistance of the player
        // and deactivates everything else. Result: only nearby terrain is rendered, the rest is asleep.
        private void UpdateFacesAroundPlayer(int row, int col)
        {
            List<TerrainPlane> planesToDeactivate = new List<TerrainPlane>(_activePlanes);
            List<TerrainPlane> planesToActivate = new List<TerrainPlane>();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    int checkRow = row + z;
                    int checkCol = col + x;
                    TerrainPlane plane = GenerateOrGetPlaneAt(checkRow, checkCol);
                    if (Mathf.Abs(x) <= renderDistance && Mathf.Abs(z) <= renderDistance)
                    {
                        if (planesToDeactivate.Contains(plane))
                        {
                            planesToDeactivate.Remove(plane);
                        }
                        else
                        {
                            planesToActivate.Add(plane);
                        }
                    }
                    else
                    {
                        if (planesToDeactivate.Contains(plane)) continue;
                        planesToDeactivate.Add(plane);
                    }
                }
            }
            
            planesToActivate.ForEach(ActivePlane);
            planesToDeactivate.ForEach(DeactivatePlane);
        }
    }
}