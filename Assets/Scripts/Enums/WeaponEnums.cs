namespace TacticalGame.Enums
{
    /// <summary>
    /// All weapon families available in the game.
    /// Each unit is locked to one family and can only equip weapons from that family.
    /// </summary>
    public enum WeaponFamily
    {
        // === MELEE (Slashing) ===
        Cutlass,
        Machete,
        Rapier,
        
        // === MELEE (Blunt) ===
        Axe,
        Hammer,
        Anchor,
        Clubs,
        Mace,
        
        // === MELEE (Pierce) ===
        Harpoon,
        Spear,
        BoardingPike,
        
        // === MELEE (Stabbing) ===
        Dagger,
        Dirk,
        
        // === RANGED (Shooting) ===
        Pistol,
        Musket,
        Blunderbuss,
        
        // === RANGED (Throwing) ===
        Grenade,
        Cannonball,
        
        // === RANGED (Casting) ===
        CursedBird,
        CursedMonkey
    }

    /// <summary>
    /// Subcategory of weapon (affects animations and some mechanics).
    /// </summary>
    public enum WeaponSubType
    {
        // Melee
        Slashing,
        Blunt,
        Pierce,
        Stabbing,
        
        // Ranged
        Shooting,
        Throwing,
        Casting
    }

    /// <summary>
    /// Relic rarity tiers.
    /// Affects secondary stat bonus for non-matching relics.
    /// </summary>
    public enum RelicRarity
    {
        Common,     // +2% secondary stat
        Uncommon,   // +4% secondary stat
        Rare,       // +6% secondary stat
        Unique      // +8% secondary stat
    }

    /// <summary>
    /// Categories of relics (only weapons can duplicate in slots).
    /// </summary>
    public enum RelicCategory
    {
        Weapon,     // Can duplicate, must match weapon family
        Boots,      // Movement, evasion, offensive buffs
        Gloves,     // Utility buffs, offensive abilities
        Hat,        // Resource manipulation, defensive
        Coat,       // Defensive bursts, barriers, rally
        Trinket,    // Passive effects, utility
        Totem       // Cursed artifacts, summons, on-death triggers
    }

    /// <summary>
    /// Special effect types for weapons.
    /// </summary>
    public enum WeaponEffectType
    {
        None,
        
        // Cutlass
        IntimidatingCut,    // Extra morale damage if target below 80% morale
        
        // Machete
        ChopThrough,        // Destroys soft obstacles in row
        
        // Rapier
        PiercingPoint,      // 25% of damage bypasses Hull
        
        // Axe
        Gash,               // Apply Bleed (2 turns): 20 damage at end of round
        
        // Hammer
        Concuss,            // Apply Daze (1 turn): -20% damage next round
        
        // Anchor
        DragDown,           // Pull target 1 tile toward attacker
        
        // Clubs
        Rattle,             // Apply Rattled (1 turn): +10% morale damage taken
        
        // Mace
        Crack,              // Apply Cracked (2 turns): +15% Hull damage taken
        
        // Harpoon
        ReelOut,            // Push target back 1 tile
        
        // Spear
        LineBreak,          // If enemy behind target, deal 40% damage to them too
        
        // Boarding Pike
        KeepBack,           // If enemy behind, prevent target from moving 1 turn
        
        // Dagger
        Finisher,           // +10% damage if target below 50% HP
        
        // Dirk
        FearFactor,         // +10% morale damage if target below 50% morale
        
        // Pistol
        QuickDraw,          // +15% damage if first attack this turn
        
        // Musket
        ArmorPiercing,      // 60% of damage bypasses Hull
        
        // Blunderbuss
        Scattershot,        // Hit nearby enemies for 20% damage
        
        // Grenade
        Shrapnel,           // Apply Marked (2 turns): next 2 hits deal +15% damage
        
        // Cannonball
        HullBreaker,        // +50% damage to Hull, nearby targets take 20% if Hull breaks
        
        // Cursed Bird
        BadOmen,            // Apply Omen (2 turns): +20 morale damage when losing morale
        
        // Cursed Monkey
        Pilfer              // Steal 1 Grog from enemy, or +10 Buzz if none
    }
}