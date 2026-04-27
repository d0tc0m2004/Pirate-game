using System.Collections.Generic;
using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Combat;
using TacticalGame.Enums;
using System.Linq;
using TacticalGame.Equipment;

namespace TacticalGame.Hazards
{
    /// <summary>
    /// Manages hazard spawning and balance across the battlefield.
    /// </summary>
    public class HazardManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hazard Configuration")]
        [SerializeField] private List<HazardData> possibleHazards;
        
        [Header("Runtime Hazard Prefabs")]
        [Tooltip("Optional prefab for poison tiles. If null, creates a simple visual.")]
        [SerializeField] private GameObject poisonPrefab;
        [Tooltip("Optional prefab for trap tiles.")]
        [SerializeField] private GameObject trapPrefab;
        [Tooltip("Optional prefab for fire tiles.")]
        [SerializeField] private GameObject firePrefab;
        [Tooltip("Prefab for hard obstacles (boulders).")]
        [SerializeField] private GameObject hardObstaclePrefab;
        [Tooltip("Prefab for soft obstacles (boxes).")]
        [SerializeField] private GameObject softObstaclePrefab;
        [Tooltip("Prefab for exploding barrels.")]
        [SerializeField] private GameObject explodingBarrelPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Minimum tiles covered by hazards per side.")]
        [SerializeField] private int minOccupiedTilesPerSide = 5;
        [SerializeField] private int maxOccupiedTilesPerSide = 8;

        #endregion

        #region Private State

        private GridManager gridManager;
        private List<RuntimeHazard> activeRuntimeHazards = new List<RuntimeHazard>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<HazardManager>();
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
        }
        
        private void OnEnable()
        {
            GameEvents.OnPlayerTurnEnd += OnPlayerTurnEnded;
            GameEvents.OnEnemyTurnEnd += OnEnemyTurnEnded;
        }
        
        private void OnDisable()
        {
            GameEvents.OnPlayerTurnEnd -= OnPlayerTurnEnded;
            GameEvents.OnEnemyTurnEnd -= OnEnemyTurnEnded;
        }

        // The Middleman methods to identify whose turn just ended
        private void OnPlayerTurnEnded() { ProcessRuntimeHazards(true); }
        private void OnEnemyTurnEnded() { ProcessRuntimeHazards(false); }

        #endregion

        #region Public Methods - Random Generation

        /// <summary>
        /// Generate random hazards across the battlefield.
        /// </summary>
        public void GenerateRandomHazards()
        {
            if (possibleHazards == null || possibleHazards.Count == 0)
            {
                Debug.LogWarning("No hazards configured in HazardManager!");
                return;
            }

            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }

            int targetTiles = Random.Range(minOccupiedTilesPerSide, maxOccupiedTilesPerSide + 1);
            Debug.Log($"Target Balance: Occupying ~{targetTiles} tiles per side.");

            SpawnHazardsUntilTargetReached(true, targetTiles);
            SpawnHazardsUntilTargetReached(false, targetTiles);
        }

        #endregion
        
        #region Public Methods - Runtime Hazard Spawning
        
        /// <summary>
        /// Create a poison tile at a specific cell.
        /// </summary>
        public RuntimeHazard CreatePoisonTile(GridCell cell, int damagePerTurn, int duration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.Poison, damagePerTurn, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.2f, 0.8f, 0.2f, 0.6f)); // Green for poison
                Debug.Log($"Created poison tile at ({cell.XPosition}, {cell.YPosition}): {damagePerTurn} dmg for {duration} turns");
            }
            return hazard;
        }
        
        /// <summary>
        /// Create a poison cloud (multiple poison tiles in an area).
        /// </summary>
        public List<RuntimeHazard> CreatePoisonCloud(GridCell centerCell, int damagePerTurn, int duration, int range)
        {
            if (centerCell == null) return new List<RuntimeHazard>();
            
            EnsureGridManager();
            var hazards = new List<RuntimeHazard>();
            
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= range) // Diamond shape
                    {
                        var cell = gridManager.GetCell(centerCell.XPosition + dx, centerCell.YPosition + dy);
                        if (cell != null && !cell.IsMiddleColumn && !cell.HasHazard)
                        {
                            var hazard = CreatePoisonTile(cell, damagePerTurn, duration);
                            if (hazard != null) hazards.Add(hazard);
                        }
                    }
                }
            }
            
            Debug.Log($"Created poison cloud: {hazards.Count} tiles");
            return hazards;
        }

        // ==================== NEW: RUM PUDDLES FOR HELMSMAN V2 ====================
        public RuntimeHazard CreateRumPuddleTile(GridCell cell, int buzzAmount, int duration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.RumPuddle, buzzAmount, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.8f, 0.4f, 0.1f, 0.6f)); // Amber/Rum color
                Debug.Log($"Created rum puddle at ({cell.XPosition}, {cell.YPosition}): +{buzzAmount} buzz for {duration} turns");
            }
            return hazard;
        }

        public List<RuntimeHazard> CreateRumPuddleCloud(GridCell centerCell, int buzzAmount, int duration, int range)
        {
            if (centerCell == null) return new List<RuntimeHazard>();
            
            EnsureGridManager();
            var hazards = new List<RuntimeHazard>();
            
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= range) // Diamond shape
                    {
                        var cell = gridManager.GetCell(centerCell.XPosition + dx, centerCell.YPosition + dy);
                        if (cell != null && !cell.IsMiddleColumn && !cell.HasHazard)
                        {
                            var hazard = CreateRumPuddleTile(cell, buzzAmount, duration);
                            if (hazard != null) hazards.Add(hazard);
                        }
                    }
                }
            }
            
            Debug.Log($"Created Rum Puddle cloud: {hazards.Count} tiles");
            return hazards;
        }
        // ==========================================================================
        
        /// <summary>
        /// Create a fire tile at a specific cell using the ORIGINAL fire hazard system.
        /// Uses the same HazardData/HazardInstance path as scene-spawned hazards.
        /// Falls back to a simple runtime visual if no fire HazardData is configured.
        /// damagePerTurn and duration params are only used for the fallback.
        /// </summary>
        public RuntimeHazard CreateFireTile(GridCell cell, int damagePerTurn, int duration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            // Try to use the original fire hazard system
            HazardData fireData = possibleHazards?.Find(h => h != null && h.effectType == HazardEffectType.Fire);
            if (fireData != null && fireData.hazardPrefab != null)
            {
                // Use the SAME system as scene-placed hazards
                cell.ApplyHazard(fireData.hazardPrefab, fireData.isBlocking);
                
                GameObject spawnedObj = cell.HazardVisualObject;
                if (spawnedObj != null)
                {
                    HazardInstance instance = spawnedObj.GetComponent<HazardInstance>();
                    if (instance == null)
                        instance = spawnedObj.AddComponent<HazardInstance>();
                    instance.Initialize(fireData, cell);
                }
                
                Debug.Log($"Spawned original fire hazard at ({cell.XPosition}, {cell.YPosition})");
                return null; // No RuntimeHazard needed - handled by HazardInstance
            }
            
            // Fallback: runtime hazard if no fire HazardData is configured
            Debug.LogWarning("No fire HazardData in possibleHazards - using runtime fallback");
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.Fire, damagePerTurn, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(1f, 0.4f, 0.1f, 0.6f));
            }
            return hazard;
        }
        
        /// <summary>
        /// Create a trap at a specific cell.
        /// </summary>
        public RuntimeHazard CreateTrap(GridCell cell, int stunDuration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.Trap, stunDuration, -1); // -1 = permanent until triggered
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.6f, 0.3f, 0.1f, 0.6f)); // Brown for trap
                Debug.Log($"Created trap at ({cell.XPosition}, {cell.YPosition}): stuns for {stunDuration} turns");
            }
            return hazard;
        }
        
        /// <summary>
        /// Create a healing zone at a specific cell.
        /// </summary>
        public RuntimeHazard CreateHealingZone(GridCell cell, int healPerTurn, int duration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.Healing, healPerTurn, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.2f, 1f, 0.5f, 0.6f)); // Bright green for healing
                Debug.Log($"Created healing zone at ({cell.XPosition}, {cell.YPosition})");
            }
            return hazard;
        }
        
        /// <summary>
        /// Create a speed boost zone at a specific cell.
        /// </summary>
        public RuntimeHazard CreateSpeedZone(GridCell cell, int speedBonus, int duration)
        {
            if (cell == null || cell.HasHazard) return null;
            
            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.SpeedBoost, speedBonus, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.3f, 0.7f, 1f, 0.6f)); // Light blue for speed
                Debug.Log($"Created speed zone at ({cell.XPosition}, {cell.YPosition})");
            }
            return hazard;
        }
        
        /// <summary>
        /// Create a hard obstacle (indestructible) at a specific cell.
        /// </summary>
        public RuntimeHazard CreateHardObstacle(GridCell cell, int duration)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.HardObstacle, 0, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.4f, 0.35f, 0.3f, 0.95f)); // Stone gray
                cell.isBlockedState = true;
                Debug.Log($"Created hard obstacle at ({cell.XPosition}, {cell.YPosition}) for {duration} turns");
            }
            return hazard;
        }

        /// <summary>
        /// Create a soft obstacle (destructible) at a specific cell.
        /// </summary>
        public RuntimeHazard CreateSoftObstacle(GridCell cell, int hp, int duration)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.SoftObstacle, hp, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.6f, 0.45f, 0.25f, 0.9f)); // Wood brown
                cell.isBlockedState = true;
                Debug.Log($"Created soft obstacle ({hp} HP) at ({cell.XPosition}, {cell.YPosition})");
            }
            return hazard;
        }

        /// <summary>
        /// Create a cannon obstacle (destructible) at a specific cell.
        /// </summary>
        public RuntimeHazard CreateCannonObstacle(GridCell cell, int hp, int damage)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.CannonObstacle, hp, -1, damage);
            if (hazard != null)
            {
                hazard.SetColor(new Color(1f, 1f, 1f, 1f)); // White cube for cannon
                cell.isBlockedState = true;
                Debug.Log($"Created cannon obstacle ({hp} HP, {damage} dmg) at ({cell.XPosition}, {cell.YPosition})");
            }
            return hazard;
        }

        /// <summary>
        /// Create an exploding barrel at a specific cell.
        /// </summary>
        public RuntimeHazard CreateExplodingBarrel(GridCell cell, int damage, int fuseTimer)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.ExplodingBarrel, damage, fuseTimer);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.8f, 0.2f, 0.1f, 0.85f)); // Red for explosive
                Debug.Log($"Created exploding barrel at ({cell.XPosition}, {cell.YPosition}): {damage} dmg in {fuseTimer} turns");
            }
            return hazard;
        }

        /// <summary>
        /// Create an invisible trap (stun) at a specific cell.
        /// </summary>
        public RuntimeHazard CreateInvisibleTrap(GridCell cell, int duration)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.InvisibleTrap, 1, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(1f, 1f, 1f, 0f)); // Fully invisible
                Debug.Log($"Created invisible trap at ({cell.XPosition}, {cell.YPosition}) for {duration} turns");
            }
            return hazard;
        }

        /// <summary>
        /// Create a cursed tile that prevents movement out and increases damage taken.
        /// </summary>
        public RuntimeHazard CreateCursedTile(GridCell cell, int duration)
        {
            if (cell == null || cell.HasHazard || cell.IsOccupied) return null;

            var hazard = CreateRuntimeHazard(cell, RuntimeHazardType.CursedTile, 1, duration);
            if (hazard != null)
            {
                hazard.SetColor(new Color(0.3f, 0f, 0.4f, 0.8f)); // Dark purple
                Debug.Log($"Created cursed tile at ({cell.XPosition}, {cell.YPosition}) for {duration} turns");
            }
            return hazard;
        }
        /// <summary>
        /// Find empty cells near a position for spawning.
        /// </summary>
        public List<GridCell> FindEmptyCellsNear(Vector3 worldPos, int count, int searchRange = 3)
        {
            EnsureGridManager();
            var center = gridManager.WorldToGridPosition(worldPos);
            var emptyCells = new List<GridCell>();

            for (int dx = -searchRange; dx <= searchRange && emptyCells.Count < count; dx++)
            {
                for (int dy = -searchRange; dy <= searchRange && emptyCells.Count < count; dy++)
                {
                    var cell = gridManager.GetCell(center.x + dx, center.y + dy);
                    if (cell != null && !cell.IsOccupied && !cell.HasHazard && !cell.IsMiddleColumn)
                    {
                        emptyCells.Add(cell);
                    }
                }
            }

            return emptyCells;
        }

        /// <summary>
        /// Gets the active runtime hazard on a specific cell (useful for the Debug Logger).
        /// </summary>
        public RuntimeHazard GetRuntimeHazard(GridCell cell)
        {
            if (cell == null) return null;
            return activeRuntimeHazards.Find(h => h.Cell == cell);
        }
        
        /// <summary>
        /// Remove all runtime hazards from a cell.
        /// </summary>
        public void ClearHazard(GridCell cell)
        {
            if (cell == null) return;
            
            var toRemove = activeRuntimeHazards.FindAll(h => h.Cell == cell);
            foreach (var hazard in toRemove)
            {
                hazard.Destroy();
                activeRuntimeHazards.Remove(hazard);
            }
            
            cell.ClearHazard();
        }
        
        #endregion
        
        #region Runtime Hazard Logic
        
        private RuntimeHazard CreateRuntimeHazard(GridCell cell, RuntimeHazardType type, int value, int duration, int extraValue = 0)
        {
            if (cell == null) return null;
            
            // Create visual
            GameObject visual;
            GameObject prefab = GetPrefabForType(type);
            
            if (prefab != null)
            {
                visual = Instantiate(prefab, cell.GetWorldPosition(), Quaternion.identity);
            }
            else if (type == RuntimeHazardType.HardObstacle || type == RuntimeHazardType.SoftObstacle || 
                     type == RuntimeHazardType.ExplodingBarrel || type == RuntimeHazardType.CannonObstacle)
            {
                // 3D cube for obstacles/barrels/cannon so they're visible
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.position = cell.GetWorldPosition();
                visual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            }
            else
            {
                // Flat quad for ground hazards (poison, fire, traps, rum puddles etc.)
                visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
                visual.transform.position = cell.GetWorldPosition() + Vector3.up * 0.02f;
                visual.transform.rotation = Quaternion.Euler(90, 0, 0);
                visual.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                // Remove collider so it doesn't block clicks
                var collider = visual.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            
            visual.name = $"RuntimeHazard_{type}";
            visual.transform.SetParent(cell.transform);
            
            // Create runtime hazard data
            var hazard = new RuntimeHazard(cell, visual, type, value, duration, extraValue);
            activeRuntimeHazards.Add(hazard);
            
            // Mark cell as having hazard
            cell.hasHazardState = true;
            
            return hazard;
        }
        
        private GameObject GetPrefabForType(RuntimeHazardType type)
        {
            return type switch
            {
                RuntimeHazardType.Poison => poisonPrefab,
                RuntimeHazardType.Trap => trapPrefab,
                RuntimeHazardType.Fire => firePrefab,
                RuntimeHazardType.HardObstacle => hardObstaclePrefab,
                RuntimeHazardType.SoftObstacle => softObstaclePrefab,
                RuntimeHazardType.ExplodingBarrel => explodingBarrelPrefab,
                _ => null
            };
        }
        
        private void ProcessRuntimeHazards(bool isPlayerTurnEnd)
        {
            var toRemove = new List<RuntimeHazard>();
            
            foreach (var hazard in activeRuntimeHazards)
            {
                if (hazard.Cell == null)
                {
                    toRemove.Add(hazard);
                    continue;
                }
                
                // Apply effect to unit on tile
                if (hazard.Cell.IsOccupied && hazard.Cell.OccupyingUnit != null)
                {
                    var unit = hazard.Cell.OccupyingUnit.GetComponent<UnitStatus>();
                    if (unit != null && !unit.HasSurrendered)
                    {
                        ApplyHazardEffect(hazard, unit);
                    }
                }
                
                // Decrement duration (if not permanent)
                if (hazard.Duration > 0)
                {
                    hazard.Duration--;
                    if (hazard.Duration <= 0)
                    {
                        toRemove.Add(hazard);
                    }
                }

                // THE GATEKEEPER: Cannon ONLY attacks if it is the Player's turn end
                if (hazard.Type == RuntimeHazardType.CannonObstacle && isPlayerTurnEnd)
                {
                    FireCannon(hazard);
                }
            }
            
            // Clean up expired hazards
            foreach (var hazard in toRemove)
            {
                if (hazard.Cell != null)
                {
                    hazard.Cell.hasHazardState = false;
                    // Clears blocking for obstacles and cannons
                    if (hazard.Type == RuntimeHazardType.HardObstacle ||
                        hazard.Type == RuntimeHazardType.SoftObstacle ||
                        hazard.Type == RuntimeHazardType.CannonObstacle)
                    {
                        hazard.Cell.isBlockedState = false;
                    }
                    // Exploding barrel AoE on expiry
                    if (hazard.Type == RuntimeHazardType.ExplodingBarrel)
                    {
                        ExplodeBarrel(hazard);
                    }
                }
                hazard.Destroy();
                activeRuntimeHazards.Remove(hazard);
            }
        }
        
        private void ApplyHazardEffect(RuntimeHazard hazard, UnitStatus unit)
        {
            switch (hazard.Type)
            {
                case RuntimeHazardType.Poison:
                case RuntimeHazardType.Fire:
                    unit.TakeEnvironmentalDamage(hazard.Value, hazard.Type.ToString());
                    break;
                    
                case RuntimeHazardType.Trap:
                case RuntimeHazardType.InvisibleTrap:
                    unit.ApplyStun(hazard.Value);
                    Debug.Log($"{unit.UnitName} triggered trap! Stunned for {hazard.Value} turns!");
                    
                    // Check if player has PassiveUnique_EnemyDiscardOnBoot
                    if (unit.Team == Team.Enemy)
                    {
                        var allPassives = FindObjectsByType<TacticalGame.Equipment.PassiveRelicManager>(FindObjectsSortMode.None);
                        foreach (var pm in allPassives)
                        {
                            var pmUnit = pm.GetComponent<UnitStatus>();
                            if (pmUnit != null && pmUnit.Team == Team.Player && pm.HasPassive(RelicEffectType.PassiveUnique_EnemyDiscardOnBoot))
                            {
                                var deckManager = unit.GetComponent<TacticalGame.Equipment.CardDeckManager>();
                                if (deckManager != null)
                                {
                                    deckManager.DiscardRandomCard();
                                }
                                break;
                            }
                        }
                    }

                    hazard.Duration = 0;
                    break;
                    
                case RuntimeHazardType.Healing:
                    unit.Heal(hazard.Value);
                    Debug.Log($"{unit.UnitName} healed {hazard.Value} HP from healing zone!");
                    hazard.Duration = 0; // <-- Consumes the potion!
                    break;
                    
                case RuntimeHazardType.SpeedBoost:
                    var effects = unit.GetComponent<StatusEffectManager>();
                    if (effects != null) effects.ApplyEffect(StatusEffect.CreateSpeedBoost(1, hazard.Value, null));
                    Debug.Log($"{unit.UnitName} gained +{hazard.Value} movement from speed zone!");
                    break;

                case RuntimeHazardType.ExplodingBarrel:
                    unit.TakeEnvironmentalDamage(hazard.Value, "ExplodingBarrel");
                    hazard.Duration = 0; 
                    break;

                case RuntimeHazardType.RumPuddle:
                    unit.AddBuzz(hazard.Value);
                    Debug.Log($"{unit.UnitName} stepped in rum puddle! Gained {hazard.Value} buzz.");
                    break;

                case RuntimeHazardType.CursedTile:
                    var stEffects = unit.GetComponent<StatusEffectManager>();
                    if (stEffects != null)
                    {
                        stEffects.ApplyEffect(StatusEffect.CreateTrapped(hazard.Duration, null));
                        Debug.Log($"{unit.UnitName} stepped on a cursed tile and is now trapped!");
                    }
                    break;

                case RuntimeHazardType.HardObstacle:
                case RuntimeHazardType.SoftObstacle:
                case RuntimeHazardType.CannonObstacle:
                    // Obstacles block movement, don't apply effect
                    break;
            }
        }
        
        private void FireCannon(RuntimeHazard cannon)
        {
            // Find all enemies and pick a random one
            var enemies = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None)
                .Where(u => u != null && u.Team == Team.Enemy && !u.HasSurrendered)
                .ToList();
                
            if (enemies.Count > 0)
            {
                var targetUnit = enemies[Random.Range(0, enemies.Count)];
                targetUnit.TakeEnvironmentalDamage(cannon.ExtraValue, "SummonedCannon");
                Debug.Log($"Summoned Cannon ({cannon.Value} HP) fired at {targetUnit.UnitName} for {cannon.ExtraValue} dmg!");
            }
        }
        
        private void ExplodeBarrel(RuntimeHazard barrel)
        {
            EnsureGridManager();
            int damage = barrel.Value;
            // Damage all units adjacent to the barrel
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var cell = gridManager.GetCell(barrel.Cell.XPosition + dx, barrel.Cell.YPosition + dy);
                    if (cell != null && cell.IsOccupied && cell.OccupyingUnit != null)
                    {
                        var unit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                        if (unit != null && !unit.HasSurrendered)
                        {
                            unit.TakeEnvironmentalDamage(damage, "BarrelExplosion");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when a unit enters a cell - check for traps.
        /// </summary>
        public void OnUnitEnterCell(UnitStatus unit, GridCell cell)
        {
            if (unit == null || cell == null) return;
            
            // Checks for both Traps AND Healing Potions!
            var triggeredHazard = activeRuntimeHazards.Find(h => h.Cell == cell && 
                (h.Type == RuntimeHazardType.Trap || h.Type == RuntimeHazardType.Healing));
                
            if (triggeredHazard != null)
            {
                ApplyHazardEffect(triggeredHazard, unit);
                
                // Remove after triggering
                if (triggeredHazard.Duration <= 0)
                {
                    cell.hasHazardState = false;
                    triggeredHazard.Destroy();
                    activeRuntimeHazards.Remove(triggeredHazard);
                }
            }
        }
        
        #endregion

        #region Private Spawning Logic (Original)
        
        private void EnsureGridManager()
        {
            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }
        }

        private void SpawnHazardsUntilTargetReached(bool isPlayerSide, int targetTileCount)
        {
            EnsureGridManager();
            
            int occupiedCount = 0;
            int attempts = 0;
            const int maxAttempts = 100;

            while (occupiedCount < targetTileCount && attempts < maxAttempts)
            {
                attempts++;

                HazardData selectedHazard = possibleHazards[Random.Range(0, possibleHazards.Count)];
                int shapeSize = GetShapeSize(selectedHazard.shapePattern);

                // Skip if this shape would exceed target by too much
                if (occupiedCount + shapeSize > targetTileCount + 2)
                {
                    continue;
                }

                // Find valid spawn position
                int middle = gridManager.GetMiddleColumnIndex();
                int startX = isPlayerSide 
                    ? Random.Range(0, middle) 
                    : Random.Range(middle + 1, gridManager.GridWidth);
                int startY = Random.Range(0, gridManager.GridHeight);

                List<Vector2Int> targetCoords = GetShapeCoordinates(selectedHazard.shapePattern, startX, startY);

                int sizeIncrease = GetHazardSizeIncreaseForSide(isPlayerSide);
                if (sizeIncrease > 0)
                {
                    for (int i = 0; i < sizeIncrease; i++)
                    {
                        Vector2Int? extra = GetExtraAdjacentCoordinate(targetCoords);
                        if (extra.HasValue)
                        {
                            targetCoords.Add(extra.Value);
                            shapeSize++;
                        }
                    }
                }

                // Validate all coordinates
                bool shapeIsValid = true;
                foreach (Vector2Int coord in targetCoords)
                {
                    GridCell cell = gridManager.GetCell(coord.x, coord.y);

                    if (cell == null || cell.IsMiddleColumn || cell.HasHazard || cell.IsBlocked)
                    {
                        shapeIsValid = false;
                        break;
                    }
                }

                // Spawn if valid
                if (shapeIsValid)
                {
                    foreach (Vector2Int coord in targetCoords)
                    {
                        GridCell cell = gridManager.GetCell(coord.x, coord.y);

                        // Handle displacement
                        if (cell.IsOccupied && selectedHazard.causesDisplacement)
                        {
                            DisplaceUnit(cell);
                        }

                        // Spawn hazard
                        cell.ApplyHazard(selectedHazard.hazardPrefab, selectedHazard.isBlocking);

                        // Initialize hazard instance
                        GameObject spawnedObj = cell.HazardVisualObject;
                        if (spawnedObj != null)
                        {
                            HazardInstance instance = spawnedObj.GetComponent<HazardInstance>();
                            if (instance == null)
                            {
                                instance = spawnedObj.AddComponent<HazardInstance>();
                            }

                            instance.Initialize(selectedHazard, cell);

                            // Trigger effect if unit present
                            if (cell.IsOccupied && cell.OccupyingUnit != null)
                            {
                                Debug.Log($"Hazard spawned under {cell.OccupyingUnit.name}. Triggering effect!");
                                instance.OnUnitEnter(cell.OccupyingUnit);
                            }
                        }
                    }

                    occupiedCount += shapeSize;
                }
            }

            Debug.Log($"Side {(isPlayerSide ? "Left" : "Right")} finished with {occupiedCount} tiles occupied.");
        }

        private int GetShapeSize(HazardShape shape)
        {
            return shape switch
            {
                HazardShape.Single => 1,
                HazardShape.Row => 3,
                HazardShape.Column => 3,
                HazardShape.Square => 4,
                HazardShape.Plus => 5,
                _ => 1
            };
        }

        private List<Vector2Int> GetShapeCoordinates(HazardShape shape, int x, int y)
        {
            List<Vector2Int> coords = new List<Vector2Int> { new Vector2Int(x, y) };

            switch (shape)
            {
                case HazardShape.Row:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x - 1, y));
                    break;
                    
                case HazardShape.Column:
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x, y - 1));
                    break;
                    
                case HazardShape.Square:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x + 1, y + 1));
                    break;
                    
                case HazardShape.Plus:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x - 1, y));
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x, y - 1));
                    break;
            }

            return coords;
        }

        private void DisplaceUnit(GridCell currentCell)
        {
            GameObject unit = currentCell.OccupyingUnit;
            currentCell.RemoveUnit();

            Vector2Int[] directions = 
            {
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                GridCell neighbor = gridManager.GetCell(
                    currentCell.XPosition + dir.x, 
                    currentCell.YPosition + dir.y
                );

                if (neighbor != null && neighbor.CanPlaceUnit())
                {
                    neighbor.PlaceUnit(unit);
                    return;
                }
            }
        }

        private int GetHazardSizeIncreaseForSide(bool isPlayerSide)
        {
            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            int maxIncrease = 0;
            foreach (var unit in units)
            {
                if (unit == null || unit.HasSurrendered) continue;
                var passiveManager = unit.GetComponent<TacticalGame.Equipment.PassiveRelicManager>();
                if (passiveManager != null)
                {
                    int increase = passiveManager.GetEnemyHazardSizeIncrease();
                    if (increase > 0)
                    {
                        bool unitIsPlayerSide = unit.Team == Team.Player;
                        if (isPlayerSide != unitIsPlayerSide)
                        {
                            maxIncrease = Mathf.Max(maxIncrease, increase);
                        }
                    }
                }
            }
            return maxIncrease;
        }

        private Vector2Int? GetExtraAdjacentCoordinate(List<Vector2Int> existingCoords)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            // Try up to 10 random adjacent spots
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2Int baseCoord = existingCoords[Random.Range(0, existingCoords.Count)];
                Vector2Int newCoord = baseCoord + directions[Random.Range(0, directions.Length)];
                
                if (!existingCoords.Contains(newCoord))
                {
                    GridCell cell = gridManager.GetCell(newCoord.x, newCoord.y);
                    if (cell != null && !cell.IsMiddleColumn && !cell.HasHazard && !cell.IsBlocked)
                    {
                        return newCoord;
                    }
                }
            }
            return null;
        }

        #endregion
    }
    
    #region Runtime Hazard Data
    
    /// <summary>
    /// Type of runtime-created hazard.
    /// </summary>
    public enum RuntimeHazardType
    {
        Poison,
        Fire,
        Trap,
        Healing,
        SpeedBoost,
        Shield,
        Slow,
        HardObstacle,
        SoftObstacle,
        ExplodingBarrel,
        CannonObstacle,
        RumPuddle, // <--- NEW FOR HELMSMAN V2
        InvisibleTrap,
        CursedTile
    }
    
    /// <summary>
    /// Runtime hazard instance data.
    /// </summary>
    public class RuntimeHazard
    {
        public GridCell Cell { get; private set; }
        public GameObject Visual { get; private set; }
        public RuntimeHazardType Type { get; private set; }
        public int Value { get; private set; }
        public int Duration { get; set; }
        public int ExtraValue { get; private set; } // Used for cannon damage
        
        public RuntimeHazard(GridCell cell, GameObject visual, RuntimeHazardType type, int value, int duration, int extraValue = 0)
        {
            Cell = cell;
            Visual = visual;
            Type = type;
            Value = value;
            Duration = duration;
            ExtraValue = extraValue;
        }
        
        public void SetColor(Color color)
        {
            if (Visual == null) return;
            
            var renderer = Visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
        
        public void Destroy()
        {
            if (Visual != null)
            {
                Object.Destroy(Visual);
            }
        }
    }
    
    #endregion
}