# Tactical Game Code Database

This document serves as a comprehensive reference for understanding the game's codebase, methods, systems, and their interconnections.

---

## Table of Contents
1. [Core Architecture](#core-architecture)
2. [Game Systems](#game-systems)
3. [Key Classes and Methods](#key-classes-and-methods)
4. [Event Flow and Interactions](#event-flow-and-interactions)
5. [Database Index](#database-index)

---

## Core Architecture

### Unity Structure
- **MonoBehaviour**: All managers and scripts inherit from MonoBehaviour
- **Singleton Pattern**: Used extensively (Instance property with static access)
- **ServiceLocator**: Static accessor for game services

### Namespace Organization
```
TacticalGame/
├── Core/           # Core game mechanics
├── Units/          # Unit-related scripts
├── Equipment/      # Relic/equipment systems
├── Effects/        # Status effects and effects handlers
├── Managers/       # Game managers
├── Grid/           # Grid and cell systems
├── Enums/          # Enum definitions
```

### Singleton Pattern
```csharp
private static ClassName _instance;
public static ClassName Instance
{
    get
    {
        if (_instance == null)
        {
            _instance = FindFirstObjectByType<ClassName>();
            // Auto-create if null
        }
        return _instance;
    }
}
```

---

## Game Systems

### 1. Character Creation System
**Class**: `CharacterCreationManager`

**Responsibilities**:
- Manage character creation flow (Create -> Select -> Deploy)
- Handle unit selection from pool
- Coordinate with DeploymentManager after selection

**Events**:
- `OnCharacterCreationStarted`
- `OnCharacterCreated`
- `OnCharacterSelected`

**Key Methods**:
- `CreateCharacter()` - Creates a new character from the pool
- `SetSelectedCharacter(string name)` - Selects a character from creation
- `OnCharacterDeployed()` - Handles deployment completion

### 2. Deployment System
**Class**: `DeploymentManager`

**Responsibilities**:
- Manage deployment grid
- Handle unit placement
- Coordinate with BattleManager

**Grid Flow**:
1. `GetDeployableGrid()` - Get deployment grid reference
2. `SelectDeployableGrid()` - UI handles selection
3. `GetGridCellAtMouse()` - Raycast for unit placement
4. `StartPlacement()` - Begin placement animation
5. `DeployAtCell(gridCell, unit)` - Place unit
6. `CompleteDeployment()` - End placement

### 3. Enemy Spawning System
**Class**: `EnemyManager`

**Responsibilities**:
- Spawn enemy teams
- Handle enemy AI and wave management
- Coordinate with `BattleManager` for turn-based logic

**Methods**:
- `SpawnTeam()` - Spawn a team of enemies
- `SpawnWave()` - Spawn a specific wave
- `SpawnRandomEnemy()` - Spawn random enemy from pool
- `GetSpawnLocation()` - Get spawn grid locations

**Spawning Process**:
1. `SpawnTeam()` calls `SpawnWave()`
2. `SpawnWave()` calls `SpawnRandomEnemy()` for each enemy
3. Each enemy is placed at spawn locations
4. Events triggered: `OnEnemySpawned`, `OnWaveSpawned`

### 4. Battle Management
**Class**: `BattleManager`

**Responsibilities**:
- Main battle orchestrator
- Initialize battle state
- Handle turn-based combat
- Manage energy system
- Coordinate all game systems

**Initialization**:
```csharp
void OnEnable()
{
    // Subscribe to GameEvents
    // Create managers (Energy, Grid, UnitSpawning, DeadMansLocker, Enemy)
    // Subscribe to manager events
    // Initialize battle state
}
```

**Turn Flow**:
1. `OnPlayerTurnStart()` - Reset hand, check surrender status, heal
2. `OnPlayerTurnEnd()` - Discard non-stowed cards
3. Enemy actions (handled via EventSystem or directly in BattleManager)

### 5. Energy System
**Class**: `EnergyManager`

**Responsibilities**:
- Track current and max energy
- Handle energy regeneration
- Prevent over-spending

**Key Methods**:
- `SetMaxEnergy(int value)` - Set max energy
- `SetCurrentEnergy(int value)` - Set current energy
- `IncreaseEnergy(int amount)` - Increase energy
- `AddEnergy()` - Regenerate energy
- `SpendEnergy(int amount)` - Spend energy (returns bool)
- `HasEnergy(int amount)` - Check if energy available
- `TrySpendEnergy(int amount)` - Try to spend (safe)
- `ResetToMax()` - Reset to max (end of turn)

**Energy Flow**:
- Start: `currentEnergy = 0`, `maxEnergy = 3`
- Each action: `TrySpendEnergy(cost)` 
- End of turn: `ResetToMax()`

### 6. Dead Man's Locker System
**Class**: `DeadMansLocker`, `DeadMansLockerManager`

**Responsibilities**:
- Manage locker spawning at specific wave
- Handle locker contents (random rewards)
- Coordinate with `BattleManager`

**Locker Contents** (`DeadMansLocker`):
- `relics`: List of relics
- `trinkets`: List of trinkets
- `ultimateRelics`: List of ultimate relics
- `cardRelics`: List of card relics
- `weaponRelics`: List of weapon relics
- `equipment`: Flexible equipment slots

**Spawning Flow**:
1. `BattleManager.OnPlayerTurnEnd()` checks if `IsLockerWave`
2. If locker wave, calls `LockerManager.SpawnLocker()`
3. `SpawnLocker()` calls `SpawnRandomRelic()` multiple times
4. Each relic creation triggers events

### 7. Grid System
**Class**: `GridManager`, `GridCell`

**Responsibilities**:
- Manage battle grid
- Handle movement and positioning
- Coordinate with `UnitStatus` for pathfinding

**Grid Properties**:
- `grid` - The grid component
- `gridCellObjects` - List of grid cell GameObjects
- `cells` - List of grid cell references
- `isLocked` - Whether grid is locked for movement
- `turn` - Current turn

**Cell Properties** (`GridCell`):
- `transform` - Cell's transform
- `gridPosition` - Grid coordinate
- `isOccupied` - Whether occupied
- `isHazard` - Whether hazard exists
- `isTargetable` - Whether can be targeted
- `targetUnit` - Currently targeted unit

### 8. Unit Status System
**Class**: `UnitStatus`

**Responsibilities**:
- Track unit state (position, health, energy)
- Handle card management
- Coordinate with `BattleDeckManager`

**Properties**:
- `unitName` - Unit name
- `unitPrefab` - Unit prefab reference
- `position` - Current grid position
- `currentHp`, `maxHp` - Health values
- `currentEnergy`, `maxEnergy` - Energy values
- `isDeployed` - Whether deployed
- `team` - Team (Player/Enemy/Neutral)
- `hasSurrendered` - Whether surrendered
- `isMoving` - Whether currently moving

**Card Methods**:
- `GetDeckManager()` - Get deck manager
- `GetCardByCategory(category)` - Get card by category
- `DrawCard()` - Draw a card
- `DiscardCard(card)` - Discard a card
- `StowCard(card)` - Stow a card
- `PlayCard(card)` - Play a card

### 9. Card System
**Class**: `BattleCard`

**Responsibilities**:
- Represent a playable card from deck
- Track card state (in hand, stowed, discarded)
- Execute card effects

**Properties**:
- `cardId` - Unique card ID
- `cardName` - Display name
- `ownerUnit` - Unit that owns the card
- `sourceRelic` / `sourceWeaponRelic` - Source relic
- `category` - Relic category
- `roleTag` - Unit role
- `energyCost` - Energy cost to play
- `effectType` - Effect type
- `description` - Card description
- `isStowed` - Whether stowed

**Target Types** (`CardTargetType`):
- `None` - No target needed
- `Tile` - Target a grid tile
- `Ally` - Target an allied unit
- `Enemy` - Target an enemy unit
- `AdjacentEnemy` - Target adjacent enemy
- `RangedEnemy` - Target enemy in range
- `AnyUnit` - Target any unit

### 10. Status Effect System
**Class**: `StatusEffectManager`

**Responsibilities**:
- Manage active status effects
- Handle effect application and removal
- Coordinate effect timing

**Effect Components**:
- `effectType` - Type of effect
- `targetUnit` - Target unit
- `targetCell` - Target cell
- `duration` - Duration in turns
- `isTargetingUnit` - Whether targets unit
- `isTargetingTile` - Whether targets tile

**Effect Application**:
```csharp
var effect = new StatusEffect
{
    effectType = effectType,
    targetUnit = targetUnit,
    targetCell = targetCell,
    duration = duration
};

StatusEffects.Add(effect);
EffectSystem.OnEffectApplied?.Invoke(effect);
```

**Effect Removal**:
```csharp
public void ApplyEffect(StatusEffect effect)
{
    statusEffects.Add(effect);
    EffectSystem.OnEffectApplied?.Invoke(effect);
    
    // Update timers
    OnEffectTimersChanged?.Invoke();
}
```

### 11. Jewel System
**Class**: `JewelData`

**Responsibilities**:
- Manage jewel pool
- Handle jewel generation and consumption
- Coordinate with `RelicDatabase`

**Properties**:
- `jewelType` - Jewel type (Ruby, Sapphire, Diamond, etc.)
- `quantity` - Current quantity
- `baseQuantity` - Base quantity
- `maxQuantity` - Maximum quantity
- `isSpending` - Whether spending jewel
- `spendAmount` - Amount to spend

**Jewel Types**:
- `Ruby` - Base jewel
- `Sapphire` - Sapphire jewel
- `Diamond` - Diamond jewel
- `Emerald` - Emerald jewel
- `Amethyst` - Amethyst jewel
- `Peridot` - Peridot jewel
- `Opal` - Opal jewel
- `Turquoise` - Turquoise jewel
- `Jade` - Jade jewel
- `Topaz` - Topaz jewel

### 12. Hazard System
**Class**: `HazardData`, `HazardManager`, `HazardDataList`

**Responsibilities**:
- Manage hazard tiles
- Handle hazard effects (rum, curse, disable)
- Coordinate with `GridManager` for tile placement

**Hazard Types**:
- `Rum` - Rum bottle
- `RumCurse` - Rum curse
- `Disable` - Disable effect

**Hazard Application**:
```csharp
public enum HazardType
{
    Rum,
    RumCurse,
    Disable
}

public enum HazardLevel
{
    Level1,
    Level2,
    Level3,
    Level4,
    Level5
}
```

### 13. Initiative System
**Class**: `InitiativeSystem`

**Responsibilities**:
- Calculate and manage initiative order
- Coordinate turn order
- Handle initiative changes

**Initiative Types**:
- `Normal` - Standard initiative
- `Boosted` - Boosted initiative
- `Penalized` - Penalized initiative

### 14. Relic Database
**Class**: `RelicEffectsDatabase`, `RoleEffectsDatabase`

**Responsibilities**:
- Store relic data
- Provide effect lookups
- Coordinate with `CharacterCreationManager`

**RelicCategory**:
```
0 - Weapon
1 - Boots
2 - Gloves
3 - Hat
4 - Coat
5 - Totem
6 - Trinket
7 - Ultimate
8 - PassiveUnique
```

**RoleTag**:
- `Captain`
- `Pirate`
- `Sailor`
- `Crew`

### 15. EventSystem
**Class**: `EventSystem`

**Responsibilities**:
- Manage event registration and invocation
- Coordinate cross-system communication

**Event System Flow**:
```csharp
public delegate void EventDelegate();
public static event EventDelegate OnEventName;

// Subscribe
OnEventName += Handler;
OnEventName -= Handler;

// Invoke
OnEventName?.Invoke();
```

---

## Key Classes and Methods

### BattleManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `OnEnable()` | - | - | Initialize battle |
| `OnDisable()` | - | - | Cleanup |
| `OnPlayerTurnStart()` | - | - | Player turn start |
| `OnPlayerTurnEnd()` | - | - | Player turn end |
| `InitializeBattleState()` | - | - | Set up battle state |
| `AddEnergy()` | - | - | Regenerate energy |
| `ResetEnergy()` | - | - | Reset energy |
| `OnUnitDeselected()` | - | - | Handle unit deselection |
| `OnRelicSpawned()` | - | - | Handle relic spawn |
| `OnRelicDestroyed()` | - | - | Handle relic destroy |
| `SurrenderUnit()` | `unit` | bool | Surrender unit |
| `SurrenderAllPlayerUnits()` | - | - | Surrender all players |

### BattleDeckManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `BuildDeck()` | `playerUnits` | - | Build shared deck |
| `ShuffleDeck()` | - | - | Shuffle deck |
| `ResetDeck()` | - | - | Reset deck |
| `DrawToFillHand()` | - | - | Draw to hand size |
| `DrawOneCard()` | - | bool | Draw one card |
| `DrawSpecificCard()` | `card` | bool | Draw specific card |
| `DiscardNonStowedCards()` | - | - | Discard at end of turn |
| `PlayCard()` | `card`, `target`, `targetCell` | bool | Play card |
| `PlaySelectedCard()` | `target`, `targetCell` | bool | Play selected card |
| `StowCard()` | `card` | bool | Stow card |
| `DiscardAndDraw()` | `card` | bool | Discard and draw |
| `ForceDiscardCard()` | `card` | bool | Force discard |
| `GetCardsForUnit()` | `unit` | List\<BattleCard> | Get unit's cards |

### CharacterCreationManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `CreateCharacter()` | - | `UnitStatus` | Create from pool |
| `SelectCharacter(string name)` | `name` | `UnitStatus` | Select by name |
| `SetSelectedCharacter()` | `name` | - | Set selected |
| `OnCharacterDeployed()` | - | - | Handle deployment |
| `GetCharacterSelectionUI()` | - | `UI` | Get selection UI |

### DeploymentManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetDeployableGrid()` | - | `Grid` | Get deployment grid |
| `SelectDeployableGrid()` | - | `UI` | Select grid |
| `GetGridCellAtMouse()` | - | `GridCell` | Get cell at mouse |
| `StartPlacement()` | - | - | Start placement |
| `DeployAtCell()` | `gridCell`, `unit` | - | Deploy unit |
| `CompleteDeployment()` | - | - | End placement |

### EnemyManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SpawnTeam()` | - | void | Spawn team |
| `SpawnWave()` | - | void | Spawn wave |
| `SpawnRandomEnemy()` | - | `UnitStatus` | Spawn random enemy |
| `GetSpawnLocation()` | - | `List<GridCell>` | Get locations |

### EnergyManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SetMaxEnergy()` | `value` | - | Set max energy |
| `SetCurrentEnergy()` | `value` | - | Set current energy |
| `IncreaseEnergy()` | `amount` | - | Increase energy |
| `AddEnergy()` | - | - | Regenerate |
| `SpendEnergy()` | `amount` | bool | Spend energy |
| `HasEnergy()` | `amount` | bool | Check energy |
| `TrySpendEnergy()` | `amount` | bool | Try to spend |
| `ResetToMax()` | - | - | Reset to max |

### DeadMansLockerManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SpawnLocker()` | - | void | Spawn locker |
| `SpawnRandomRelic()` | - | void | Spawn random relic |
| `GetLockerContents()` | - | `DeadMansLocker` | Get contents |

### GridManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetCell()` | `x`, `y` | `GridCell` | Get cell |
| `IsOccupied()` | `x`, `y` | bool | Check occupation |
| `IsWalkable()` | `x`, `y` | bool | Check walkable |
| `SetWalkable()` | `x`, `y`, `walkable` | - | Set walkable |

### GridCell.cs
| Property | Type | Description |
|----------|------|-------------|
| `transform` | Transform | Cell transform |
| `gridPosition` | Vector2Int | Grid position |
| `isOccupied` | bool | Occupied state |
| `isHazard` | bool | Hazard state |
| `isTargetable` | bool | Targetable state |
| `targetUnit` | UnitStatus | Target unit |

### RelicEffectsDatabase.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `GetEffect()` | `category`, `roleTag` | `RelicData` | Get relic data |
| `GetEffectsByCategory()` | `category` | List\<RelicData> | Get by category |
| `GetEffectsByRole()` | `roleTag` | List\<RelicData> | Get by role |

### StatusEffectManager.cs
| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `ApplyEffect()` | `effect` | void | Apply effect |
| `RemoveEffect()` | `effect` | void | Remove effect |
| `GetEffect()` | `unit`, `effectType` | `StatusEffect` | Get effect |
| `ClearEffects()` | - | - | Clear all |

---

## Event Flow and Interactions

### Battle Initialization Flow
```
1. GameEvents.OnBattleStart -> BattleManager.OnEnable()
2. BattleManager creates managers (Energy, Grid, UnitSpawning, etc.)
3. BattleManager subscribes to manager events
4. GameEvents.OnCharacterCreationStarted -> CharacterCreationManager.CreateCharacter()
5. CharacterCreationManager.OnCharacterCreated -> DeploymentManager.SelectDeployableGrid()
6. DeploymentManager.StartPlacement()
7. DeploymentManager.DeployAtCell()
8. DeploymentManager.CompleteDeployment()
9. DeploymentManager.OnUnitDeployed -> BattleManager.InitializeBattleState()
10. BattleManager.OnPlayerTurnStart()
11. BattleManager adds energy, initializes deck
12. EnemyManager.SpawnTeam()
13. DeadMansLockerManager.SpawnLocker() (if locker wave)
```

### Turn Flow
```
Player Turn:
1. GameEvents.OnPlayerTurnStart -> BattleManager.OnPlayerTurnStart()
2. BattleManager resets hand, checks surrender status
3. EnergyManager.AddEnergy()
4. BattleDeckManager.DrawToFillHand()
5. Player plays cards
6. GameEvents.OnPlayerTurnEnd -> BattleManager.OnPlayerTurnEnd()
7. BattleManager discards non-stowed cards
8. EnergyManager.ResetToMax()

Enemy Turn:
1. Enemy actions handled via EventSystem or directly
```

### Card Play Flow
```
1. Player selects unit
2. Player selects card from hand
3. BattleDeckManager.PlaySelectedCard()
4. BattleDeckManager.PlayCard()
5. Check unit ownership
6. Check energy (EnergyManager.TrySpendEnergy())
7. ExecuteCard(card, target, targetCell)
8. ExecuteWeaponCard() or RelicEffectExecutor.Execute()
9. Move card to discard pile
10. OnHandChanged event
```

### Deck Building Flow
```
1. BattleManager.BuildDeck(playerUnits)
2. Iterate through all player units
3. Add cards from FlexibleUnitEquipment or UnitEquipmentUpdated
4. Add weapon relic cards
5. Add category relic cards
6. Track passive relics
7. ShuffleDeck()
8. OnDeckBuilt event
```

### Deck Operations Flow
```
Draw Card:
1. DrawOneCard()
2. Check if deck empty
3. If empty, check discard pile
4. If discard pile has cards, ResetDeck()
5. Move card from deck to hand
6. OnCardDrawn, OnHandChanged events

Discard:
1. DiscardNonStowedCards()
2. Filter hand for non-stowed cards
3. Move cards to discard pile
4. OnCardDiscarded, OnHandChanged events

Stow:
1. StowCard(card)
2. Check if stowed
3. Check energy
4. Spend energy
5. Mark card as stowed
6. OnCardStowed, OnHandChanged events

Play:
1. PlayCard(card)
2. Execute card effect
3. Move to discard pile
4. OnCardPlayed, OnHandChanged events
```

---

## Database Index

### Manager Classes
| Manager | Class | Primary Responsibility |
|---------|-------|------------------------|
| Battle | BattleManager | Battle orchestration |
| Deck | BattleDeckManager | Shared deck management |
| Character Creation | CharacterCreationManager | Character creation |
| Deployment | DeploymentManager | Unit deployment |
| Enemy | EnemyManager | Enemy spawning |
| Energy | EnergyManager | Energy tracking |
| Dead Man's Locker | DeadMansLockerManager | Locker spawning |
| Grid | GridManager | Grid management |
| Status Effect | StatusEffectManager | Status effects |
| Jewel | JewelData | Jewel tracking |
| Hazard | HazardManager | Hazard management |
| Initiative | InitiativeSystem | Initiative calculation |
| Relic Database | RelicEffectsDatabase | Relic data |

### Event System
| Event | Delegate | Invoked By |
|-------|----------|------------|
| OnCharacterCreationStarted | - | CharacterCreationManager |
| OnCharacterCreated | - | CharacterCreationManager |
| OnCharacterSelected | - | CharacterCreationManager |
| OnBattleStart | - | BattleManager |
| OnBattleEnd | - | BattleManager |
| OnPlayerTurnStart | - | BattleManager |
| OnPlayerTurnEnd | - | BattleManager |
| OnUnitDeselected | - | BattleManager |
| OnUnitSelected | - | BattleManager |
| OnUnitDeployed | - | DeploymentManager |
| OnDeckBuilt | - | BattleDeckManager |
| OnDeckShuffled | - | BattleDeckManager |
| OnDeckReset | - | BattleDeckManager |
| OnCardDrawn | - | BattleDeckManager |
| OnCardPlayed | - | BattleDeckManager |
| OnCardDiscarded | - | BattleDeckManager |
| OnCardStowed | - | BattleDeckManager |
| OnHandChanged | - | BattleDeckManager |
| OnTurnStartDraw | - | BattleDeckManager |
| OnTurnEndDiscard | - | BattleDeckManager |
| OnEnemySpawned | - | EnemyManager |
| OnWaveSpawned | - | EnemyManager |
| OnRelicSpawned | - | DeadMansLockerManager |
| OnRelicDestroyed | - | DeadMansLockerManager |

### Key Enums
| Enum | Values | Description |
|------|--------|-------------|
| Team | Player, Enemy, Neutral | Unit team |
| RelicCategory | Weapon, Boots, Gloves, etc. | Relic categories |
| RoleTag | Captain, Pirate, Sailor, Crew | Unit roles |
| JewelType | Ruby, Sapphire, Diamond, etc. | Jewel types |
| HazardType | Rum, RumCurse, Disable | Hazard types |
| HazardLevel | Level1-5 | Hazard levels |
| CardTargetType | None, Tile, Ally, etc. | Card targets |
| StatusEffectType | Debuff, Buff, etc. | Effect types |

---

## Summary

This database provides a comprehensive reference for understanding:

1. **Architecture**: Singleton pattern, namespace organization, event-driven design
2. **Systems**: All major game systems (battle, deck, energy, hazards, etc.)
3. **Methods**: Key methods and their purposes
4. **Events**: Event flows and interconnections
5. **Data Flow**: How data flows between systems

Use this database as a quick reference when:
- Understanding how a system works
- Debugging specific issues
- Adding new features
- Understanding cross-system dependencies

---

*Last Updated: $(date)*
*Total Classes: $(count)*
*Total Methods: $(count)*
*Total Events: $(count)*