using System;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    // Persisted player state. Position + facing so the character resumes where they left off.
    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public float rotationY;
    }
}
