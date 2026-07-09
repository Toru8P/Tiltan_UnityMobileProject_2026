using UnityEngine;

namespace _Scripts.MainGame.Loot
{
    public class Resource : Lootable
    {
        [SerializeField] private int amount = 1;

        public int Amount => amount;

        public int GatherResource(int damage = 1)
        {
            int finalAmount = Mathf.Min(damage, this.amount);
            this.amount -= finalAmount;
            if (this.amount <= 0)
            {
                Loot();
            }
            return finalAmount;
        }
    }
}
