# 🏴‍☠️ Pirate Tactical Game — Complete Codebase Reference

> **Purpose**: This document describes every system, script, enum, data structure, and interaction in the codebase so that any developer or AI model can fully understand the project without reading the source files.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Architecture & Patterns](#2-architecture--patterns)
3. [Namespace & Folder Map](#3-namespace--folder-map)
4. [Enums (Shared Vocabulary)](#4-enums-shared-vocabulary)
5. [Core Infrastructure](#5-core-infrastructure)
6. [Configuration System](#6-configuration-system)
7. [Grid System](#7-grid-system)
8. [Unit System](#8-unit-system)
9. [Equipment & Relic System](#9-equipment--relic-system)
10. [Deck & Card System](#10-deck--card-system)
11. [Combat System](#11-combat-system)
12. [Hazard System](#12-hazard-system)
13. [Manager Layer](#13-manager-layer)
14. [UI System](#14-ui-system)
15. [Camera System](#15-camera-system)
16. [Debug & Editor Tools](#16-debug--editor-tools)
17. [Game Flow (Start to Finish)](#17-game-flow-start-to-finish)
18. [Key Formulas & Balance Constants](#18-key-formulas--balance-constants)
19. [Cross-System Interactions](#19-cross-system-interactions)

---

## 1. Project Overview

This is a **Unity 3D turn-based tactical combat game** with a pirate theme. Two teams of units (Player vs Enemy) are placed on a grid and fight using a **card-based combat system** powered by equipped relics.

**Core gameplay loop:**
1. **Character Creation** — Generate units with randomized roles, stats, and weapons.
2. **Equipment Phase** — Equip relics (weapons + category relics) to unit slots.
3. **Deployment** — Manually place player units on a grid; enemies spawn automatically.
4. **Battle** — Turn-based combat using cards drawn from a shared deck built from all units' equipped relics.

**Tech:** Unity (C#), URP rendering, TextMeshPro for UI.

---

## 2. Architecture & Patterns

### Service Locator (`Core/ServiceLocator.cs`)
- Static dictionary `Dictionary<Type, object>` stores singleton manager references.
- Managers call `ServiceLocator.Register<T>(this)` in `Awake()` and `Unregister<T>()` in `OnDestroy()`.
- Any script accesses managers via `ServiceLocator.Get<T>()` or `ServiceLocator.TryGet<T>()`.
- `ServiceLocatorCleanup` MonoBehaviour handles cleanup on application quit.

### Event System (`Core/GameEvents.cs`)
- Fully static event hub using C# `Action` delegates.
- Categories: Turn events, Unit events, Status Effect events, Combat events, Resource events, Game State events, Dead Man's Locker events, Hazard events.
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- `ClearAllEvents()` nulls every delegate — call on scene unload.

### Key Design Decisions
- **No MonoBehaviour singletons** — uses ServiceLocator instead of `DontDestroyOnLoad`.
- **ScriptableObjects** for data definitions (`GameConfig`, `WeaponData`, `HazardData`, `RelicData`).
- **Static utility classes** for calculations (`DamageCalculator`, `StatGenerator`, `InitiativeSystem`, `TargetFinder`).
- **Component-based units** — each unit GameObject has `UnitStatus`, `UnitMovement`, `UnitAttack`, `StatusEffectManager`, `PassiveRelicManager`, `FlexibleUnitEquipment`, `CardDeckManager`, `AttackAnimator`.

---

## 3. Namespace & Folder Map

| Folder | Namespace | Purpose |
|--------|-----------|---------|
| `Enums/` | `TacticalGame.Enums` | All shared enumerations |
| `Core/` | `TacticalGame.Core` | ServiceLocator, GameEvents |
| `Config/` | `TacticalGame.Config` | GameConfig ScriptableObject |
| `Grid/` | `TacticalGame.Grid` | GridManager, GridCell |
| `Units/` | `TacticalGame.Units` | UnitData, UnitStatus, UnitMovement, UnitAttack, StatGenerator |
| `Combat/` | `TacticalGame.Combat` | DamageCalculator, InitiativeSystem, TargetFinder, AttackAnimator, StatusEffect/Manager, DeadMansLocker/Manager, WeaponRelicEffectHandler |
| `Equipment/` | `TacticalGame.Equipment` | WeaponData, WeaponRelic, RelicData, JewelData, WeaponDatabase, WeaponAssigner, RoleEffectsDatabase, WeaponEffectHandler, WeaponRelicGenerator, UnitEquipment/Data |
| `Deck/` | `TacticalGame.Equipment` | BattleCard, BattleDeckManager, BattleDeckUI, CardUI, CardUIGenerator, CardPlayabilityChecker, PassiveRelicsPanel |
| `new/` | `TacticalGame.Equipment` | FlexibleUnitEquipment, EquippedRelic, PassiveRelicManager, RelicEffectData, RelicEffectExecutor, RelicEffectsDatabase, RelicTargetSelector, CardDeckManager, UnitEquipmentUpdated, RelicTestTracker |
| `Hazards/` | `TacticalGame.Hazards` | HazardData, HazardInstance, HazardManager |
| `Managers/` | `TacticalGame.Managers` | BattleManager, TurnManager, EnergyManager, DeploymentManager, CharacterCreationManager, EnemyManager, EquipmentUIManager, GlobalUIManager |
| `UI/` | `TacticalGame.UI` | DamagePopup, BillBoard, UIManager, UnitWorldUI, UnitIcon, RelicSlotUI, UnitEquipmentCard, UnitListItemUI, JewelPoolItemUI, RelicPoolItemUI, RelicSlotWithJewels |
| `Camera/` | `TacticalGame.Camera` | CameraOrbit |
| `Debug/` | (various) | DebugConsoleLogger, LogCapture |
| `Editor/` | (Editor) | RelicTestTrackerEditor, RoleEffectsGenerator |
| `Settings/` | — | URP render pipeline assets (not code) |

---

## 4. Enums (Shared Vocabulary)

### `StatType` — All unit stats
`Health, Morale, Buzz, Power, Aim, Tactics, Skill, Proficiency, Grit, Hull, Speed`

### `Team`
`Player, Enemy, Neutral`

### `UnitRole` — 12 pirate roles
`Captain, Quartermaster, Boatswain, Shipwright, Helmsmaster, MasterGunner, MasterAtArms, Navigator, Surgeon, Cook, Swashbuckler, Deckhand`

Each role has a **fixed primary stat** (e.g., MasterAtArms → Power, Surgeon → Skill). Captain gets **two random** primary stats.

### `WeaponType`
`Melee, Ranged`

### `WeaponFamily` — 21 weapon families
**Melee (Slashing):** Cutlass, Machete, Rapier
**Melee (Blunt):** Axe, Hammer, Anchor, Clubs, Mace
**Melee (Pierce):** Harpoon, Spear, BoardingPike
**Melee (Stabbing):** Dagger, Dirk
**Ranged (Shooting):** Pistol, Musket, Blunderbuss
**Ranged (Throwing):** Grenade, Cannonball
**Ranged (Casting):** CursedBird, CursedMonkey

### `WeaponSubType`
`Slashing, Blunt, Pierce, Stabbing, Shooting, Throwing, Casting`

### `WeaponEffectType` — 21 weapon-specific effects
Each weapon family has a unique on-hit effect (e.g., Axe → Gash/Bleed, Hammer → Concuss/Daze, Pistol → QuickDraw, Musket → ArmorPiercing).

### `RelicRarity`
`Common (+2%), Uncommon (+4%), Rare (+6%), Unique (+8%)` — secondary stat bonus for non-matching relics.

### `RelicCategory` — 9 equipment categories
`Weapon, Boots, Gloves, Hat, Coat, Trinket, Totem, Ultimate, PassiveUnique`

### `CardTargetType`
`None, Tile, Ally, Enemy, AdjacentEnemy, RangedEnemy, AnyUnit`

---

## 5. Core Infrastructure

### `ServiceLocator` (static class)
- `Register<T>(service)` / `Unregister<T>()` / `Get<T>()` / `TryGet<T>(out T)`
- Registered managers: `GridManager`, `BattleManager`, `TurnManager`, `EnergyManager`, `DeploymentManager`, `EnemyManager`, `GlobalUIManager`, `HazardManager`, `DeadMansLockerManager`

### `GameEvents` (static class)
Key events and their signatures:
- **Turn:** `OnPlayerTurnStart/End`, `OnEnemyTurnStart/End`, `OnRoundStart(int round)`
- **Unit:** `OnUnitSelected(GameObject)`, `OnUnitDeselected`, `OnUnitDeath(GameObject)`, `OnUnitSurrender(GameObject)`, `OnUnitDamaged(GameObject, int)`, `OnUnitHealed(GameObject, int)`, `OnMoraleDamaged(GameObject, int)`, `OnUnitAttack(GameObject attacker, GameObject target)`, `OnUnitMoved(GameObject, GridCell from, GridCell to)`
- **Status:** `OnUnitStunned/Trapped/Cursed/Exposed(GameObject)`
- **Combat:** `OnAttackBlocked(GameObject attacker, GameObject obstacle, bool destroyed)`
- **Resources:** `OnEnergyChanged(int)`, `OnGrogChanged(int)`
- **Game State:** `OnBattleStart/End`, `OnGameEnd(bool playerWon)`, `OnDeploymentStart/End`
- **Lockers:** `OnLockerHit(GameObject, int)`, `OnLockerDestroyed(GameObject)`, `OnLockerTributeChanged(GameObject, float)`, `OnAllLockersDestroyed`
- **Hazards:** `OnUnitEnteredHazard(GameObject, HazardInstance)`, `OnHazardDestroyed(HazardInstance)`

---

## 6. Configuration System

### `GameConfig` (ScriptableObject, singleton via `Resources.Load`)

#### Stat Generation Ranges
Each stat has Low/Mid/High ranges (e.g., Health Low: 510–600, Mid: 600–720, High: 720–840). Primary stats roll High, secondary rolls Mid, everything else rolls Low.

#### Combat Balance Constants
| Constant | Value | Purpose |
|----------|-------|---------|
| `meleeBaseDamage` | 10 | Default melee base |
| `rangedBaseDamage` | 8 | Default ranged base |
| `powerScalingPercent` | 0.03 | +3% dmg per Power point |
| `aimScalingPercent` | 0.03 | +3% dmg per Aim point |
| `tacticsScalingPercent` | 0.04 | +4% potency per Tactics point |
| `skillComboMultiplier` | 0.003 | Combo step per Skill |
| `comboStepMin/Max` | 0.02 / 0.12 | Combo bonus range |
| `maxComboChain` | 6 | Max combo hits |
| `drunkDamageMultiplier` | 0.8 | −20% damage when too drunk |
| `rangedHPMultiplier` | 1.1 | +10% HP dmg for ranged |
| `meleeMoraleMultiplier` | 1.1 | +10% morale dmg for melee |
| `adjacencyCoverReduction` | 0.1 | −10% if target has cover |
| `exposedDamageMultiplier` | 1.2 | +20% dmg to exposed units |
| `focusFireMultipliers` | [0, 0, 0.10, 0.25, 0.45, 0.65] | Morale dmg bonus per stack |

#### Buzz/Rum System
- `buzzPerDrink`: 30, `healthRumRestore`: 20, `moraleRumRestore`: 20
- `buzzDecayPerTurn`: 15, `buzzDecayOnAttack`: 25, `maxBuzz`: 100

#### Swap System
- Cost: 1 energy, cooldown: 3 turns, max 1/round, morale penalty: 15%, min HP to swap: 20%

#### Grit (Damage Reduction)
- `GritFactor = (1−HP%) × 0.50 + Morale% × 0.40`
- `DR = min(40%, GritFactor × Grit/100)`

#### Hull (Armor)
- `MaxHull = 50 + Hull×10`, absorbs up to 30% of incoming damage

#### Dead Man's Locker
- Base 3 pips, max 2 fortify pips, 1–4 lockers per battle, 50% tribute lost on destroy

---

## 7. Grid System

### `GridManager` (MonoBehaviour, registered in ServiceLocator)
- Generates a **random-sized grid** each battle: width 7–15 (odd), height 3–9.
- Grid is split by a **middle column** (blocked/impassable) into Player side (left) and Enemy side (right).
- Creates a visual base plane and individual cell GameObjects.
- Provides coordinate conversion: `WorldToGridPosition()` ↔ `GridToWorldPosition()`.

### `GridCell` (MonoBehaviour, attached to each cell)
- Tracks: position (x,y), occupied status, blocking status, player/enemy side, middle column flag.
- **Hazard support:** `ApplyHazard()` instantiates a hazard prefab as child, `ClearHazard()` removes it.
- **Unit placement:** `PlaceUnit(GameObject)` sets position + marks occupied; `RemoveUnit()` clears.
- **Highlighting:** `SetHighlightColor()`, `ResetHighlight()`, `FlashHighlight()` for visual feedback.
- **Passability:** `CanPlaceUnit()` = not occupied, not blocked, not middle; `IsPassable()` = not blocked, not occupied.

---

## 8. Unit System

### `UnitData` (Serializable data class, NOT a MonoBehaviour)
- Holds all unit info during character creation / between scenes.
- **Basic:** unitName, role, team, weaponType, weaponFamily.
- **Stat tracking:** primaryStat, secondaryPrimaryStat, secondaryStat, hasTwoPrimaryStats.
- **Stat values:** health, morale, grit, buzz, power, aim, proficiency, skill, tactics, speed, hull.
- **Equipment storage:** `weaponRelics[7]` and `categoryRelics[7]` arrays for slot-based relics.
- Methods: `SetStat()`, `GetStat()`, `EquipWeaponRelic()`, `EquipCategoryRelic()`, `GetAllWeaponRelics()`, `GetAllCategoryRelics()`, `ClearSlot()`, `ClearAllEquipment()`.

### `StatGenerator` (static class)
- **Role → Primary stat mapping:** Captain=random, Quartermaster=Morale, Boatswain=Health, Shipwright=Grit, Helmsmaster=Buzz, MasterGunner=Aim, MasterAtArms=Power, Navigator=Tactics, Surgeon=Skill, Cook=Proficiency, Swashbuckler=Speed, Deckhand=Hull.
- **Captain special:** Gets 2 random primary stats (both High range) + 1 random secondary.
- **All others:** Fixed primary (High), random secondary (Mid), rest Low.
- Assigns weapon type (MasterAtArms=Melee only, MasterGunner=Ranged only, others=50/50 random).
- Picks a random weapon from `WeaponDatabase` matching the type, then generates a `WeaponRelic` via `WeaponRelicGenerator`.

### `UnitStatus` (MonoBehaviour — CORE unit component, ~934 lines)
**Required components:** `UnitEquipmentUpdated`, `PassiveRelicManager`, `StatusEffectManager`.

**Identity & Stats:**
- All stats from UnitData, plus derived values: `HPPercent`, `MoralePercent`, `ProficiencyMultiplier`, `IsTooDrunk`.
- Hull pool: `MaxHullPool = baseHull + hull × hullPerPoint`, absorbs damage.
- Speed property dynamically checks for enemy Trinket_V2 passive (−10% speed).

**Damage System (`TakeDamage()`):**
1. Updates focus-fire tracking.
2. Optionally applies curse.
3. Checks adjacency cover.
4. Calls `DamageCalculator.Calculate()` for HP + morale damage with all modifiers.
5. Applies Grit DR → Hull absorption → final HP damage.
6. Spawns `DamagePopup` for HP, Hull, and Morale damage.
7. Fires `GameEvents.TriggerUnitDamaged`.
8. Checks death (HP ≤ 0 → `Die()` → destroys GameObject).

**Environmental damage:** `TakeEnvironmentalDamage()` and `TakeEnvironmentalMoraleDamage()` bypass full calculator — flat damage, no Grit/Hull.

**Surrender System:**
- Dynamic threshold (default 20% morale, can be modified by passives to 10% or 30%).
- `PreventSurrender` status effect can block surrender and restore morale.
- Surrender: turns grey, shows white flag, untags from "Unit", fires event.
- `ClearSurrender()` reverses surrender (for revive effects).

**Rum System:** `DrinkRum("Health"/"Morale")` adds buzz and restores HP/morale. `AddBuzz()`, `ReduceBuzz()`.

**Arrows:** Ranged units have 10 arrows. `UseArrow()` consumes one, `AddArrows()` restocks.

**Turn Lifecycle:**
- `OnTurnStart()`: Clears trap, decays buzz, resets focus fire, clears exposed, reduces swap cooldown, processes status effects.
- `OnTurnEnd()`: Processes stun duration.

### `UnitMovement` (MonoBehaviour)
- Base move range: 3 tiles (Manhattan distance).
- `GetEffectiveMoveRange()`: Applies status effect reductions (Slowed), ally passives (+movement), enemy passives (movement limit).
- `MoveToCell()`: Animated movement via coroutine, fires `GameEvents.TriggerUnitMoved`, checks for hazards on destination tile.
- `ForceMoveTo()`: Instant displacement for knockback effects (checks `CanBeDisplaced()`).
- Free move system: `ShouldCostEnergy()`, `ConsumeFreeMove()`.
- `CanIgnoreObstacles()`: Checks self and nearby ally passives.

### `UnitAttack` (MonoBehaviour)
- Supports **card-based attacks** (primary) and **legacy keyboard attacks** (backward compat).
- `ExecuteCardAttack(WeaponRelic, energyAlreadySpent)`: Main entry point from card system.
- Calculates damage pipeline: Base weapon → Stat scaling → Relic rarity → Relic effect → Weapon effect → Proficiency → Drunk penalty → Hazard bonuses.
- Delays damage application until attack animation completes (melee dash or ranged projectile).
- Post-attack: Applies on-hit effects (`WeaponRelicEffectHandler`, `WeaponEffectHandler`), reduces buzz, marks as attacked, fires event.
- Combo system: `comboCount` increments per attack in a turn, resets at turn start.

---

## 9. Equipment & Relic System

### Data Layer

**`WeaponData`** (ScriptableObject): Defines a weapon — name, family, subType, attackType (melee/ranged), cardCopies, energyCost, baseDamage, scalingStat, scalingCoefficient, targetType, targetArea, effectType + params, icon.

**`WeaponDatabase`** (ScriptableObject singleton): Registry of all `WeaponData` assets. `GetRandomWeaponByType(type)` returns a random weapon matching melee/ranged.

**`RelicData`** (ScriptableObject): Defines a relic — name, category, rarity, roleTag, weaponFamily, level (1–3 = socket count), cardCopies, energyCost, effect description, scaling, base values, icon.

**`RelicEffectData`** (class in `new/`): Runtime data for a relic effect — category, roleTag, effectType (`RelicEffectType` enum), description, energyCost, copies, isPassive flag, and numeric parameters. `GetDisplayName()` generates readable name.

**`RelicEffectsDatabase`** (singleton): Maps `(RelicCategory, UnitRole)` → `RelicEffectData`. Contains the complete table of all ~200+ relic effects. `GetEffect(category, role)` looks up the effect.

**`RoleEffectsDatabase`** (singleton): Maps `(UnitRole, effectTier)` → weapon relic effect data. Used for generating weapon relic effects.

### Runtime Equipment

**`WeaponRelic`** (Serializable class): Combines a `WeaponData` + `UnitRole` + effectTier (1/2/3) + `WeaponRelicEffectData`. Generates a name like "Surgeon Hammer (Uncommon)". Methods: `GetTotalBaseDamage()`, `GetEnergyCost()`, `MatchesFamily()`, `MatchesRole()`.

**`EquippedRelic`** (Serializable class in `new/`): Runtime representation of a non-weapon relic. Holds category, roleTag, and lazy-loads `RelicEffectData` from database. Methods: `GetCopies()`, `GetEnergyCost()`, `IsPassive()`, `MatchesRole()`, `GetEffectType()`.

**`FlexibleUnitEquipment`** (MonoBehaviour in `new/`): The **runtime equipment component** on each unit.
- **7 slots:** [0–4] flexible (weapon OR category relic), [5] Ultimate (auto-assigned by role), [6] Passive (auto-assigned by role).
- `Initialize(role, family)`: Auto-assigns Ultimate + PassiveUnique relics.
- `EquipWeapon(slot, relic)` / `EquipCategory(slot, relic)`: Equip to flexible slots.
- `GetPassiveRelics()`: Returns all passive relics for `PassiveRelicManager`.
- `GetTotalCardCount()`: Sums card copies across all active relics.

**`WeaponRelicGenerator`** (static): `GenerateDefaultWeaponRelic(UnitData)` creates the unit's starting weapon relic by combining their weapon family with their role's effect.

### Effect Execution

**`WeaponEffectHandler`** (static, `Equipment/`): Handles the 21 base weapon effects (e.g., Axe→Gash applies Bleed, Hammer→Concuss applies Daze). `CalculatePreAttackBonus()` for damage modifiers, `ApplyPostAttackEffect()` for on-hit status effects.

**`WeaponRelicEffectHandler`** (static, `Combat/`): Handles relic-specific on-hit effects (role-tagged bonuses). `CalculateBonusDamageMultiplier()` and `ApplyOnHitEffect()`.

**`RelicEffectExecutor`** (~183KB, `new/`): The massive effect execution engine. Contains the implementation for every `RelicEffectType` — Boots movement, Gloves attacks, Hat buffs, Coat defenses, Totem summons, Ultimate abilities. Called by `BattleDeckUI` when a card is played.

**`RelicTargetSelector`** (`new/`): Handles target validation and selection for relic effects. Determines valid targets based on `CardTargetType`.

**`PassiveRelicManager`** (MonoBehaviour, `new/`): Manages always-active passive effects (Trinkets, PassiveUniques). `RegisterPassiveEffects()` reads from `FlexibleUnitEquipment`. Provides query methods: `GetOutgoingDamageModifier()`, `GetIncomingDamageModifier()`, `HasNoBuzzDownside()`, `GetAllyExtraMovement()`, `GetEnemyMovementLimit()`, `NearbyAlliesIgnoreObstacles()`, `GetSurrenderThreshold()`, etc.

---

## 10. Deck & Card System

### `BattleCard` (Serializable class, `Deck/`)
- Represents a single playable card in the shared battle deck.
- Ties to an **owner unit** (the unit who equipped the source relic).
- Can be created from `EquippedRelic` (`BattleCard.FromRelic()`) or `WeaponRelic` (`BattleCard.FromWeaponRelic()`).
- Key fields: cardId, cardName, ownerUnit, sourceRelic/sourceWeaponRelic, category, roleTag, energyCost, effectType, description, isStowed.
- `RequiresTarget()`: Returns true if card needs a target (based on `GetTargetType()` logic).
- `GetTargetType()`: Weapons → None (auto-target closest), Boots → Tile, specific effects override defaults (e.g., some Boots target Ally, some Ultimates target Enemy).
- Stow system: `isStowed = true` prevents discard at end of turn.

### `CardDeckManager` (MonoBehaviour, `new/`)
- Per-unit component that generates cards from `FlexibleUnitEquipment`.
- Builds cards from all equipped relics (weapons produce weapon cards, active category relics produce ability cards, passive relics are skipped).

### `BattleDeckManager` (MonoBehaviour singleton, `Deck/`)
- **Shared team-wide deck** that aggregates cards from all deployed player units.
- `BuildDeckFromScene()`: Scans all "Unit"-tagged player units, collects their cards, shuffles into draw pile.
- Manages draw pile, hand, discard pile, and stow area.
- Draw/discard/shuffle lifecycle per turn.

### `BattleDeckUI` (MonoBehaviour singleton, ~88KB, `Deck/`)
- The main battle UI controller. Renders the hand of cards, handles card selection, targeting mode, and card execution.
- `IsTargeting`: True when waiting for player to click a target/tile.
- `OnTargetSelected(UnitStatus, GridCell)`: Called by `BattleManager` when player clicks during targeting mode.
- Plays cards by calling `RelicEffectExecutor` (for category relics) or `UnitAttack.ExecuteCardAttack()` (for weapons).
- Handles stowing (right-click context menu), discarding, and end-of-turn cleanup.

### `CardUI` (MonoBehaviour, `Deck/`): Visual card component with hover/click effects.
### `CardUIGenerator` (`Deck/`): Generates card UI elements dynamically.
### `CardPlayabilityChecker` (`Deck/`): Validates whether a card can be played (energy, target availability, owner alive, etc.).
### `PassiveRelicsPanel` (`Deck/`): UI panel showing active passive effects.

---

## 11. Combat System

### `DamageCalculator` (static class)

**`Calculate()` — Full damage pipeline:**

**HP Damage Modifiers (multiplicative):**
1. First-action bonus: `min(15%, Speed × 0.2%)` if team went first.
2. Combo bonus: `1 + (comboCount−1) × clamp(Skill × 0.003, 0.02, 0.12)`.
3. Cover reduction: −10% if target adjacent to hazard.
4. Attacker status effect buffs (DamageBuff, StatBuff from Power/Aim modifiers).
5. Attacker passive relic outgoing modifier.
6. Target status effect debuffs (Vulnerable, Protected, Marked, RangedShield).
7. Target passive relic incoming modifier.
8. Ranged HP bonus: +10%.
9. Curse multiplier (default 1.5×).
10. Exposed multiplier: +20%.
11. Flat bonuses from hazard standing effects.

**Morale Damage Modifiers (similar pipeline):**
- Same first-action, combo, cover.
- Target morale damage reduction effects.
- Melee morale bonus: +10%.
- Focus fire bonus: [0%, 0%, 10%, 25%, 45%, 65%] per stack.
- Exposed multiplier.

**Then in `UnitStatus.TakeDamage()`:**
- Grit DR applied to HP damage.
- Hull absorbs up to 30% of post-Grit damage.
- Final HP and morale damage applied.

### `InitiativeSystem` (static class)
- `CalculateInitiative()`: Sums Speed of all active units per team. Higher total goes first. Ties → Player first.
- Recalculated each round.
- `GetFirstActionBonus(unit)`: `min(15%, Speed × 0.2%)`.

### `TargetFinder` (static class)
- `FindNearestEnemy(attacker)`: Manhattan distance, skips surrendered/stasis units.
- `GetAllEnemies(team)`, `GetAllAllies(team)`, `GetAllUnits()`.
- `HasActiveEnemies()`, `HasActiveAllies()`.

### `AttackAnimator` (MonoBehaviour)
- **Melee:** Dash forward (ease-in), pause at target, return faster. Calls `onHit` at target, `onComplete` on return.
- **Ranged:** Spawns golden emissive sphere projectile, flies to target, destroys on hit. Calls `onHit` then `onComplete`.

### `StatusEffect` & `StatusEffectManager` (`Combat/`)
- `StatusEffect`: Data class for buff/debuff instances (type, duration, value, source).
- `StatusEffectManager` (MonoBehaviour): Manages active effects on a unit. Processes duration ticks, provides modifier queries (damage boost, damage reduction, movement reduction, stun, stasis, free move, miss chance, etc.).
- Effects include: Bleed, Daze, Rattled, Cracked, Marked, Slowed, Stunned, Trapped, Cursed, Exposed, DamageBoost, DamageReduction, MoraleDamageReduction, RangedDamageReduction, FreeMove, PreventSurrender, IgnoredByEnemies, ForceTargetClosest, PreventDisplacement, and many more.

### `DeadMansLocker` & `DeadMansLockerManager` (`Combat/`)
**Dead Man's Locker** — Cursed chests placed on the player side that the player must protect.
- Uses **Hit Pips** (not HP) — any hit removes 1 pip regardless of damage.
- Base 3 pips, can fortify up to +2 via tribute deposits.
- When hit: leaks 10% stored tribute, applies Morale Shock to all player units.
- When destroyed: 50% tribute lost permanently, 50% spills as loose tribute.
- **DeadMansLockerManager**: Spawns 1–4 lockers based on grid width, manages tribute quota, tracks surviving lockers.

---

## 12. Hazard System

### `HazardData` (ScriptableObject)
- Defines: name, prefab, isBlocking, isDestructible, maxHealth, causesDisplacement.
- Shape patterns: `Single, Row, Column, Square, Plus`.
- Effect types: `None, Fire, Trap, Plague, ShiftingSand, Lightning, Cursed, Boulder, Box`.
- Standing bonuses: Extra HP/morale damage when attacking from hazard tile, can apply curse.

### `HazardInstance` (MonoBehaviour, attached to spawned hazard)
- Initialized with `HazardData` and parent `GridCell`.
- **Obstacle types:** Box = soft obstacle (destructible), Boulder = hard obstacle (indestructible).
- `TakeObstacleDamage()`: Hard obstacles take no damage. Soft obstacles lose HP and are destroyed when depleted.
- **Turn-end effects:** Fire → environmental HP damage, Plague/ShiftingSand → morale damage, Lightning → 50% chance stun, Cursed → apply curse.
- **On-enter effects:** Trap → apply trap + destroy self, Cursed → apply curse.
- **Destruction:** Drops loot, clears cell blocking, fires event.

### `HazardManager` (~44KB, `Hazards/`)
- `GenerateRandomHazards()`: Places hazards on the grid after deployment.
- `ProcessEarthquakeHazards()`: Displaces units standing on earthquake tiles.
- Manages hazard spawning patterns using `HazardShape`.

---

## 13. Manager Layer

### `CharacterCreationManager` (MonoBehaviour)
- Pre-battle UI for generating units. Has panels for player and enemy teams.
- Each panel: role dropdown, generate button, stat display texts.
- `GenerateUnit()`: Calls `StatGenerator.GenerateStats(role, team)`.
- `GenerateAllUnits()`: Randomizes all panels with unique roles, generates all.
- Flow: Generate units → Click "Start Game" → Opens `EquipmentUIBuilder` → Then to Deployment.

### `EquipmentUIBuilder` (~76KB, root `Scripts/`)
- Code-built UI for equipping relics to units before battle.
- Shows unit list, 7 equipment slots, relic pool, weapon pool.
- Allows dragging relics between slots.
- Callbacks: `onBackClicked`, `onStartBattle`.

### `DeploymentManager` (MonoBehaviour)
- Manual unit placement phase. Player clicks grid cells to place units on player side.
- After placement: Spawns Dead Man's Lockers → Spawns enemies on enemy side → Starts battle.
- Supports selecting and repositioning already-placed units.
- Uses `FlexibleUnitEquipment` for slot-based relic transfer from `UnitData`.
- `SetupCardDeck()`: Adds `CardDeckManager` component to each unit.

### `BattleManager` (MonoBehaviour)
- Handles battle-phase interactions: unit selection (left click), deselection (right click), swap mode.
- **Card targeting integration:** When `BattleDeckUI.IsTargeting` is true, clicks are forwarded to `BattleDeckUI.OnTargetSelected()`.
- Movement is now card-driven (Boots relics), not tile-click.
- Swap system: Initiates swap mode, validates conditions (energy, cooldown, HP threshold, round limit), executes position swap.

### `TurnManager` (MonoBehaviour)
- Alternates between player and enemy turns.
- **Round tracking:** A round completes when both teams have acted.
- **Initiative recalculation** each round via `InitiativeSystem`.
- `StartGameLoop()`: Initializes round 1, builds deck, determines first team.
- `EndTurn()`: Applies hazard effects, processes earthquakes, toggles turn, checks round completion, resets units.
- Enemy turns auto-skip after a delay (`enemyTurnDelay`).
- Swap tracking: `UseSwap()`, `CanSwap()`.
- Unit reset: Calls `UnitMovement.BeginTurn()`, `UnitStatus.OnTurnStart()`, `UnitAttack.ResetCombo()`.

### `EnergyManager` (MonoBehaviour)
- Player resource: Energy (refreshes to max each turn) and Grog (accumulated).
- `StartTurn()`: Resets energy to max. `EndTurn()`: Converts unused energy → grog.
- `TrySpendEnergy(amount)` / `TrySpendGrog(amount)`: Spend and return success.
- `AddEnergy()`: Used by passives (e.g., Captain V1 gives +1 energy/turn).

### `EnemyManager` (MonoBehaviour)
- Simple enemy spawner for non-manual deployment scenarios.
- Spawns 3–4 enemies on enemy side, 50% chance for a captain.

### `GlobalUIManager` (`Managers/`): Manages global UI elements like unit icons.
### `EquipmentUIManager` (`Managers/`): Manages the equipment screen UI flow.

---

## 14. UI System

### `DamagePopup` (MonoBehaviour, `UI/`)
- World-space floating text (TextMeshPro, no Canvas).
- `Create(position, amount, PopupType)`: Spawns popup, styles by type.
- Types: Damage (red, scales with amount), Heal (green), HullDamage (blue), MoraleDamage (yellow), Miss (gray), Stun (bright yellow), Environmental (orange).
- Floats upward with random horizontal spread, fades out over 1 second, auto-destroys.

### `BillBoard` (`UI/`): Makes UI elements always face the camera.
### `UnitWorldUI` (`UI/`): World-space unit info display (HP/morale bars).
### `UnitIcon` (`UI/`): Small unit icon for the roster display.
### `UIManager` (`UI/`): Basic UI management utilities.
### `RelicSlotUI` (`UI/`): Equipment slot visual in the equipment screen.
### `RelicSlotWithJewels` (`UI/`): Equipment slot with jewel socket display.
### `UnitEquipmentCard` (`UI/`): Card-style unit equipment display.
### `UnitListItemUI` (`UI/`): Unit list entry in equipment screen.
### `JewelPoolItemUI` / `RelicPoolItemUI` (`UI/`): Pool item displays in equipment screen.

---

## 15. Camera System

### `CameraOrbit` (MonoBehaviour, `Camera/`)
- Orbital camera around grid center.
- **Controls:** Scroll wheel = zoom (5–30 range), Right mouse = rotate (pitch 10°–85°), Middle mouse = pan.
- Auto-centers on grid at start based on grid width.
- Updates in `LateUpdate()` using spherical coordinate math.

---

## 16. Debug & Editor Tools

### `DebugConsoleLogger` (~22KB, `Debug/`)
- In-game debug console overlay (F1 key toggle).
- Shows recent damage logs, unit states, status effects.
- Color-coded output matching Unity's rich text.

### `LogCapture` (`Debug/`)
- Captures Unity `Debug.Log` output for the debug console.

### `RelicTestTracker` / `RelicTestTrackerEditor` (`new/` + `Editor/`)
- Editor tool for tracking which relic effects have been tested.
- Custom inspector with checkboxes per relic effect.

### `RoleEffectsGenerator` (`Editor/`)
- Editor utility for generating/updating the `RoleEffectsDatabase` ScriptableObject.

---

## 17. Game Flow (Start to Finish)

```
┌──────────────────────────────────────────┐
│ 1. CHARACTER CREATION                     │
│   CharacterCreationManager                │
│   • Player selects roles per panel        │
│   • "Generate" → StatGenerator creates    │
│     UnitData with randomized stats        │
│   • "Generate All" → random unique roles  │
│   • Click "Start Game" →                  │
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 2. EQUIPMENT PHASE                        │
│   EquipmentUIBuilder                      │
│   • Shows all player units + 7 slots each │
│   • Slots 0-4: Flexible (weapon/relic)    │
│   • Slot 5: Ultimate (auto-assigned)      │
│   • Slot 6: Passive (auto-assigned)       │
│   • Player equips relics from pools       │
│   • Click "Start Battle" →                │
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 3. DEPLOYMENT                             │
│   DeploymentManager                       │
│   • Grid generated (random size)          │
│   • Player clicks cells to place units    │
│   • Can reposition by selecting + clicking│
│   • "Finish Deployment" →                 │
│   • Spawns Dead Man's Lockers             │
│   • Spawns enemy units (random positions) │
│   • Generates random hazards              │
│   • Builds shared card deck from all units│
└────────────────┬─────────────────────────┘
                 ▼
┌──────────────────────────────────────────┐
│ 4. BATTLE LOOP                            │
│   TurnManager + BattleManager             │
│                                           │
│   ┌─ ROUND START ─────────────────────┐   │
│   │ InitiativeSystem calculates who   │   │
│   │ goes first (sum of team Speed)    │   │
│   └───────────┬───────────────────────┘   │
│               ▼                           │
│   ┌─ PLAYER TURN ─────────────────────┐   │
│   │ • Energy refreshed                │   │
│   │ • Units reset (movement, combo)   │   │
│   │ • Status effects tick             │   │
│   │ • Draw cards from shared deck     │   │
│   │ • Play cards:                     │   │
│   │   - Weapon → auto-target + attack │   │
│   │   - Boots → move to tile          │   │
│   │   - Gloves → attack + effect      │   │
│   │   - Hat → buff/resource           │   │
│   │   - Coat → defense/barrier        │   │
│   │   - Totem → summon/curse          │   │
│   │   - Ultimate → powerful ability   │   │
│   │ • Can stow cards (keep for later) │   │
│   │ • Click "End Turn"                │   │
│   │ • Unused energy → grog            │   │
│   └───────────┬───────────────────────┘   │
│               ▼                           │
│   ┌─ ENEMY TURN ──────────────────────┐   │
│   │ • Auto-skips after delay          │   │
│   │ • (AI not yet implemented)        │   │
│   └───────────┬───────────────────────┘   │
│               ▼                           │
│   ┌─ TURN END ─────────────────────────┐  │
│   │ • Hazard effects applied           │  │
│   │ • Earthquake displacement          │  │
│   │ • Check round completion           │  │
│   └───────────┬────────────────────────┘  │
│               ▼                           │
│   Loop until all enemies/players defeated │
└──────────────────────────────────────────┘
```

---

## 18. Key Formulas & Balance Constants

### Damage Formula (Weapon Attack)
```
BaseDmg = WeaponData.baseDamage
StatScaled = BaseDmg × (1 + StatValue × 0.03)
  where Stat = Power (melee) or Aim (ranged)

TotalMultiplier = (1 + RarityBonus) × RelicEffectMult × WeaponEffectMult 
                  × DrunkMod × ProficiencyMult

RawDamage = StatScaled × TotalMultiplier

[In DamageCalculator.Calculate():]
HPDamage = RawDamage × HPModifiers × TypeBonus × CurseMult × ExposedMult + FlatBonus
MoraleDamage = RawDamage × MoraleModifiers × TypeBonus × FocusFire × ExposedMult + FlatBonus

[In UnitStatus.TakeDamage():]
AfterGrit = HPDamage × (1 − GritDR)
HullAbsorbed = min(CurrentHull, AfterGrit × 0.30)
FinalHP = AfterGrit − HullAbsorbed
```

### Grit DR Formula
```
GritFactor = (1 − HP%) × 0.50 + Morale% × 0.40
DR = min(40%, GritFactor × EffectiveGrit × 0.01)
```

### Combo Multiplier
```
ComboStep = clamp(Skill × 0.003, 0.02, 0.12)
ComboMult = 1 + (min(comboCount, 6) − 1) × ComboStep
```

### First-Action Bonus
```
Bonus = min(15%, Speed × 0.2%)
Applied to both HP and morale damage.
```

### Hull Pool
```
MaxHull = 50 + HullStat × 10
Absorbs up to 30% of incoming post-Grit damage.
```

### Proficiency
```
Stored as int percentage (e.g., 150 = 1.5×)
Applied when relic's roleTag matches unit's role.
```

### Surrender Threshold
```
Default: 20% of MaxMorale
Modified by passives: Can be lowered to 10% or raised to 30%
Checked after every morale damage application.
```

---

## 19. Cross-System Interactions

### Card Play → Attack Flow
```
BattleDeckUI (card clicked)
  → CardPlayabilityChecker.CanPlay()
  → EnergyManager.TrySpendEnergy()
  → [If weapon card]: UnitAttack.ExecuteCardAttack(relic)
    → AttackAnimator.PlayMelee/RangedAttack()
    → [on hit]: UnitStatus.TakeDamage() → DamageCalculator.Calculate()
    → [on hit]: WeaponRelicEffectHandler.ApplyOnHitEffect()
    → [on hit]: WeaponEffectHandler.ApplyPostAttackEffect()
    → [on complete]: ReduceBuzz, MarkAsAttacked, GameEvents
  → [If category card]: RelicEffectExecutor.Execute()
    → Various effects (heal, buff, debuff, summon, move, etc.)
```

### Status Effect Integration
```
StatusEffectManager is queried by:
  • DamageCalculator (damage modifiers, miss chance)
  • UnitMovement (movement reduction, stun, stasis, free move)
  • UnitStatus (surrender prevention)
  • PassiveRelicManager (passive effect queries)
  • TargetFinder (skip units in stasis)
```

### Passive Relic Flow
```
DeploymentManager.SpawnUnit()
  → FlexibleUnitEquipment.Initialize() (auto-assigns Ultimate + Passive)
  → Transfer relics from UnitData to FlexibleUnitEquipment
  → PassiveRelicManager.RegisterPassiveEffects()
    → Reads FlexibleUnitEquipment.GetPassiveRelics()
    → Stores active passive effect types

PassiveRelicManager is queried by:
  • DamageCalculator (outgoing/incoming damage modifiers)
  • UnitMovement (extra movement, movement limits, ignore obstacles)
  • UnitStatus (Speed modifier, surrender threshold)
  • BattleDeckUI (some passives affect card costs)
```

### Hazard Interaction
```
HazardManager.GenerateRandomHazards() → places on grid
UnitMovement.CheckHazardOnTile() → HazardInstance.OnUnitEnter()
TurnManager.ApplyHazardEffects() → HazardInstance.OnTurnEnd()
UnitAttack.GetStandingBonuses() → reads hazard bonuses from current tile
UnitAttack.IsBlockedByRow() → checks for obstacle hazards between attacker/target
```

### Dead Man's Locker Integration
```
DeploymentManager → DeadMansLockerManager.SpawnLockers()
  → Places on player side, marks cells as blocked
Enemies attack lockers → DeadMansLocker.TakeHit()
  → Removes 1 pip, leaks tribute, applies Morale Shock to ALL player units
  → If pips = 0: DestroyLocker() → 50% tribute lost, 50% spills
```

---

## Quick Reference: Component Stack per Unit GameObject

Every deployed unit has these components:
| Component | Purpose |
|-----------|---------|
| `UnitStatus` | Stats, HP/morale, status flags, damage intake |
| `UnitMovement` | Movement, range calculation, displacement |
| `UnitAttack` | Attack execution, combo tracking |
| `StatusEffectManager` | Active buff/debuff management |
| `PassiveRelicManager` | Always-on passive effect queries |
| `FlexibleUnitEquipment` | 7-slot equipment storage |
| `CardDeckManager` | Generates cards from equipment |
| `UnitEquipmentUpdated` | Legacy equipment bridge |
| `AttackAnimator` | Melee dash / ranged projectile animations |
| `MeshRenderer` | Visual representation |

---

*End of documentation. This file should give complete understanding of every system, data flow, formula, and interaction in the codebase.*
