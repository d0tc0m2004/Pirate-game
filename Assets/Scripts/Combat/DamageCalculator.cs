using UnityEngine;
using TacticalGame.Config;
using TacticalGame.Units;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles all damage calculations. Extracted from UnitStatus for single responsibility.
    /// 
    /// FORMULAS FROM STAT TABLE:
    /// - Power: MeleeOutput = Base × (1 + Power × 0.03)
    /// - Aim: RangedOutput = Base × (1 + Aim × 0.03)
    /// - Skill: ComboMult(n) = 1 + (n-1) × ComboStep, where ComboStep = clamp(Skill × 0.003, 0.02, 0.12)
    /// - Tactics: Potency = Base × (1 + Tactics × 0.04) [for heals/buffs]
    /// - Grit: DR = min(0.40, GritFactor × Grit/100), GritFactor = (1-HP%)×0.50 + Morale%×0.40
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
            public int HullDamageAbsorbed;
            public float GritDRApplied;
            public string HPBreakdown;
            public string MoraleBreakdown;
        }

        /// <summary>
        /// Calculate damage for an attack.
        /// </summary>
        public static DamageResult Calculate(
            int baseDamage,
            bool isMelee,
            UnitStatus attackerStatus,
            UnitStatus targetStatus,
            bool hasCover,
            bool isFirstAction = false,
            int comboCount = 1,
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
            
            // Combo bonus (Skill)
            if (comboCount > 1 && attackerStatus != null)
            {
                float comboMult = GetComboMultiplier(attackerStatus.Skill, comboCount, config);
                if (comboMult > 1f)
                {
                    hpDamageMod *= comboMult;
                    logHP += $" x{comboMult:F2}(Combo x{comboCount})";
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
            
            // Calculate HP damage before DR
            int calculatedHPDamage = Mathf.RoundToInt(baseDamage * hpDamageMod * hpTypeMultiplier * curseMultiplier * exposedMultiplier);
            calculatedHPDamage += flatBonusHP;
            
            if (flatBonusHP > 0)
            {
                logHP += $" +{flatBonusHP}(HazardBonus)";
            }
            
            result.HPBreakdown = logHP;
            result.FinalHPDamage = calculatedHPDamage;
            
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
            
            // Combo bonus applies to morale too
            if (comboCount > 1 && attackerStatus != null)
            {
                float comboMult = GetComboMultiplier(attackerStatus.Skill, comboCount, config);
                if (comboMult > 1f)
                {
                    moraleDamageMod *= comboMult;
                    logMorale += $" x{comboMult:F2}(Combo x{comboCount})";
                }
            }
            
            // Cover reduction
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
            calculatedMoraleDamage += flatBonusMorale;
            
            if (flatBonusMorale > 0)
            {
                logMorale += $" +{flatBonusMorale}(HazardBonus)";
            }
            
            result.MoraleBreakdown = logMorale;
            result.FinalMoraleDamage = calculatedMoraleDamage;
            
            return result;
        }

        /// <summary>
        /// Overload for backward compatibility.
        /// </summary>
        public static DamageResult Calculate(
            int baseDamage,
            bool isMelee,
            UnitStatus targetStatus,
            bool hasCover,
            int flatBonusHP = 0,
            int flatBonusMorale = 0)
        {
            return Calculate(baseDamage, isMelee, null, targetStatus, hasCover, false, 1, flatBonusHP, flatBonusMorale);
        }

        /// <summary>
        /// Calculate base melee damage from attacker stats.
        /// Formula: Base × (1 + Power × 0.03)
        /// </summary>
        public static int GetMeleeBaseDamage(UnitStatus attacker, int weaponBaseDamage = 0)
        {
            var config = GameConfig.Instance;
            
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : config.meleeBaseDamage;
            
            // Apply Power scaling: Base × (1 + Power × 0.03)
            float powerMultiplier = 1f + (attacker.Power * config.powerScalingPercent);
            int scaledDamage = Mathf.RoundToInt(baseDmg * powerMultiplier);
            
            // Apply drunk penalty
            float drunkMod = attacker.IsTooDrunk ? config.drunkDamageMultiplier : 1.0f;
            
            return Mathf.RoundToInt(scaledDamage * drunkMod);
        }

        /// <summary>
        /// Calculate base ranged damage from attacker stats.
        /// Formula: Base × (1 + Aim × 0.03)
        /// </summary>
        public static int GetRangedBaseDamage(UnitStatus attacker, int weaponBaseDamage = 0)
        {
            var config = GameConfig.Instance;
            
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : config.rangedBaseDamage;
            
            // Apply Aim scaling: Base × (1 + Aim × 0.03)
            float aimMultiplier = 1f + (attacker.Aim * config.aimScalingPercent);
            int scaledDamage = Mathf.RoundToInt(baseDmg * aimMultiplier);
            
            // Apply drunk penalty
            float drunkMod = attacker.IsTooDrunk ? config.drunkDamageMultiplier : 1.0f;
            
            return Mathf.RoundToInt(scaledDamage * drunkMod);
        }

        /// <summary>
        /// Calculate combo multiplier based on Skill stat.
        /// Formula: ComboMult(n) = 1 + (n-1) × ComboStep
        /// Where: ComboStep = clamp(Skill × 0.003, 0.02, 0.12)
        /// </summary>
        public static float GetComboMultiplier(int skill, int comboCount, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            // Clamp combo count
            int n = Mathf.Clamp(comboCount, 1, config.maxComboChain);
            
            if (n <= 1) return 1f;
            
            // Calculate ComboStep
            float comboStep = Mathf.Clamp(
                skill * config.skillComboMultiplier,
                config.comboStepMin,
                config.comboStepMax
            );
            
            // ComboMult(n) = 1 + (n-1) × ComboStep
            float comboMult = 1f + ((n - 1) * comboStep);
            
            return comboMult;
        }

        /// <summary>
        /// Calculate Grit damage reduction.
        /// Formula: 
        ///   GritFactor = (1-HP%) × 0.50 + Morale% × 0.40
        ///   DR = min(0.40, GritFactor × (Grit/100))
        /// </summary>
        public static float GetGritDamageReduction(UnitStatus target, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            float hpPercent = target.HPPercent;
            float moralePercent = target.MoralePercent;
            
            // GritFactor rewards low HP but high morale
            float gritFactor = ((1f - hpPercent) * config.gritLowHPWeight) + 
                               (moralePercent * config.gritMoraleWeight);
            
            // DR = GritFactor × (Grit / 100)
            float dr = gritFactor * (target.Grit * config.gritPerPointPercent);
            
            // Cap at max DR
            return Mathf.Min(dr, config.gritDRCap);
        }

        /// <summary>
        /// Calculate Tactics potency multiplier for heals/buffs/debuffs.
        /// Formula: Potency = Base × (1 + Tactics × 0.04)
        /// </summary>
        public static float GetTacticsPotencyMultiplier(int tactics, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            return 1f + (tactics * config.tacticsScalingPercent);
        }

        /// <summary>
        /// Apply Tactics scaling to a base value (for heals, shields, buffs, etc.)
        /// </summary>
        public static int ApplyTacticsScaling(int baseValue, int tactics)
        {
            float multiplier = GetTacticsPotencyMultiplier(tactics);
            return Mathf.RoundToInt(baseValue * multiplier);
        }
    }
}