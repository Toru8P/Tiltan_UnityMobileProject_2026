using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.MainGame.UI;
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
        [SerializeField] private AmountBarInUIDriver hpBarDriver;
        [SerializeField] private AmountBarInUIDriver shieldBarDriver;

        public int CurrentHealth => currentStats.CurrentHealth;
        public bool IsDead => currentStats.CurrentHealth <= 0;

        private void Start()
        {
            currentStats.Fill(initialStats);
            if (startWithFullHp)
            {
                currentStats.CurrentHealth = currentStats.MaxHealth;
                currentStats.CurrentShield = currentStats.MaxShield;
            }

            UpdateBars();
            
            StartCoroutine(Regenerate());
        }

        private IEnumerator Regenerate()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                if (currentStats.CurrentHealth == 0) break;
                currentStats.CurrentHealth = Mathf.Min(currentStats.CurrentHealth + currentStats.HpRegenerationAmount, currentStats.MaxHealth);
                currentStats.CurrentShield = Mathf.Min(currentStats.CurrentShield + currentStats.ShieldRegenerationAmount, currentStats.MaxShield);
                UpdateBars();
            }
        }

        private void UpdateBars()
        {
            hpBarDriver.SetFill(currentStats.CurrentHealth, currentStats.MaxHealth);
            shieldBarDriver.SetFill(currentStats.CurrentShield, currentStats.MaxShield);
        }

        public void DealDamage(int damage)
        {
            int totalDamage = damage;
            if (currentStats.CurrentShield > 0)
            {
                currentStats.CurrentShield -= damage;
                if (currentStats.CurrentShield < 0)
                {
                    damage = -currentStats.CurrentShield; // Remaining damage after shield is depleted
                    currentStats.CurrentShield = 0;
                }
                else
                {
                    damage = 0; // All damage absorbed by shield
                }
                shieldBarDriver.SetFill(currentStats.CurrentShield, currentStats.MaxShield);
            }
            
            if (damage > 0)
            {
                currentStats.CurrentHealth -= damage;
                if (currentStats.CurrentHealth <= 0) 
                {
                    currentStats.CurrentHealth = 0;
                }
                hpBarDriver.SetFill(currentStats.CurrentHealth, currentStats.MaxHealth);
            }

            if (IndicatorManager.Instance != null && totalDamage > 0)
            {
                IndicatorManager.Instance.SpawnDamagePlayer(transform.position, totalDamage);
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || currentStats.CurrentHealth >= currentStats.MaxHealth) return;

            int oldHealth = currentStats.CurrentHealth;
            currentStats.CurrentHealth = Mathf.Min(currentStats.CurrentHealth + amount, currentStats.MaxHealth);
            int actualHeal = currentStats.CurrentHealth - oldHealth;

            if (actualHeal > 0)
            {
                UpdateBars();
                if (IndicatorManager.Instance != null)
                {
                    IndicatorManager.Instance.SpawnHealing(transform.position, actualHeal);
                }
            }
        }
    }

    [Serializable]
    public struct PlayerStats
    {
        public int MaxHealth;
        public int CurrentHealth;
        public int MaxShield;
        public int CurrentShield;
        public int HpRegenerationAmount;
        public int ShieldRegenerationAmount;

        public void Fill(PlayerStats stats)
        {
            CurrentHealth = stats.CurrentHealth;
            MaxHealth = stats.MaxHealth;
            CurrentShield = stats.CurrentShield;
            MaxShield = stats.MaxShield;
            HpRegenerationAmount = stats.HpRegenerationAmount;
            ShieldRegenerationAmount = stats.ShieldRegenerationAmount;
        }
    }
}