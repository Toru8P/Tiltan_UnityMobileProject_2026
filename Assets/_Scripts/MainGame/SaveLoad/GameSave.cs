using System;
using _Scripts.MainGame.Inventory;
using _Scripts.MainGame.Terrain;

namespace _Scripts.MainGame.SaveLoad
{
    [Serializable]
    public class GameSave
    {
        public bool hasTerrain;
        public TerrainSaveData terrain = new TerrainSaveData();

        public bool hasInventory;
        public InventorySave inventory = new InventorySave();
    }
}
