using System;
using UnityEngine;

namespace _Scripts.MainGame.SaveLoad
{
    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public float rotationY;
        public int currentHealth;
        public int currentShield;
    }
}
