using System.Collections.Generic;
using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Enums;
using TacticalGame.Grid;
using TacticalGame.Managers;
using TacticalGame.Units;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Per-effect playability rules for cards.
    /// Decides whether a card can be played given the current board state.
    ///
    /// Two layers of checks:
    ///   1. Basic: energy available, owner alive, not surrendered.
    ///   2. Per-effect: role-specific conditions from the design doc.
    ///
    /// Each role's effects are added in batches as they get verified in-game.
    /// Currently implemented: Captain (12 cards).
    /// </summary>
    public static class CardPlayabilityChecker
    {
        /// <summary>
        /// Reason a card is unplayable. Empty string means playable.
        /// </summary>
        public struct Result
        {
            public bool isPlayable;
            public bool hasEnergy;
            public string reason;

            public static Result Playable => new Result { isPlayable = true, hasEnergy = true, reason = "" };
        }

        /// <summary>
        /// Check whether a card can be played by its owner right now.
        /// </summary>
        public static Result Check(BattleCard card, UnitStatus selectedUnit)
        {
            if (card == null)
                return new Result { isPlayable = false, hasEnergy = false, reason = "No card" };

            if (selectedUnit == null)
                return new Result { isPlayable = false, hasEnergy = false, reason = "No unit selected" };

            if (!card.BelongsTo(selectedUnit))
                return new Result { isPlayable = false, hasEnergy = false, reason = "Not your card" };

            if (card.ownerUnit == null || card.ownerUnit.HasSurrendered)
                return new Result { isPlayable = false, hasEnergy = false, reason = "Owner unavailable" };

            // Energy check — tracked separately so UI can color the cost text red.
            var energyManager = ServiceLocator.Get<EnergyManager>();
            bool hasEnergy = energyManager != null && energyManager.HasEnergy(card.energyCost);

            if (card.effectType == RelicEffectType.Boots_FreeIfGrog && energyManager != null && energyManager.GrogTokens > 0)
            {
                hasEnergy = true;
            }
            // Per-effect conditions
            string effectReason = CheckEffectCondition(card);
            if (!string.IsNullOrEmpty(effectReason))
                return new Result { isPlayable = false, hasEnergy = hasEnergy, reason = effectReason };

            if (!hasEnergy)
                return new Result { isPlayable = false, hasEnergy = false, reason = "Not enough energy" };

            return Result.Playable;
        }

        /// <summary>
        /// Check per-effect conditions. Returns empty string if the effect is playable.
        /// </summary>
        private static string CheckEffectCondition(BattleCard card)
        {
            // Weapon cards: need a living enemy to exist on the board.
            if (card.IsWeaponCard)
            {
                if (!HasAnyLivingEnemy(card.ownerUnit))
                    return "No enemies remaining";
                return "";
            }

            switch (card.effectType)
            {
                // ==================== CAPTAIN ====================

                // Boots V1: "Swap location with another unit"
                // User rule: unplayable if no other allies alive
                case RelicEffectType.Boots_SwapWithUnit:
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies to swap with";
                    return "";

                // Boots V2: "Move any allied unit 2 tiles"
                // User rule: unplayable if no other allies alive
                case RelicEffectType.Boots_MoveAlly:
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies to move";
                    return "";

                // Gloves V1: "Attack, enemy draws 1 less next turn"
                // User rule: unplayable if no enemy in weapon range
                case RelicEffectType.Gloves_AttackReduceEnemyDraw:
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";

                // Gloves V2: "Attack with weapon, enemy next card costs +1 energy"
                // User rule: unplayable if no enemy in weapon range
                case RelicEffectType.Gloves_AttackIncreaseEnemyCost:
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";

                // Hat V1: "Draw 2, take 200% damage for 2 turns"
                // User rule: always playable
                case RelicEffectType.Hat_DrawCardsVulnerable:
                    return "";

                // Hat V2: "Draw an ultimate ability"
                // User rule: unplayable if no ultimate card exists in hand/deck/discard
                case RelicEffectType.Hat_DrawUltimate:
                    if (!AnyUltimateCardAvailable())
                        return "No ultimate cards remaining";
                    return "";

                // Coat V1: "Allies in 1 tile radius +20% Aim/Power for 2 turns"
                // User rule: always playable (does nothing if no allies nearby)
                case RelicEffectType.Coat_BuffNearbyAimPower:
                    return "";

                // Coat V2: "For each enemy attack (3 max) next 2 turns, draw a card and enemy discards 1"
                // User rule: always playable
                case RelicEffectType.Coat_DrawOnEnemyAttack:
                    return "";

                // Totem V1: "Summon cannon, 250 HP, attacks random enemy"
                // User rule: unplayable if no empty tile on player side
                case RelicEffectType.Totem_SummonCannon:
                    if (!HasAnyEmptyTileOnPlayerSide())
                        return "No empty tile on player side";
                    return "";

                // Totem V2: "Curse enemy captain this turn, damage reflects to other enemies"
                case RelicEffectType.Totem_CurseCaptainReflect:
                    if (!HasEnemyCaptain(card.ownerUnit))
                        return "Requires an Enemy Captain";
                    return "";

                // Ultimate V1: "Fire ship cannon: 3 shots, 200 damage + fire hazard"
                // User rule: always playable
                case RelicEffectType.Ultimate_ShipCannon:
                    return "";

                // Ultimate V2: "Attack enemy captain with weapon, mark as only target this turn"
                case RelicEffectType.Ultimate_MarkCaptainOnly:
                    if (!HasEnemyCaptain(card.ownerUnit))
                        return "Requires an Enemy Captain";
                    return "";

                // ==================== QUARTERMASTER ====================

                // --- BOOTS ---
                case RelicEffectType.Boots_MoveRestoreMorale:
                    return ""; // Moves and restores morale, always playable

                case RelicEffectType.Boots_AllyFreeMoveLowestMorale:
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies available to move";
                    return "";

                // --- GLOVES ---
                case RelicEffectType.Gloves_AttackBonusByMissingMorale: // V1
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";
                    
                case RelicEffectType.Gloves_AttackMarkMoraleFocus: // V2
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";

                // --- HAT ---
                case RelicEffectType.Hat_RestoreMoraleLowest:
                    var unitsForHat = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
                    UnitStatus lowestAllyForHat = null;
                    float lowestMoraleForHat = float.MaxValue;

                    foreach (var u in unitsForHat)
                    {
                        if (u != null && u.Team == card.ownerUnit.Team && u != card.ownerUnit && !u.HasSurrendered && u.CurrentHP > 0)
                        {
                            if (u.MoralePercent < lowestMoraleForHat)
                            {
                                lowestMoraleForHat = u.MoralePercent;
                                lowestAllyForHat = u;
                            }
                        }
                    }

                    // If no other valid ally was found, lock the card
                    if (lowestAllyForHat == null)
                    {
                        return "No other allies to rally";
                    }
                    
                    // If an ally was found, check their morale percentage
                    float missingMoralePercent = 1f - lowestAllyForHat.MoralePercent;
                    if (missingMoralePercent <= 0.30f)
                    {
                        return "Lowest morale ally is missing 30% or less morale.";
                    }
                    
                    return "";

// ==================== SURGEON ====================
                case RelicEffectType.Boots_V2_SwapLowestHealthAlly:
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies to swap with";
                    return "";

                case RelicEffectType.Ultimate_V2_FullHealthRestore:
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No allies to heal";
                    return "";
                    
                case RelicEffectType.Hat_RestoreMoraleNearby:
                    return ""; // Always playable

                // --- COAT ---
                case RelicEffectType.Coat_ReduceMoraleDamage:
                    return ""; // Global buff, always playable

                case RelicEffectType.Coat_PreventSurrender:
                    return ""; // Always playable

                // --- TOTEM ---
                case RelicEffectType.Totem_RallyNoMoraleDamage: // V1
                    if (!HasLivingQuartermaster(card.ownerUnit))
                        return "Requires a living Quartermaster";
                    return "";

                case RelicEffectType.Totem_SummonHighQualityRum: // V2
                    return ""; // Always playable

                // --- ULTIMATE ---
                case RelicEffectType.Ultimate_ReflectMoraleDamage:
                    return ""; // Global buff, always playable

                case RelicEffectType.Ultimate_ReviveAlly:
                    if (!HasAnyDeadOrSurrenderedAlly(card.ownerUnit))
                        return "No dead or surrendered allies to revive";
                    return "";

                // ==================== HELMSMAN ====================
                case RelicEffectType.Totem_ConvertGrogToEnergy:
                    var energyManager = ServiceLocator.Get<EnergyManager>();
                    if (energyManager == null || energyManager.GrogTokens < 2)
                        return "Requires 2 Grog";
                    return "";

                // ==================== BOATSWAIN ====================
                case RelicEffectType.Gloves_AttackBonusIfMoreHP: // V1
                case RelicEffectType.Gloves_AttackLowerEnemyHealth: // V2
                case RelicEffectType.Ultimate_IgnoreHighestHP: // V2
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";

                case RelicEffectType.Coat_ProtectLowHP: // V2
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies to protect";
                    return "";

                case RelicEffectType.Totem_SummonAnchorHealthBuff: // V2
                case RelicEffectType.Ultimate_SummonHardObstacles: // V1
                    if (!HasAnyEmptyTileOnPlayerSide())
                        return "No empty tiles available";
                    return "";

                // ==================== COOK ====================
                case RelicEffectType.Gloves_V2_StasisClosest:
                    if (!HasAnyLivingEnemy(card.ownerUnit) && !HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other units to put in stasis";
                    return "";

                case RelicEffectType.Hat_ReduceLowestAllyCardCost: // V1
                case RelicEffectType.Coat_StunOnAllyAttacked: // V1
                    if (!HasAnyOtherLivingAlly(card.ownerUnit))
                        return "No other allies available";
                    return "";

                case RelicEffectType.Ultimate_SwapHealthClosest: // V1
                case RelicEffectType.Ultimate_V2_FireColumn: // V2
                    if (!HasAnyLivingEnemy(card.ownerUnit))
                        return "No enemies remaining";
                    return "";

                // ==================== SWASHBUCKLER ====================
                // Ultimate V1: "Lowest health unit and captain attack each other"
                // Unplayable if there is no enemy captain
                case RelicEffectType.Ultimate_ForceLowestAndCaptainFight:
                    if (!HasEnemyCaptain(card.ownerUnit))
                        return "No enemy captain on the board";
                    return "";

                // ==================== OTHER ROLES ====================
                // Will be added in subsequent batches. Default to playable so
                // untested cards don't silently lock themselves out.
                default:
                    return "";
            }
        }

        #region Condition Helpers

        /// <summary>
        /// Is there at least one other living player ally besides the owner?
        /// </summary>
        private static bool HasAnyOtherLivingAlly(UnitStatus self)
        {
            if (self == null) return false;

            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null || u == self) continue;
                if (u.Team != self.Team) continue;
                if (u.HasSurrendered) continue;
                if (u.CurrentHP <= 0) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Is there at least one living enemy on the board?
        /// (Weapons don't currently use a range field — attacks find the nearest enemy.)
        /// </summary>
        private static bool HasAnyLivingEnemy(UnitStatus self)
        {
            if (self == null) return false;

            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null || u == self) continue;
                if (u.Team == self.Team) continue;
                if (u.HasSurrendered) continue;
                if (u.CurrentHP <= 0) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Is there a living enemy Captain on the board?
        /// </summary>
        private static bool HasEnemyCaptain(UnitStatus self)
        {
            if (self == null) return false;

            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null || u == self) continue;
                if (u.Team == self.Team) continue;
                if (u.HasSurrendered) continue;
                if (u.CurrentHP <= 0) continue;
                if (u.IsCaptain) return true;
            }
            return false;
        }

        /// <summary>
        /// Is there at least one empty placement tile on the player side of the grid?
        /// </summary>
        private static bool HasAnyEmptyTileOnPlayerSide()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;

            int middleCol = gridManager.GetMiddleColumnIndex();
            for (int x = 0; x < middleCol; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;
                    if (cell.IsOccupied) continue;
                    if (cell.IsBlocked) continue;
                    if (cell.HasHazard) continue;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Is there any ultimate-category card anywhere in the shared deck/hand/discard?
        /// </summary>
        private static bool AnyUltimateCardAvailable()
        {
            var deckManager = BattleDeckManager.Instance;
            if (deckManager == null) return false;

            return ContainsUltimate(deckManager.Deck)
                || ContainsUltimate(deckManager.Hand)
                || ContainsUltimate(deckManager.DiscardPile);
        }

        private static bool ContainsUltimate(IReadOnlyList<BattleCard> cards)
        {
            if (cards == null) return false;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c != null && c.category == RelicCategory.Ultimate)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Is there a living Quartermaster on the same team?
        /// Used for Quartermaster Totem checks.
        /// </summary>
        private static bool HasLivingQuartermaster(UnitStatus self)
        {
            if (self == null) return false;
            if (self.Role == UnitRole.Quartermaster) return true; // Caster is the Quartermaster

            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null || u.Team != self.Team) continue;
                if (u.HasSurrendered || u.CurrentHP <= 0) continue;
                if (u.Role == UnitRole.Quartermaster) return true;
            }
            return false;
        }

        /// <summary>
        /// Is there at least one allied unit that is dead or surrendered?
        /// Used for the Quartermaster Ultimate Revive ability.
        /// </summary>
        private static bool HasAnyDeadOrSurrenderedAlly(UnitStatus self)
        {
            if (self == null) return false;

            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null) continue;
                if (u.Team == self.Team && (u.HasSurrendered || u.CurrentHP <= 0))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}