using System;
using _Scripts.MainGame.Inventory;
using _Scripts.MainGame.Terrain;

namespace _Scripts.MainGame.SaveLoad
{
    [Serializable]
    public class GameSave
    {
        public string characterId = "HumanMale_Character_FREE";
        public string characterName = "Hero";
        public int skinColorIndex;
        public int outfitColorIndex;
        public float playtimeSeconds;
        public int wave;
        public int score;
        public bool hasTerrain;
        public TerrainSaveData terrain = new TerrainSaveData();
        public bool hasInventory;
        public InventorySave inventory = new InventorySave();
        public bool hasPlayer;
        public PlayerSaveData player = new PlayerSaveData();
    }
}
