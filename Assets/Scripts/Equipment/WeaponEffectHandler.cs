using UnityEngine;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Core;
using TacticalGame.Combat;
using TacticalGame.Config;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles execution of BASE weapon effects (not relic effects).
    /// These are the innate effects each weapon family has.
    /// 
    /// Weapon Effects:
    /// - Cutlass: IntimidatingCut - Extra morale damage if target below 80% morale
    /// - Machete: ChopThrough - Destroys soft obstacles in row
    /// - Rapier: PiercingPoint - 25% of damage bypasses Hull
    /// - Axe: Gash - Apply Bleed (2 turns): 20 damage at end of round
    /// - Hammer: Concuss - Apply Daze (1 turn): -20% damage next round
    /// - Anchor: DragDown - Pull target 1 tile toward attacker
    /// - Clubs: Rattle - Apply Rattled (1 turn): +10% morale damage taken
    /// - Mace: Crack - Apply Cracked (2 turns): +15% Hull damage taken
    /// - Harpoon: ReelOut - Push target back 1 tile
    /// - Spear: LineBreak - If enemy behind target, deal 40% damage to them too
    /// - BoardingPike: KeepBack - If enemy behind, prevent target from moving 1 turn
    /// - Dagger: Finisher - +10% damage if target below 50% HP
    /// - Dirk: FearFactor - +10% morale damage if target below 50% morale
    /// - Pistol: QuickDraw - +15% damage if first attack this turn
    /// - Musket: ArmorPiercing - 60% of damage bypasses Hull
    /// - Blunderbuss: Scattershot - Hit nearby enemies for 20% damage
    /// - Grenade: Shrapnel - Apply Marked (2 turns): next 2 hits deal +15% damage
    /// - Cannonball: HullBreaker - +50% damage to Hull, nearby targets take 20% if Hull breaks
    /// - CursedBird: BadOmen - Apply Omen (2 turns): +20 morale damage when losing morale
    /// - CursedMonkey: Pilfer - Steal 1 Grog from enemy, or +10 Buzz if none
    /// </summary>
    public static class WeaponEffectHandler
    {
        private static GridManager gridManager;
        
        /// <summary>
        /// Calculate bonus damage from weapon effects (applied BEFORE attack).
        /// Returns a multiplier (1.0 = no bonus).
        /// </summary>
        public static float CalculatePreAttackBonus(
            UnitStatus attacker,
            UnitStatus target,
            WeaponData weapon,
            bool isFirstAttack)
        {
            if (weapon == null) return 1.0f;
            
            float bonus = 1.0f;
            
            switch (weapon.effectType)
            {
                case WeaponEffectType.IntimidatingCut:
                    // +extra morale damage if target below 80% morale
                    // This is morale-specific, handled in ApplyPostAttackEffect
                    break;
                    
                case WeaponEffectType.Finisher:
                    // +10% damage if target below 50% HP
                    if (target.HPPercent < 0.5f)
                    {
                        bonus += 0.10f;
                        Debug.Log($"<color=red>Finisher! +10% damage (target at {target.HPPercent:P0} HP)</color>");
                    }
                    break;
                    
                case WeaponEffectType.QuickDraw:
                    // +15% damage if first attack this turn
                    if (isFirstAttack)
                    {
                        bonus += 0.15f;
                        Debug.Log($"<color=yellow>Quick Draw! +15% damage (first attack)</color>");
                    }
                    break;
            }
            
            return bonus;
        }
        
        /// <summary>
        /// Calculate how much damage bypasses Hull (for piercing weapons).
        /// Returns percentage (0.0 to 1.0).
        /// </summary>
        public static float GetHullBypassPercent(WeaponData weapon)
        {
            if (weapon == null) return 0f;
            
            return weapon.effectType switch
            {
                WeaponEffectType.PiercingPoint => 0.25f,    // Rapier: 25% bypasses Hull
                WeaponEffectType.ArmorPiercing => 0.60f,    // Musket: 60% bypasses Hull
                _ => 0f
            };
        }
        
        /// <summary>
        /// Apply weapon effect AFTER attack damage is dealt.
        /// </summary>
        public static void ApplyPostAttackEffect(
            UnitStatus attacker,
            UnitStatus target,
            WeaponData weapon,
            int damageDealt,
            bool targetDied)
        {
            if (weapon == null || target == null || targetDied) return;
            
            EnsureGridManager();
            
            switch (weapon.effectType)
            {
                case WeaponEffectType.IntimidatingCut:
                    ApplyIntimidatingCut(target);
                    break;
                    
                case WeaponEffectType.Gash:
                    ApplyGash(target, attacker.gameObject);
                    break;
                    
                case WeaponEffectType.Concuss:
                    ApplyConcuss(target);
                    break;
                    
                case WeaponEffectType.DragDown:
                    ApplyDragDown(attacker, target);
                    break;
                    
                case WeaponEffectType.Rattle:
                    ApplyRattle(target);
                    break;
                    
                case WeaponEffectType.Crack:
                    ApplyCrack(target);
                    break;
                    
                case WeaponEffectType.ReelOut:
                    ApplyReelOut(attacker, target);
                    break;
                    
                case WeaponEffectType.LineBreak:
                    ApplyLineBreak(attacker, target, damageDealt);
                    break;
                    
                case WeaponEffectType.KeepBack:
                    ApplyKeepBack(attacker, target);
                    break;
                    
                case WeaponEffectType.FearFactor:
                    ApplyFearFactor(target);
                    break;
                    
                case WeaponEffectType.Scattershot:
                    ApplyScattershot(attacker, target, damageDealt);
                    break;
                    
                case WeaponEffectType.Shrapnel:
                    ApplyShrapnel(target, attacker.gameObject);
                    break;
                    
                case WeaponEffectType.HullBreaker:
                    ApplyHullBreaker(target, damageDealt);
                    break;
                    
                case WeaponEffectType.BadOmen:
                    ApplyBadOmen(target, attacker.gameObject);
                    break;
                    
                case WeaponEffectType.Pilfer:
                    ApplyPilfer(attacker, target);
                    break;
            }
        }
        
        #region Effect Implementations
        
        // === CUTLASS: IntimidatingCut ===
        private static void ApplyIntimidatingCut(UnitStatus target)
        {
            // Extra morale damage if target below 80% morale
            if (target.MoralePercent < 0.8f)
            {
                int bonusMorale = 15;
                target.ApplyMoraleDamage(bonusMorale);
                Debug.Log($"<color=red>Intimidating Cut! +{bonusMorale} bonus morale damage (target at {target.MoralePercent:P0} morale)</color>");
            }
        }
        
        // === AXE: Gash ===
        private static void ApplyGash(UnitStatus target, GameObject source)
        {
            // Apply Bleed (2 turns): 20 damage at end of round
            StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
            if (effectManager != null)
            {
                StatusEffect bleed = new StatusEffect(
                    StatusEffectType.Bleed,
                    "Bleeding",
                    2,
                    20f,
                    0f,
                    source
                );
                effectManager.ApplyEffect(bleed);
                Debug.Log($"<color=red>Gash! Applied Bleed (20 dmg/turn for 2 turns)</color>");
            }
        }
        
        // === HAMMER: Concuss ===
        private static void ApplyConcuss(UnitStatus target)
        {
            // Apply Daze (1 turn): -20% damage next round
            // For now, apply stun for 1 turn as a simplified version
            target.ApplyStun(1);
            Debug.Log($"<color=yellow>Concuss! Target dazed for 1 turn</color>");
        }
        
        // === ANCHOR: DragDown (KNOCKBACK - Pull) ===
        private static void ApplyDragDown(UnitStatus attacker, UnitStatus target)
        {
            // Pull target 1 tile toward attacker
            if (!MoveUnitToward(target, attacker))
            {
                Debug.Log($"<color=yellow>Drag Down! Target couldn't be pulled (blocked)</color>");
            }
            else
            {
                Debug.Log($"<color=cyan>Drag Down! Pulled {target.name} closer!</color>");
            }
        }
        
        // === CLUBS: Rattle ===
        private static void ApplyRattle(UnitStatus target)
        {
            // Apply Rattled (1 turn): +10% morale damage taken
            StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
            if (effectManager != null)
            {
                StatusEffect rattled = new StatusEffect(
                    StatusEffectType.Marked,
                    "Rattled",
                    1,
                    0.10f, // +10% damage taken
                    0f,
                    null
                );
                effectManager.ApplyEffect(rattled);
                Debug.Log($"<color=yellow>Rattle! Target takes +10% morale damage for 1 turn</color>");
            }
        }
        
        // === MACE: Crack ===
        private static void ApplyCrack(UnitStatus target)
        {
            // Apply Cracked (2 turns): +15% Hull damage taken
            int hullCracked = target.CrackHull(0.15f);
            Debug.Log($"<color=orange>Crack! Reduced target's Hull by {hullCracked}</color>");
        }
        
        // === HARPOON: ReelOut (KNOCKBACK - Push) ===
        private static void ApplyReelOut(UnitStatus attacker, UnitStatus target)
        {
            // Push target back 1 tile (away from attacker)
            if (!MoveUnitAway(target, attacker))
            {
                Debug.Log($"<color=yellow>Reel Out! Target couldn't be pushed (blocked)</color>");
            }
            else
            {
                Debug.Log($"<color=cyan>Reel Out! Pushed {target.name} back!</color>");
            }
        }
        
        // === SPEAR: LineBreak ===
        private static void ApplyLineBreak(UnitStatus attacker, UnitStatus target, int damageDealt)
        {
            // If enemy behind target, deal 40% damage to them too
            EnsureGridManager();
            if (gridManager == null) return;
            
            Vector2Int attackerPos = gridManager.WorldToGridPosition(attacker.transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            
            // Calculate direction from attacker to target
            int dirX = System.Math.Sign(targetPos.x - attackerPos.x);
            int dirY = System.Math.Sign(targetPos.y - attackerPos.y);
            
            // Check cell behind target
            Vector2Int behindPos = new Vector2Int(targetPos.x + dirX, targetPos.y + dirY);
            GridCell behindCell = gridManager.GetCell(behindPos.x, behindPos.y);
            
            if (behindCell != null && behindCell.IsOccupied && behindCell.OccupyingUnit != null)
            {
                UnitStatus behindUnit = behindCell.OccupyingUnit.GetComponent<UnitStatus>();
                if (behindUnit != null && behindUnit.Team != attacker.Team)
                {
                    int pierceDamage = Mathf.RoundToInt(damageDealt * 0.4f);
                    behindUnit.TakeDamage(pierceDamage, attacker.gameObject, true);
                    Debug.Log($"<color=red>Line Break! Pierced through to {behindUnit.name} for {pierceDamage} damage!</color>");
                }
            }
        }
        
        // === BOARDING PIKE: KeepBack ===
        private static void ApplyKeepBack(UnitStatus attacker, UnitStatus target)
        {
            // If enemy behind target, prevent target from moving 1 turn
            EnsureGridManager();
            if (gridManager == null) return;
            
            Vector2Int attackerPos = gridManager.WorldToGridPosition(attacker.transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            
            int dirX = System.Math.Sign(targetPos.x - attackerPos.x);
            int dirY = System.Math.Sign(targetPos.y - attackerPos.y);
            
            Vector2Int behindPos = new Vector2Int(targetPos.x + dirX, targetPos.y + dirY);
            GridCell behindCell = gridManager.GetCell(behindPos.x, behindPos.y);
            
            if (behindCell != null && behindCell.IsOccupied)
            {
                UnitStatus behindUnit = behindCell.OccupyingUnit?.GetComponent<UnitStatus>();
                if (behindUnit != null && behindUnit.Team != attacker.Team)
                {
                    target.ApplyTrap(); // Prevent movement
                    Debug.Log($"<color=yellow>Keep Back! {target.name} cannot move next turn (enemy behind)</color>");
                }
            }
        }
        
        // === DIRK: FearFactor ===
        private static void ApplyFearFactor(UnitStatus target)
        {
            // +10% morale damage if target below 50% morale
            if (target.MoralePercent < 0.5f)
            {
                int bonusMorale = 10;
                target.ApplyMoraleDamage(bonusMorale);
                Debug.Log($"<color=purple>Fear Factor! +{bonusMorale} bonus morale damage (target at {target.MoralePercent:P0} morale)</color>");
            }
        }
        
        // === BLUNDERBUSS: Scattershot ===
        private static void ApplyScattershot(UnitStatus attacker, UnitStatus target, int damageDealt)
        {
            // Hit nearby enemies for 20% damage
            EnsureGridManager();
            if (gridManager == null) return;
            
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            Vector2Int[] neighbors = {
                new Vector2Int(targetPos.x + 1, targetPos.y),
                new Vector2Int(targetPos.x - 1, targetPos.y),
                new Vector2Int(targetPos.x, targetPos.y + 1),
                new Vector2Int(targetPos.x, targetPos.y - 1)
            };
            
            int scatterDamage = Mathf.RoundToInt(damageDealt * 0.2f);
            
            foreach (Vector2Int pos in neighbors)
            {
                GridCell cell = gridManager.GetCell(pos.x, pos.y);
                if (cell != null && cell.IsOccupied && cell.OccupyingUnit != null)
                {
                    UnitStatus nearbyUnit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                    if (nearbyUnit != null && nearbyUnit.Team != attacker.Team && nearbyUnit != target)
                    {
                        nearbyUnit.TakeDamage(scatterDamage, attacker.gameObject, false);
                        Debug.Log($"<color=orange>Scattershot! Hit {nearbyUnit.name} for {scatterDamage} splash damage!</color>");
                    }
                }
            }
        }
        
        // === GRENADE: Shrapnel ===
        private static void ApplyShrapnel(UnitStatus target, GameObject source)
        {
            // Apply Marked (2 turns): next 2 hits deal +15% damage
            StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
            if (effectManager != null)
            {
                StatusEffect marked = StatusEffect.CreateMarked(2, 0.15f, source);
                effectManager.ApplyEffect(marked);
                Debug.Log($"<color=yellow>Shrapnel! Target marked for +15% damage for 2 turns</color>");
            }
        }
        
        // === CANNONBALL: HullBreaker ===
        private static void ApplyHullBreaker(UnitStatus target, int damageDealt)
        {
            // +50% damage to Hull handled in damage calculation
            // If Hull breaks, nearby targets take 20% damage
            if (target.CurrentHullPool <= 0 && target.MaxHullPool > 0)
            {
                EnsureGridManager();
                if (gridManager == null) return;
                
                Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
                Vector2Int[] neighbors = {
                    new Vector2Int(targetPos.x + 1, targetPos.y),
                    new Vector2Int(targetPos.x - 1, targetPos.y),
                    new Vector2Int(targetPos.x, targetPos.y + 1),
                    new Vector2Int(targetPos.x, targetPos.y - 1)
                };
                
                int splashDamage = Mathf.RoundToInt(damageDealt * 0.2f);
                
                foreach (Vector2Int pos in neighbors)
                {
                    GridCell cell = gridManager.GetCell(pos.x, pos.y);
                    if (cell != null && cell.IsOccupied && cell.OccupyingUnit != null)
                    {
                        UnitStatus nearbyUnit = cell.OccupyingUnit.GetComponent<UnitStatus>();
                        if (nearbyUnit != null && nearbyUnit != target)
                        {
                            nearbyUnit.TakeDamage(splashDamage, null, false);
                            Debug.Log($"<color=red>Hull Breaker splash! {nearbyUnit.name} took {splashDamage} damage!</color>");
                        }
                    }
                }
            }
        }
        
        // === CURSED BIRD: BadOmen ===
        private static void ApplyBadOmen(UnitStatus target, GameObject source)
        {
            // Apply Omen (2 turns): +20 morale damage when losing morale
            target.ApplyCurse(1.2f); // 20% increased damage
            Debug.Log($"<color=purple>Bad Omen! Target cursed (takes +20% morale damage) for 2 turns</color>");
        }
        
        // === CURSED MONKEY: Pilfer ===
        private static void ApplyPilfer(UnitStatus attacker, UnitStatus target)
        {
            // Steal 1 Grog from enemy, or +10 Buzz if none
            // Since enemies don't have grog, just increase their buzz
            target.ReduceBuzz(-10); // Negative reduction = increase
            Debug.Log($"<color=green>Pilfer! Increased target's Buzz by 10</color>");
        }
        
        #endregion
        
        #region Movement Helpers (Knockback)
        
        /// <summary>
        /// Move a unit 1 tile TOWARD another unit.
        /// Returns true if movement was successful.
        /// </summary>
        private static bool MoveUnitToward(UnitStatus target, UnitStatus toward)
        {
            return MoveUnit(target, toward, pullToward: true);
        }
        
        /// <summary>
        /// Move a unit 1 tile AWAY from another unit.
        /// Returns true if movement was successful.
        /// </summary>
        private static bool MoveUnitAway(UnitStatus target, UnitStatus away)
        {
            return MoveUnit(target, away, pullToward: false);
        }
        
        /// <summary>
        /// Core movement logic for push/pull effects.
        /// </summary>
        private static bool MoveUnit(UnitStatus target, UnitStatus reference, bool pullToward)
        {
            EnsureGridManager();
            if (gridManager == null) return false;
            
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);
            Vector2Int refPos = gridManager.WorldToGridPosition(reference.transform.position);
            
            // Calculate direction
            int dirX = System.Math.Sign(refPos.x - targetPos.x);
            int dirY = System.Math.Sign(refPos.y - targetPos.y);
            
            // If pushing away, reverse direction
            if (!pullToward)
            {
                dirX = -dirX;
                dirY = -dirY;
            }
            
            // Prefer X movement if both are non-zero, or use whichever is non-zero
            Vector2Int newPos;
            if (dirX != 0)
            {
                newPos = new Vector2Int(targetPos.x + dirX, targetPos.y);
            }
            else if (dirY != 0)
            {
                newPos = new Vector2Int(targetPos.x, targetPos.y + dirY);
            }
            else
            {
                return false; // Same position
            }
            
            // Check if new position is valid
            GridCell newCell = gridManager.GetCell(newPos.x, newPos.y);
            if (newCell == null || !newCell.CanPlaceUnit())
            {
                // Try the other direction if primary failed
                if (dirY != 0 && dirX != 0)
                {
                    newPos = new Vector2Int(targetPos.x, targetPos.y + dirY);
                    newCell = gridManager.GetCell(newPos.x, newPos.y);
                    if (newCell == null || !newCell.CanPlaceUnit())
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            
            // Get current cell and remove unit
            GridCell currentCell = gridManager.GetCell(targetPos.x, targetPos.y);
            if (currentCell != null)
            {
                currentCell.RemoveUnit();
            }
            
            // Move unit to new position
            target.transform.position = newCell.GetWorldPosition();
            newCell.PlaceUnit(target.gameObject);
            
            // Trigger movement event
            GameEvents.TriggerUnitMoved(target.gameObject, currentCell, newCell);
            
            return true;
        }
        
        private static void EnsureGridManager()
        {
            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }
        }
        
        #endregion
    }
}