using System;
using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.Loot
{
    public class Lootable : MonoBehaviour
    {
        [SerializeField] protected ItemData itemData;
        
        public ItemData ItemData { get => itemData; private set => itemData = value; }
        
        private event System.Action OnLoot;

        protected virtual void OnDisable()
        {
            OnLoot = null;
        }

        public void SubscribeOnLoot(System.Action callback)
{
            OnLoot += callback;
        }
        
        public void UnsubscribeOnLoot(System.Action callback)
        {
            OnLoot -= callback;
        }
        
        protected void Loot()
        {
            OnLoot?.Invoke();
        }
    }
}