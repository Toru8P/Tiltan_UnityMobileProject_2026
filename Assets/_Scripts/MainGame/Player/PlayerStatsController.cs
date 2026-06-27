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
        
        [Header("Effective Stats")]
        [SerializeField] private int effectiveAttack;
        [SerializeField] private int effectiveDefense;
        [SerializeField] private float effectiveMoveSpeed;
        [SerializeField] private float effectiveAttackSpeed;

        public int EffectiveAttack => effectiveAttack;
        public int EffectiveDefense => effectiveDefense;
        public float EffectiveMoveSpeed => effectiveMoveSpeed;
        public float EffectiveAttackSpeed => effectiveAttackSpeed;

        [Header("UI")]
        [SerializeField] private AmountBarInUIDriver hpBarDriver;
        [SerializeField] private AmountBarInUIDriver shieldBarDriver;

        public int CurrentHealth => currentStats.CurrentHealth;
        public bool IsDead => currentStats.CurrentHealth <= 0;

        private PlayerEquipment _equipment;

        private void Awake()
        {
            _equipment = GetComponent<PlayerEquipment>();
        }

        private void Start()
        {
            currentStats.Fill(initialStats);
            if (startWithFullHp)
            {
                currentStats.CurrentHealth = currentStats.MaxHealth;
                currentStats.CurrentShield = currentStats.MaxShield;
            }

            if (_equipment != null)
                _equipment.OnItemEquipped += OnHeldItemChanged;

            if (_Scripts.MainGame.Inventory.PlayerArmorManager.Instance != null)
                _Scripts.MainGame.Inventory.PlayerArmorManager.Instance.OnArmorChanged += OnArmorChanged;

            RecalculateStats();
            UpdateBars();
            
            StartCoroutine(Regenerate());
        }

        private void OnDestroy()
        {
            if (_equipment != null)
                _equipment.OnItemEquipped -= OnHeldItemChanged;

            if (_Scripts.MainGame.Inventory.PlayerArmorManager.Instance != null)
                _Scripts.MainGame.Inventory.PlayerArmorManager.Instance.OnArmorChanged -= OnArmorChanged;
        }

        private void OnHeldItemChanged(_Scripts.MainGame.Inventory.ItemData item)
        {
            RecalculateStats();
        }

        private void OnArmorChanged(_Scripts.MainGame.Inventory.ArmorSlot slot, _Scripts.MainGame.Inventory.ItemData item)
        {
            RecalculateStats();
        }

        public void RecalculateStats()
        {
            float flatAttack = 0;
            float percAttack = 0;
            float flatDefense = 0;
            float percDefense = 0;
            float flatSpeed = 0;
            float percSpeed = 0;
            float flatAtkSpd = 0;
            float percAtkSpd = 0;

            // Held Item
            if (_equipment != null && _equipment.CurrentItem != null)
            {
                ApplyModifiers(_equipment.CurrentItem, ref flatAttack, ref percAttack, ref flatDefense, ref percDefense, ref flatSpeed, ref percSpeed, ref flatAtkSpd, ref percAtkSpd);
            }

            // Armor
            if (_Scripts.MainGame.Inventory.PlayerArmorManager.Instance != null)
            {
                foreach (_Scripts.MainGame.Inventory.ArmorSlot slot in System.Enum.GetValues(typeof(_Scripts.MainGame.Inventory.ArmorSlot)))
                {
                    if (slot == _Scripts.MainGame.Inventory.ArmorSlot.None) continue;
                    var armor = _Scripts.MainGame.Inventory.PlayerArmorManager.Instance.GetEquippedItem(slot);
                    if (armor != null)
                    {
                        ApplyModifiers(armor, ref flatAttack, ref percAttack, ref flatDefense, ref percDefense, ref flatSpeed, ref percSpeed, ref flatAtkSpd, ref percAtkSpd);
                    }
                }
            }

            // Defaults if not set in inspector
            float baseAtk = initialStats.BaseAttack == 0 ? 25 : initialStats.BaseAttack;
            float baseDef = initialStats.BaseDefense;
            float baseMov = initialStats.BaseMoveSpeed == 0 ? 6f : initialStats.BaseMoveSpeed;
            float baseAtkSpd = initialStats.BaseAttackSpeed == 0 ? 1f : initialStats.BaseAttackSpeed;

            effectiveAttack = Mathf.Max(0, Mathf.RoundToInt((baseAtk + flatAttack) * (1 + percAttack)));
            effectiveDefense = Mathf.Max(0, Mathf.RoundToInt((baseDef + flatDefense) * (1 + percDefense)));
            effectiveMoveSpeed = Mathf.Max(0.1f, (baseMov + flatSpeed) * (1 + percSpeed));
            effectiveAttackSpeed = Mathf.Max(0.1f, (baseAtkSpd + flatAtkSpd) * (1 + percAtkSpd));
        }

        private void ApplyModifiers(_Scripts.MainGame.Inventory.ItemData item, ref float fAtk, ref float pAtk, ref float fDef, ref float pDef, ref float fSpd, ref float pSpd, ref float fAtkSpd, ref float pAtkSpd)
        {
            if (item.statModifiers == null) return;
            foreach (var mod in item.statModifiers)
            {
                switch (mod.statType)
                {
                    case _Scripts.MainGame.Inventory.StatType.Attack:
                        fAtk += mod.flatAmount;
                        pAtk += mod.percentageAmount;
                        break;
                    case _Scripts.MainGame.Inventory.StatType.Defense:
                        fDef += mod.flatAmount;
                        pDef += mod.percentageAmount;
                        break;
                    case _Scripts.MainGame.Inventory.StatType.MovementSpeed:
                        fSpd += mod.flatAmount;
                        pSpd += mod.percentageAmount;
                        break;
                    case _Scripts.MainGame.Inventory.StatType.AttackSpeed:
                        fAtkSpd += mod.flatAmount;
                        pAtkSpd += mod.percentageAmount;
                        break;
                }
            }
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
            // Simple defense formula: damage = damage * (100 / (100 + defense))
            float reduction = effectiveDefense / (effectiveDefense + 50f); // 50 defense = 50% reduction
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction)));

            int totalDamage = finalDamage;
            damage = finalDamage;

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

        [Header("Combat Stats")]
        public int BaseAttack;
        public int BaseDefense;
        public float BaseMoveSpeed;
        public float BaseAttackSpeed;

        public void Fill(PlayerStats stats)
        {
            CurrentHealth = stats.CurrentHealth;
            MaxHealth = stats.MaxHealth;
            CurrentShield = stats.CurrentShield;
            MaxShield = stats.MaxShield;
            HpRegenerationAmount = stats.HpRegenerationAmount;
            ShieldRegenerationAmount = stats.ShieldRegenerationAmount;
            BaseAttack = stats.BaseAttack;
            BaseDefense = stats.BaseDefense;
            BaseMoveSpeed = stats.BaseMoveSpeed;
            BaseAttackSpeed = stats.BaseAttackSpeed;
        }
    }
}