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

    public class WorldPersistence
    {
        private Dictionary<Vector2Int, ChunkData> _exploredChunks = new Dictionary<Vector2Int, ChunkData>();

        public bool HasChunk(Vector2Int coords) => _exploredChunks.ContainsKey(coords);

        public ChunkData GetChunk(Vector2Int coords) => _exploredChunks[coords];

        public void SaveChunk(ChunkData data)
        {
            _exploredChunks[data.coordinates] = data;
        }

        public string ToJson()
        {
            WorldDataWrapper wrapper = new WorldDataWrapper();
            foreach (var chunk in _exploredChunks.Values)
            {
                wrapper.chunks.Add(chunk);
            }
            return JsonUtility.ToJson(wrapper);
        }

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