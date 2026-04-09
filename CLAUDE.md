# Pirate Tactical Game - Project Guide

## Overview
Unity C# tactical pirate-themed game with grid-based combat, deck/card system, and deep equipment mechanics.

## Namespace
All code uses `TacticalGame.*` namespaces (e.g., `TacticalGame.Equipment`, `TacticalGame.Enums`, `TacticalGame.Units`, `TacticalGame.Combat`, `TacticalGame.Managers`, `TacticalGame.Grid`, `TacticalGame.Hazards`, `TacticalGame.Core`, `TacticalGame.Config`).

## Architecture

### 12 Unit Roles
Captain, Quartermaster, Boatswain, Shipwright, Helmsmaster, MasterGunner, MasterAtArms, Navigator, Surgeon, Cook, Swashbuckler, Deckhand

### 11 Stats
Health, Morale, Buzz, Power, Aim, Tactics, Skill, Proficiency, Grit, Hull, Speed
Each role has primary/secondary stats with Low/Mid/High generation ranges defined in GameConfig.

### Weapon System
- 21 weapon families across 7 subtypes (Slashing, Blunt, Pierce, Stabbing, Shooting, Throwing, Casting)
- Each weapon has: baseDamage, scalingStat (Power/Aim), scalingCoefficient, cardCopies, energyCost, effectType
- WeaponRelic = BaseWeapon + RoleTag + EffectTier (1-3 rarity: Common/Uncommon/Rare)
- Role-specific weapon effects defined in RoleWeaponEffects (36 total: 3 tiers x 12 roles)

### Relic/Equipment System (New)
- 8 categories: Weapon, Boots, Gloves, Hat, Coat, Trinket, Totem, Ultimate, PassiveUnique
- Each category has V1 and V2 variants per role = 192 total relic effects
- Equipment slots: 1 Weapon + 6 Category (Boots/Gloves/Hat/Coat/Trinket/Totem) + Ultimate + PassiveUnique
- Ultimate and PassiveUnique are role-locked (auto-assigned)
- Relics with matching role get Proficiency bonus; non-matching get secondary stat bonus by rarity
- EquippedRelic wraps RelicEffectData with lazy-loading from RelicEffectsDatabase

### Card/Deck System
- Each equipped relic adds cards to unit's deck (cardCopies field)
- Passive relics (Trinket, PassiveUnique) don't add cards
- CardDeckManager handles: draw, shuffle, play, energy costs, status effect modifiers
- Cards cost energy (modified by StatusEffects: cost increase, ranged reduction, free moves)
- Deck resets when all cards spent

### Combat
- Grid-based with targeting (Closest, Furthest, LowestHP, LowestMorale, Random, Manual)
- Area effects: Single, Row, Column, Adjacent, All
- DamageCalculator, InitiativeSystem, StatusEffectManager
- Hazard system: Fire, Trap, Plague, ShiftingSand, Lightning, Cursed, Boulder, Box

### Key Databases (ScriptableObjects in Resources/)
- `WeaponDatabase` - all 21 weapons
- `RoleEffectsDatabase` - weapon relic effects per role (3 tiers each)
- `RelicEffectsDatabase` - all 192 category relic effects (auto-populates if empty)
- `GameConfig` - all balance values, stat ranges, combat formulas

### Key Script Locations
- `Assets/Scripts/Enums/` - UnitRole, StatType, WeaponEnums (WeaponFamily, WeaponSubType, RelicRarity, RelicCategory, WeaponEffectType)
- `Assets/Scripts/Equipment/` - WeaponData, WeaponDatabase, RelicData, JewelData, WeaponRelic, RoleWeaponEffects, RoleEffectsDatabase
- `Assets/Scripts/new/` - RelicEffectData (192 effect types enum), RelicEffectsDatabase, RelicEffectExecutor, EquippedRelic, CardDeckManager, UnitEquipmentUpdated, PassiveRelicManager
- `Assets/Scripts/Units/` - UnitData, UnitStatus, UnitAttack, UnitMovement, StatGenerator
- `Assets/Scripts/Managers/` - BattleManager, TurnManager, EnergyManager, DeploymentManager, EnemyManager
- `Assets/Scripts/Combat/` - DamageCalculator, InitiativeSystem, StatusEffectManager, WeaponRelicEffectHandler
- `Assets/Scripts/Hazards/` - HazardData, HazardInstance, HazardManager

## Conventions
- ScriptableObjects use singleton pattern with `Resources.Load<T>()`
- MonoBehaviours use `GetComponent<T>()` and `ServiceLocator.Get<T>()`
- Events via `GameEvents` static class
- Debug logging uses Unity color tags: `<color=cyan>`, `<color=green>`, etc.
