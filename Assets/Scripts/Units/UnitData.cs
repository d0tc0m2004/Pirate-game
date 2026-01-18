using System;
using System.Collections.Generic;
using TacticalGame.Equipment;
using TacticalGame.Enums;

/// <summary>
/// Data container for unit information.
/// Used during character creation and passed to battle system.
/// </summary>
[Serializable]
public class UnitData
{
    #region Basic Info
    
    public string unitName;
    public UnitRole role;
    public Team team;
    public WeaponType weaponType;
    public WeaponFamily weaponFamily;
    
    #endregion
    
    #region Primary/Secondary Stats
    
    public StatType primaryStat;
    public StatType secondaryPrimaryStat; // Captain's 2nd primary
    public StatType secondaryStat;
    public bool hasTwoPrimaryStats;
    
    #endregion
    
    #region Stat Values
    
    public int health;
    public int morale;
    public int grit;
    public int buzz;
    public int power;
    public int aim;
    public int proficiency;
    public int skill;
    public int tactics;
    public int speed;
    public int hull;
    
    #endregion
    
    #region Equipment - Weapon Relic (legacy)
    
    /// <summary>
    /// Default weapon relic assigned during character creation.
    /// </summary>
    [NonSerialized]
    public WeaponRelic defaultWeaponRelic;
    
    /// <summary>
    /// Legacy equipment data component (for backwards compatibility).
    /// Auto-initialized on first access.
    /// </summary>
    [NonSerialized]
    private UnitEquipmentData _equipment;
    
    public UnitEquipmentData equipment
    {
        get
        {
            if (_equipment == null)
                _equipment = new UnitEquipmentData();
            return _equipment;
        }
        set { _equipment = value; }
    }
    
    #endregion
    
    #region Equipment - New Relic Storage
    
    /// <summary>
    /// Weapon relics for slots 0-6.
    /// </summary>
    [NonSerialized]
    public WeaponRelic[] weaponRelics = new WeaponRelic[7];
    
    /// <summary>
    /// Category relics for slots 0-6 (Boots, Gloves, Hat, Coat, Trinket, Totem).
    /// </summary>
    [NonSerialized]
    public EquippedRelic[] categoryRelics = new EquippedRelic[7];
    
    #endregion
    
    #region Stat Methods
    
    /// <summary>
    /// Set a stat value by StatType.
    /// </summary>
    public void SetStat(StatType stat, int value)
    {
        switch (stat)
        {
            case StatType.Health: health = value; break;
            case StatType.Morale: morale = value; break;
            case StatType.Grit: grit = value; break;
            case StatType.Buzz: buzz = value; break;
            case StatType.Power: power = value; break;
            case StatType.Aim: aim = value; break;
            case StatType.Proficiency: proficiency = value; break;
            case StatType.Skill: skill = value; break;
            case StatType.Tactics: tactics = value; break;
            case StatType.Speed: speed = value; break;
            case StatType.Hull: hull = value; break;
        }
    }
    
    /// <summary>
    /// Get a stat value by StatType.
    /// </summary>
    public int GetStat(StatType stat)
    {
        return stat switch
        {
            StatType.Health => health,
            StatType.Morale => morale,
            StatType.Grit => grit,
            StatType.Buzz => buzz,
            StatType.Power => power,
            StatType.Aim => aim,
            StatType.Proficiency => proficiency,
            StatType.Skill => skill,
            StatType.Tactics => tactics,
            StatType.Speed => speed,
            StatType.Hull => hull,
            _ => 0
        };
    }
    
    #endregion
    
    #region Display Methods
    
    /// <summary>
    /// Get role display name.
    /// </summary>
    public string GetRoleDisplayName()
    {
        return role switch
        {
            UnitRole.MasterGunner => "Master Gunner",
            UnitRole.MasterAtArms => "Master-at-Arms",
            _ => role.ToString()
        };
    }
    
    /// <summary>
    /// Get weapon family display name.
    /// </summary>
    public string GetWeaponFamilyDisplayName()
    {
        return weaponFamily.ToString();
    }
    
    #endregion
    
    #region Weapon Relic Methods
    
    /// <summary>
    /// Equip a weapon relic to a slot.
    /// </summary>
    public void EquipWeaponRelic(int slot, WeaponRelic relic)
    {
        if (weaponRelics == null) weaponRelics = new WeaponRelic[7];
        if (slot < 0 || slot >= weaponRelics.Length) return;
        weaponRelics[slot] = relic;
        
        // Also set default for backwards compatibility
        if (slot == 0 && relic != null)
            defaultWeaponRelic = relic;
    }
    
    /// <summary>
    /// Get weapon relic from a slot.
    /// </summary>
    public WeaponRelic GetWeaponRelic(int slot)
    {
        if (weaponRelics == null) return null;
        if (slot < 0 || slot >= weaponRelics.Length) return null;
        return weaponRelics[slot];
    }
    
    /// <summary>
    /// Get all equipped weapon relics.
    /// </summary>
    public List<WeaponRelic> GetAllWeaponRelics()
    {
        var list = new List<WeaponRelic>();
        if (weaponRelics == null) return list;
        
        foreach (var relic in weaponRelics)
        {
            if (relic != null)
                list.Add(relic);
        }
        return list;
    }
    
    #endregion
    
    #region Category Relic Methods
    
    /// <summary>
    /// Equip a category relic to a slot.
    /// </summary>
    public void EquipCategoryRelic(int slot, EquippedRelic relic)
    {
        if (categoryRelics == null) categoryRelics = new EquippedRelic[7];
        if (slot < 0 || slot >= categoryRelics.Length) return;
        categoryRelics[slot] = relic;
    }
    
    /// <summary>
    /// Get category relic from a slot.
    /// </summary>
    public EquippedRelic GetCategoryRelic(int slot)
    {
        if (categoryRelics == null) return null;
        if (slot < 0 || slot >= categoryRelics.Length) return null;
        return categoryRelics[slot];
    }
    
    /// <summary>
    /// Get all equipped category relics.
    /// </summary>
    public List<EquippedRelic> GetAllCategoryRelics()
    {
        var list = new List<EquippedRelic>();
        if (categoryRelics == null) return list;
        
        foreach (var relic in categoryRelics)
        {
            if (relic != null)
                list.Add(relic);
        }
        return list;
    }
    
    #endregion
    
    #region Equipment Clear Methods
    
    /// <summary>
    /// Clear a slot (both weapon and category).
    /// </summary>
    public void ClearSlot(int slot)
    {
        if (weaponRelics != null && slot >= 0 && slot < weaponRelics.Length)
            weaponRelics[slot] = null;
        if (categoryRelics != null && slot >= 0 && slot < categoryRelics.Length)
            categoryRelics[slot] = null;
    }
    
    /// <summary>
    /// Clear all equipment.
    /// </summary>
    public void ClearAllEquipment()
    {
        if (weaponRelics != null)
        {
            for (int i = 0; i < weaponRelics.Length; i++)
                weaponRelics[i] = null;
        }
        if (categoryRelics != null)
        {
            for (int i = 0; i < categoryRelics.Length; i++)
                categoryRelics[i] = null;
        }
    }
    
    #endregion
}