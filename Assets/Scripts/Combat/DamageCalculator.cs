using UnityEngine;
using TacticalGame.Config;
using TacticalGame.Units;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles all damage calculations. Extracted from UnitStatus for single responsibility.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Result of a damage calculation with all details for logging.
        /// </summary>
        public struct DamageResult
        {
            public int FinalHPDamage;
            public int FinalMoraleDamage;
            public string HPBreakdown;
            public string MoraleBreakdown;
        }

        /// <summary>
        /// Calculate damage for an attack.
        /// </summary>
        /// <param name="baseDamage">Base damage before modifiers</param>
        /// <param name="isMelee">True for melee, false for ranged</param>
        /// <param name="attackerStatus">The attacking unit (for first-action bonus)</param>
        /// <param name="targetStatus">The target unit</param>
        /// <param name="hasCover">Does target have adjacency cover</param>
        /// <param name="isFirstAction">Did attacker's team go first this round</param>
        /// <param name="flatBonusHP">Flat HP damage bonus (from hazards etc)</param>
        /// <param name="flatBonusMorale">Flat morale damage bonus</param>
        public static DamageResult Calculate(
            int baseDamage,
            bool isMelee,
            UnitStatus attackerStatus,
            UnitStatus targetStatus,
            bool hasCover,
            bool isFirstAction = false,
            int flatBonusHP = 0,
            int flatBonusMorale = 0)
        {
            var config = GameConfig.Instance;
            var result = new DamageResult();
            
            string logHP = $"{baseDamage} Base";
            string logMorale = $"{baseDamage} Base";
            
            // === HP DAMAGE CALCULATION ===
            float hpDamageMod = 1.0f;
            
            // First-action bonus (Speed)
            // Formula: min(15%, Speed × 0.2%) bonus damage if team went first
            if (isFirstAction && attackerStatus != null)
            {
                float firstBonus = Mathf.Min(
                    config.firstActionBonusCap, 
                    attackerStatus.Speed * config.firstActionBonusPerSpeed
                );
                if (firstBonus > 0)
                {
                    hpDamageMod += firstBonus;
                    logHP += $" +{Mathf.RoundToInt(firstBonus * 100)}%(FirstAction)";
                }
            }
            
            // Cover reduction
            if (hasCover)
            {
                hpDamageMod -= config.adjacencyCoverReduction;
                logHP += $" -{Mathf.RoundToInt(config.adjacencyCoverReduction * 100)}%(Cover)";
            }
            
            // Ranged bonus to HP (+10%)
            float hpTypeMultiplier = isMelee ? 1.0f : config.rangedHPMultiplier;
            if (!isMelee)
            {
                logHP += $" +{Mathf.RoundToInt((config.rangedHPMultiplier - 1) * 100)}%(Ranged)";
            }
            
            // Curse multiplier
            float curseMultiplier = targetStatus.IsCursed ? targetStatus.CurseMultiplier : 1.0f;
            if (targetStatus.IsCursed)
            {
                logHP += $" x{curseMultiplier}(Curse)";
            }
            
            // Exposed multiplier
            float exposedMultiplier = targetStatus.IsExposed ? config.exposedDamageMultiplier : 1.0f;
            if (targetStatus.IsExposed)
            {
                logHP += $" +{Mathf.RoundToInt((config.exposedDamageMultiplier - 1) * 100)}%(Exposed)";
            }
            
            // Calculate final HP damage
            int calculatedHPDamage = Mathf.RoundToInt(baseDamage * hpDamageMod * hpTypeMultiplier * curseMultiplier * exposedMultiplier);
            result.FinalHPDamage = calculatedHPDamage + flatBonusHP;
            
            if (flatBonusHP > 0)
            {
                logHP += $" +{flatBonusHP}(HazardBonus)";
            }
            
            result.HPBreakdown = logHP;
            
            // === MORALE DAMAGE CALCULATION ===
            float moraleDamageMod = 1.0f;
            
            // First-action bonus applies to morale too
            if (isFirstAction && attackerStatus != null)
            {
                float firstBonus = Mathf.Min(
                    config.firstActionBonusCap, 
                    attackerStatus.Speed * config.firstActionBonusPerSpeed
                );
                if (firstBonus > 0)
                {
                    moraleDamageMod += firstBonus;
                    logMorale += $" +{Mathf.RoundToInt(firstBonus * 100)}%(FirstAction)";
                }
            }
            
            // Cover reduction (applies to morale too)
            if (hasCover)
            {
                moraleDamageMod -= config.adjacencyCoverReduction;
                logMorale += $" -{Mathf.RoundToInt(config.adjacencyCoverReduction * 100)}%(Cover)";
            }
            
            // Melee bonus to morale (+10%)
            float moraleTypeMultiplier = isMelee ? config.meleeMoraleMultiplier : 1.0f;
            if (isMelee)
            {
                logMorale += $" +{Mathf.RoundToInt((config.meleeMoraleMultiplier - 1) * 100)}%(Melee)";
            }
            
            // Focus fire bonus
            int stackIndex = Mathf.Clamp(targetStatus.FocusFireStacks, 0, config.focusFireMultipliers.Length - 1);
            float focusFireBonus = config.focusFireMultipliers[stackIndex];
            if (focusFireBonus > 0)
            {
                logMorale += $" +{Mathf.RoundToInt(focusFireBonus * 100)}%(FocusFire x{targetStatus.FocusFireStacks})";
            }
            
            // Exposed multiplier
            if (targetStatus.IsExposed)
            {
                logMorale += $" +{Mathf.RoundToInt((config.exposedDamageMultiplier - 1) * 100)}%(Exposed)";
            }
            
            // Calculate final morale damage
            int calculatedMoraleDamage = Mathf.RoundToInt(
                baseDamage * moraleDamageMod * moraleTypeMultiplier * (1.0f + focusFireBonus) * exposedMultiplier
            );
            result.FinalMoraleDamage = calculatedMoraleDamage + flatBonusMorale;
            
            if (flatBonusMorale > 0)
            {
                logMorale += $" +{flatBonusMorale}(HazardBonus)";
            }
            
            result.MoraleBreakdown = logMorale;
            
            return result;
        }

        /// <summary>
        /// Overload for backward compatibility (no attacker/first-action info).
        /// </summary>
        public static DamageResult Calculate(
            int baseDamage,
            bool isMelee,
            UnitStatus targetStatus,
            bool hasCover,
            int flatBonusHP = 0,
            int flatBonusMorale = 0)
        {
            return Calculate(baseDamage, isMelee, null, targetStatus, hasCover, false, flatBonusHP, flatBonusMorale);
        }

        /// <summary>
        /// Calculate base melee damage from attacker stats.
        /// Formula: Base + Power × 1.25
        /// </summary>
        public static int GetMeleeBaseDamage(UnitStatus attacker)
        {
            var config = GameConfig.Instance;
            float drunkMod = attacker.IsTooDrunk ? config.drunkDamageMultiplier : 1.0f;
            int baseDmg = config.meleeBaseDamage + Mathf.RoundToInt(attacker.Power * config.powerScaling);
            return Mathf.RoundToInt(baseDmg * drunkMod);
        }

        /// <summary>
        /// Calculate base ranged damage from attacker stats.
        /// Formula: Base + Aim × 1.35
        /// </summary>
        public static int GetRangedBaseDamage(UnitStatus attacker)
        {
            var config = GameConfig.Instance;
            float drunkMod = attacker.IsTooDrunk ? config.drunkDamageMultiplier : 1.0f;
            int baseDmg = config.rangedBaseDamage + Mathf.RoundToInt(attacker.Aim * config.aimScaling);
            return Mathf.RoundToInt(baseDmg * drunkMod);
        }
    }
}