using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.World
{
    [Serializable]
    public class ObjectData
    {
        public int prefabType; // 0: Tree, 1: Rock, 2: Bush, 3: Small
        public int prefabIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    [Serializable]
    public class ChunkData
    {
        public Vector2Int coordinates;
        public int groundPrefabIndex;
        public List<ObjectData> objects = new List<ObjectData>();
    }

    [Serializable]
    public class WorldDataWrapper
    {
        public List<ChunkData> chunks = new List<ChunkData>();
    }

    // Handles storing every chunk the player has explored.
    // Used by WorldGenerator so re-visiting a chunk shows the SAME trees/rocks instead of fresh random ones.
    public class WorldPersistence
    {
        private Dictionary<Vector2Int, ChunkData> _exploredChunks = new Dictionary<Vector2Int, ChunkData>();

        // True if this chunk has already been generated and saved.
        public bool HasChunk(Vector2Int coords) => _exploredChunks.ContainsKey(coords);

        // Returns the saved data for an already-explored chunk.
        public ChunkData GetChunk(Vector2Int coords) => _exploredChunks[coords];

        // Stores chunk data (overwrites if it already existed).
        public void SaveChunk(ChunkData data)
        {
            _exploredChunks[data.coordinates] = data;
        }

        // Serializes all explored chunks to a JSON string for saving to disk (PlayerPrefs).
        public string ToJson()
        {
            WorldDataWrapper wrapper = new WorldDataWrapper();
            foreach (var chunk in _exploredChunks.Values)
            {
                wrapper.chunks.Add(chunk);
            }
            return JsonUtility.ToJson(wrapper);
        }

        // Reads a JSON string back into the chunk dictionary. Used at game start to restore the saved world.
        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            WorldDataWrapper wrapper = JsonUtility.FromJson<WorldDataWrapper>(json);
            _exploredChunks.Clear();
            foreach (var chunk in wrapper.chunks)
            {
                _exploredChunks[chunk.coordinates] = chunk;
            }
        }
    }
}