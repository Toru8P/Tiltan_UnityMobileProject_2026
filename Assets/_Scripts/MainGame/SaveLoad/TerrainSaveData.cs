using System;
using System.Collections.Generic;
using _Scripts.MainGame.Terrain;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    
    [Serializable]
    public class SerializableObjectData
    {
        public string prefabName;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    
    [Serializable]
    public class ChunkSaveData
    {
        public int row;
        public int col;
        public List<SerializableObjectData> objects = new List<SerializableObjectData>();
    }
    
    [Serializable]
    public class TerrainSaveData
    {
        public int seed;
        public int width;
        public int height;
        public int resolution;
        public NoiseSettings noiseSettings;
        public List<ChunkSaveData> chunks = new List<ChunkSaveData>();
    }
}
