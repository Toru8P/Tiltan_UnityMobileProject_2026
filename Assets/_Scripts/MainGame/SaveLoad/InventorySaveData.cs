using System;
using System.Collections.Generic;

namespace _Scripts.MainGame.SaveLoad
{
    [Serializable]
    public class InventorySlotSave
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class InventorySave
    {
        public List<InventorySlotSave> slots = new List<InventorySlotSave>();
    }
}
