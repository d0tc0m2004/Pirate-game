# Tactical Game Database

This document contains a comprehensive database of all methods, systems, and connections in the Pirate/Tactical game codebase.

---

## 🗺️ Table of Contents

1. [Core Architecture](#core-architecture)
2. [Game Managers](#game-managers)
3. [Unit System](#unit-system)
4. [Combat System](#combat-system)
5. [Equipment & Relics](#equipment--relics)
6. [Status Effects](#status-effects)
7. [Hazard System](#hazard-system)
8. [Card System](#card-system)
9. [Utilities](#utilities)
10. [Events & Game Flow](#events--game-flow)

---

## Core Architecture

### Service Locator Pattern
```
All managers use ServiceLocator for singleton access:
  ServiceLocator.Register(this)   // in Awake()
  ServiceLocator.Get<ManagerType> // to access other managers
  ServiceLocator.Unregister<T>()  // in OnDestroy()
```

### Team Enum
- `Team.Player` - Player controlled units
- `Team.Enemy` - Enemy/AI controlled units

### Game Flow Sequence
1. Character Creation → UnitData generation
2. Equipment Setup → FlexibleUnitEquipment with relics
3. Deployment Phase → Manual placement on grid
4. Chest Spawn → Dead Mans Locker spawns loot
5. Enemy Spawn → Enemies appear on their side
6. Hazards → Random hazards generated
7. Battle → Combat begins

---

## Game Managers

### BattleManager
**Purpose:** Controls overall battle flow and turn-based gameplay.

**Methods:**
- `OnBattleStart()` - Initialize battle state
- `IsBattleActive = bool` - Property to track battle state
- **Damage Calculator Methods:**
  - `CalculateDamage()` - Main damage calculation logic
    - Applies weapon stat
    - Applies proficiency multiplier
    - Applies status modifiers
    - Applies relic passives
  - `CalculateStatusEffectDamage()` - Calculates status effect damage
  - `GetWeaponStatMultiplier()` - Returns weapon stat bonus
  - `ApplyProficiencyMultiplier()` - Applies proficiency bonus
  - `ApplyStatusModifiers()` - Applies active status effect modifiers
  - `GetRelicPassives()` - Retrieves active relic passive effects

### CharacterCreationManager
**Purpose:** Manages character creation and unit generation screen.

**Methods:**
- `GenerateUnit(panel, team)` - Generate stats for selected role
- `GenerateAllUnits()` - Randomize and generate all units
- `RandomizeAndGenerate(panels, team)` - Shuffle roles to avoid duplicates
- `UpdatePanelUI(panel, data)` - Update stat display UI
- `OnStartGameClicked()` - Hide creation canvas, show equipment UI
- `OnEquipmentBackClicked()` - Close equipment UI, show creation
- `OnEquipmentStartBattle(playerUnits, enemyUnits)` - Close equipment, go to deployment
- `GoToDeployment(playerUnits, enemyUnits)` - Transition to deployment phase
- `SetupEquipmentUI()` - Initialize equipment UI builder

### DeploymentManager
**Purpose:** Manages unit deployment phase before battle.

**Methods:**
- `StartManualDeployment(playerUnits, enemyUnits)` - Begin deployment phase
- `FinishDeploymentAndStartBattle()` - End deployment, start battle sequence
- `HandleMouseInteraction()` - Process mouse input during deployment
- `HandleLeftClick(cell)` - Place, move, or select units
- `HandleRightClick()` - Deselect unit
- `TrySelectUnit(unit)` - Select a player unit for moving
- `MoveSelectedUnit(targetCell)` - Move selected unit to new position
- `SpawnUnit(cell, data)` - Instantiate and setup new unit
- `SetupUnitEquipment(unitObj, data)` - Setup flexible equipment with relics
- `SetupCardDeck(unitObj)` - Initialize card deck manager
- `SpawnEnemyUnits()` - Spawn enemy units after player deployment
- `GetValidEnemySpawnCells()` - Find valid spawn spots for enemies
- `HighlightCell(cell)` - Visual feedback for hover state
- `IsValidPlacement(cell)` - Check if placement is valid
- `UpdateDeploymentButtonVisibility()` - Toggle finish button

### EnemyManager
**Purpose:** Spawns enemy units (for non-manual deployment).

**Methods:**
- `SpawnEnemies()` - Spawn random number of enemy units
- `SpawnSingleUnit(prefabToSpawn)` - Place a single enemy unit

### EnergyManager
**Purpose:** Manages player energy pool and energy-based actions.

**Methods:**
- `EnergyPool = int` - Current energy value
- `MaxEnergyPool = int` - Maximum energy capacity
- `EnergyCost = int` - Cost for specific actions
- Energy regenerates each turn
- Actions like playing cards and movement consume energy

### DeadMansLockerManager
**Purpose:** Manages Dead Mans Locker chests that spawn loot during deployment.

**Methods:**
- `SpawnLockers()` - Spawn loot chests at deployment end
- Chests contain weapons and relics

### HazardManager
**Purpose:** Generates and manages hazard effects on the grid.

**Methods:**
- `GenerateRandomHazards()` - Create random hazards at battle start
- Hazard types include storms, fog, and other environmental effects
- Hazards can impede movement or provide status bonuses

### EquipmentUIBuilder
**Purpose:** UI builder for equipment management screen.

**Methods:**
- `Open(playerUnits, enemyUnits)` - Show equipment screen with units
- `Close()` - Hide equipment screen
- `onBackClicked` - Callback when clicking back
- `onStartBattle` - Callback when starting battle from equipment screen

### DeadMansLocker (ScriptableObject)
**Purpose:** Defines loot contents from Dead Mans Locker chests.

**Fields:**
- WeaponRelic[] lockerWeapons - Array of possible weapons
- Relic[] lockerRelics - Array of possible relics

---

## Unit System

### UnitData (ScriptableObject)
**Purpose:** Defines all stats and equipment for a unit.

**Fields:**
- `unitName` - Display name
- `role` - Unit role (Captain, Quartermaster, etc.)
- `team` - Team assignment
- `health`, `morale`, `buzz`, `power`, `aim`, `tactics`, `skill`, `grit`, `hull`, `speed` - Stats
- `proficiency` - Proficiency multiplier (1-200%)
- `weaponFamily` - Weapon family (Firearms, Blunt, etc.)
- `primaryStat` - Primary stat for this role
- `secondaryStat` - Secondary stat for this role
- `hasTwoPrimaryStats` - Whether unit has two primary stats
- `secondaryPrimaryStat` - Secondary primary stat (for roles like Captain)
- `defaultWeaponRelic` - Default weapon relic
- `weaponRelics` - Array of possible weapon relics
- `categoryRelics` - Array of equipment category relics
- `passiveRelics` - Array of passive relic ScriptableObjects

**Methods:**
- `GetWeaponRelic(index)` - Get weapon relic from slot
- `GetCategoryRelic(index)` - Get category relic from slot
- `GetWeaponFamilyDisplayName()` - Get display name of weapon family

### UnitStatus (MonoBehaviour)
**Purpose:** Runtime state of a unit during battle.

**Fields:**
- `unit` - GameObject reference
- `unitData` - Original unit data
- `health` - Current health
- `maxHealth` - Maximum health
- `morale` - Current morale
- `maxMorale` - Maximum morale
- `buzz` - Current buzz
- `power`, `aim`, `tactics`, `skill`, `grit`, `hull`, `speed` - Runtime stats
- `proficiency` - Runtime proficiency
- `team` - Team assignment
- `isDead` - Death status
- `isDeadMansLockerTarget` - Whether unit is a locker spawn target

**Methods:**
- `Initialize(data)` - Initialize from UnitData
- `TakeDamage(amount)` - Apply damage
- `Heal(amount)` - Apply healing
- `ChangeMorale(amount)` - Modify morale
- `ChangeBuzz(amount)` - Modify buzz
- `Die()` - Mark unit as dead
- `IsDead()` - Check if dead

### UnitAttack (MonoBehaviour)
**Purpose:** Handles weapon attacks and damage calculation.

**Fields:**
- `weaponRelic` - Currently equipped weapon relic
- `attackRange` - Attack range
- `attackCooldown` - Attack cooldown
- `canAttack` - Can attack this turn

**Methods:**
- `SetWeaponRelic(weapon)` - Set weapon relic
- `SetupManagers(gridManager, energyManager)` - Initialize manager references

### CardDeckManager (MonoBehaviour)
**Purpose:** Manages a unit's card deck.

**Methods:**
- Draws cards each turn
- Cards can be played for various effects
- Cards consume energy when played

### FlexibleUnitEquipment (MonoBehaviour)
**Purpose:** Slot-based equipment storage system.

**Fields:**
- WeaponRelics at slots 0-4 (flexible equipment)
- Stores weapons and category-specific relics

**Methods:**
- `EquipWeapon(slot, weapon)` - Equip weapon to slot
- `EquipCategory(slot, relic)` - Equip category relic
- `LogEquipmentState()` - Log current equipment state
- `Initialize(role, weaponFamily)` - Initialize for specific role

---

## Combat System

### DamageCalculator (Static Helper Class)
**Purpose:** Central damage calculation logic.

**Main Method:**
```csharp
public static int CalculateDamage(
    UnitData attacker, 
    UnitData defender, 
    StatType weaponStat, 
    bool isCriticalHit)
```

**Damage Flow:**
1. Get weapon stat value from attacker
2. Apply proficiency multiplier
3. Apply status effect modifiers (Intimidate, etc.)
4. Apply relic passive modifiers (Finesse, etc.)
5. Calculate base damage
6. Apply critical hit modifier
7. Apply defense/armor reduction
8. Clamp to [0, max health]

---

## Equipment & Relics

### WeaponRelic
**Purpose:** Weapon equipment data.

**Fields:**
- `relicName` - Display name
- `relicId` - Unique identifier
- `statType` - Primary weapon stat (Power, Aim, etc.)
- `statValue` - Stat value
- `attackRange` - Attack range
- `attackCooldown` - Cooldown
- `proficiencyCost` - Proficiency cost
- `isMelee` - Whether melee weapon
- `weaponFamily` - Weapon family
- `equipped` - Equipped status
- `scriptableObject` - Full WeaponRelic definition

**Methods:**
- `GetValue()` - Get stat value with proficiency
- `GetDisplayStat()` - Get display-friendly stat string
- `GetWeaponFamilyDisplayName()` - Get family name
- `GetDescription()` - Get relic description

### EquippedRelic (Base Class)
**Purpose:** Base class for all equipped relics.

**Fields:**
- `relicName` - Display name
- `relicId` - Unique identifier
- `category` - Equipment category
- `scriptableObject` - Full relic definition

**Methods:**
- `GetDescription()` - Get description
- `GetWeaponFamilyDisplayName()` - Get family name

### PassiveRelic (Base Class)
**Purpose:** Base class for relics with passive effects.

**Fields:**
- `relicId` - Unique identifier
- `scriptableObject` - Full relic definition
- `isUltimate` - Whether ultimate relic
- `isUnique` - Whether unique relic

### PassiveRelicManager (MonoBehaviour)
**Purpose:** Registers and manages passive relic effects.

**Fields:**
- `RelicIdToPassive` - Dictionary of relic ID to passive effect

**Methods:**
- `RegisterPassiveEffect(relicId, passive)` - Register passive effect
- `GetPassiveEffect(relicId)` - Get passive effect by ID
- Called in `Awake()` to register all equipped passives

### RelicData (ScriptableObject)
**Purpose:** Defines full relic data including passives.

**Fields:**
- `relicId` - Unique identifier
- `relicName` - Display name
- `relicDescription` - Description text
- `relicCategory` - Equipment category
- `isMelee` - Whether melee weapon
- `isUltimate` - Whether ultimate relic
- `isUnique` - Whether unique relic
- `statType` - Weapon stat type
- `statValue` - Stat value
- `attackRange` - Attack range
- `attackCooldown` - Cooldown
- `proficiencyCost` - Proficiency cost
- `proficiencyBonus` - Proficiency bonus
- `scriptableObject` - Reference to relic ScriptableObject

**Methods:**
- `GetDescription()` - Get full description with passives
- `GetPassiveEffects()` - Get all passive effects
- `GetPrimaryPassive()` - Get primary passive effect
- `GetSecondaryPassive()` - Get secondary passive effect

---

## Status Effects

### StatusEffectManager (MonoBehaviour)
**Purpose:** Manages active status effects on a unit.

**Fields:**
- `statusEffectStacks` - Dictionary of effect type to stack count
- `statusEffectModifiers` - Dictionary of effect type to modifier

**Methods:**
- `ApplyStatusEffect(statusEffect)` - Apply a status effect
- `RemoveStatusEffect(statusEffectType)` - Remove a status effect type
- `RemoveStatusEffectInstance(statusEffectType)` - Remove one instance
- `GetStatusEffectModifier(type)` - Get current modifier for a type
- `GetTotalStatusEffectModifier(type)` - Get total modifier

### StatusEffect (ScriptableObject)
**Purpose:** Defines a status effect type and its properties.

**Fields:**
- `statusEffectId` - Unique identifier
- `statusEffectName` - Display name
- `statusEffectDescription` - Description text
- `stackCount` - Current stack count
- `duration` - Duration in turns
- `modifierType` - Type of modifier applied
- `modifierValue` - Modifier value
- `isInstant` - Whether instant effect

**Methods:**
- `GetDescription()` - Get formatted description
- `Apply()` - Apply the effect
- `ApplyModifier(unit, value)` - Apply modifier to unit

### StatusEffectApplicator (Utility Class)
**Purpose:** Applies status effects from relics to units.

**Methods:**
- `ApplyStatusEffectsToUnit(relic, unit)` - Apply all status effects from a relic to a unit
- Applies each effect with appropriate stack count
- Handles duration and modifiers

### StatusEffectTypes
**Common Status Effects:**
- `Buzz` - Stun effect
- `MoraleBoost` - Morale increase
- `MoralePenalty` - Morale decrease
- `Intimidate` - Defense reduction
- `MoraleStabilize` - Prevent morale change
- `MoraleFreeze` - Freeze morale
- `BuzzResistance` - Resistance to buzz
- `MoraleResistance` - Resistance to morale change
- `HealthRegen` - Health regeneration

---

## Hazard System

### HazardData (ScriptableObject)
**Purpose:** Defines a hazard type and its effects.

**Fields:**
- `hazardId` - Unique identifier
- `hazardName` - Display name
- `hazardDescription` - Description text
- `statusEffect` - Primary status effect to apply
- `duration` - Duration in turns
- `team` - Affects Player or Enemy side
- `areaOfEffect` - Radius of effect

### HazardManager
**Purpose:** Generates and manages hazards on the grid.

**Methods:**
- `GenerateRandomHazards()` - Create random hazards at battle start
- Hazards appear on player or enemy side
- Apply status effects to units in area of effect

---

## Card System

### CardPlayabilityChecker (Utility Class)
**Purpose:** Determines if a card can be played.

**Methods:**
- `CanCardBePlayed(card)` - Check if card can be played
- `CanCardBePlayedWithEnergy(card, energy)` - Check with energy requirement
- Checks card requirements against current state

---

## Utilities

### TargetFinder (Utility Class)
**Purpose:** Find valid attack targets for a unit.

**Methods:**
- `FindTargets(attacker, attackRange, teamFilter)` - Find valid targets
- Filters by team, range, and other criteria

---

## Events & Game Flow

### GameEvents
**Purpose:** Event system for game state changes.

**Events:**
- `DeploymentStart` - Triggered when deployment begins
- `DeploymentEnd` - Triggered when deployment ends
- Other game state events

---

## Unit Roles

### UnitRole Enum
- Captain
- Quartermaster
- Boatswain
- Shipwright
- Helmsmaster
- MasterGunner
- MasterAtArms
- Navigator
- Surgeon
- Cook
- Swashbuckler
- Deckhand

### Role Display Names
- MasterGunner → "Master Gunner"
- MasterAtArms → "Master-at-Arms"
- Others → ToString()

### Role Stat Assignments
Each role has:
- Primary stat
- Secondary stat
- Weapon family
- Proficiency range
- Health/morale/buzz distribution

---

## Status Effect Categories

### Morale Effects
- `MoraleBoost` - Increases morale
- `MoralePenalty` - Decreases morale
- `MoraleStabilize` - Prevents morale change
- `MoraleFreeze` - Freezes morale

### Buzz Effects
- `Buzz` - Applies stun
- `BuzzResistance` - Reduces stun chance

### Health Effects
- `HealthRegen` - Regenerates health over time

### Defense Effects
- `Intimidate` - Reduces defense
- `DefenseBonus` - Increases defense

---

## Equipment Categories

### Equipment Type Enum
- EquipmentType.Weapon - Weapons
- EquipmentType.Plate - Armor
- EquipmentType.Grappling - Grappling hooks
- EquipmentType.Pistols - Pistols
- EquipmentType.Cannons - Cannons
- EquipmentType.Shotguns - Shotguns

### Category-to-Equipment Mapping
- Weapon → Weapons
- Plate → Armor
- Grappling → Grappling hooks
- Pistols → Pistols
- Cannons → Cannons
- Shotguns → Shotguns

---

## Deployment Sequence

```
1. Character Creation Phase
   ├─ GenerateUnitData for each unit
   └─ EquipmentUI shown for equipment selection

2. Deployment Phase
   ├─ Player places units manually
   ├─ Dead Mans Locker spawns chests
   ├─ EnemyManager spawns enemies
   └─ HazardManager generates hazards

3. Battle Phase
   ├─ Turns begin
   ├─ Units take actions
   └─ Combat resolves
```

---

## Key Connections

### Character Creation → Equipment
- CharacterCreationManager generates UnitData
- EquipmentUIBuilder handles equipment selection
- OnStartGameClicked() → Open equipment UI
- OnEquipmentStartBattle() → Go to deployment

### Deployment → Battle
- DeploymentManager.FinishDeploymentAndStartBattle()
  ├─ SpawnLockers()
  ├─ SpawnEnemyUnits()
  ├─ battleManager.IsBattleActive = true
  ├─ turnManager.StartGameLoop()
  └─ hazardManager.GenerateRandomHazards()

### Unit → Components
- UnitGameObject has:
  ├─ UnitStatus (health, stats)
  ├─ UnitAttack (weapon attacks)
  ├─ StatusEffectManager (status effects)
  ├─ FlexibleUnitEquipment (slots 0-4)
  ├─ PassiveRelicManager (passive effects)
  └─ CardDeckManager (card deck)

### Damage Flow
```
UnitAttack attacks
  ↓
BattleManager.CalculateDamage()
  ↓
1. Get weapon stat
2. Apply proficiency
3. Apply status modifiers
4. Apply relic passives
5. Calculate final damage
6. Apply to target
```

---

## Notes

- All managers use ServiceLocator for singleton access
- FlexibleUnitEquipment uses slot-based (not category-based) storage
- Status effects have stacks and durations
- Damage calculation applies multiple modifiers in sequence
- Hazards are generated randomly at battle start
- Equipment can be selected before deployment