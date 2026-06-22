using System;
using _Scripts.MainGame.Canvas;
using UnityEngine;

namespace _Scripts.MainGame.Player
{
    public class PlayerStatsController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private bool startWithFullHp;
        [SerializeField] private PlayerStats initialStats = new PlayerStats();
        [SerializeField] private PlayerStats currentStats = new PlayerStats();
        
        [Header("UI")]
        [SerializeField] private HpBarInUIDriver hpBarDriver;

        private void Start()
        {
            currentStats.Fill(initialStats);
            if (startWithFullHp)
            {
                currentStats.CurrentHealth = currentStats.MaxHealth;
            }
            hpBarDriver.SetFill(currentStats.CurrentHealth, currentStats.MaxHealth);
        }

        public void DealDamage(int damage)
        {
            currentStats.CurrentHealth -= damage;
            if  (currentStats.CurrentHealth <= 0) 
            {
                currentStats.CurrentHealth = 0;
            }
            hpBarDriver.SetFill(currentStats.CurrentHealth, currentStats.MaxHealth);
        }
    }

    [Serializable]
    public struct PlayerStats
    {
        public int MaxHealth;
        public int CurrentHealth;

        public void Fill(PlayerStats stats)
        {
            CurrentHealth = stats.CurrentHealth;
            MaxHealth = stats.MaxHealth;
        }
    }
}