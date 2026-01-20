using UnityEngine;
using TacticalGame.Config;
using TacticalGame.Units;
using TacticalGame.Equipment;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles all damage calculations.
    /// Integrates with StatusEffectManager for buff/debuff modifiers.
    /// Integrates with PassiveRelicManager for passive relic modifiers.
    /// </summary>
    public static class DamageCalculator
    {
        public struct DamageResult
        {
            public int FinalHPDamage;
            public int FinalMoraleDamage;
            public int HullDamageAbsorbed;
            public float GritDRApplied;
            public string HPBreakdown;
            public string MoraleBreakdown;
        }

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
            
            // === STATUS EFFECT OUTGOING DAMAGE MODIFIER (attacker buffs) ===
            if (attackerStatus != null)
            {
                var attackerEffects = attackerStatus.GetComponent<StatusEffectManager>();
                if (attackerEffects != null)
                {
                    // Damage boost buff
                    float outgoingMod = attackerEffects.GetOutgoingDamageModifier();
                    if (outgoingMod != 0f)
                    {
                        hpDamageMod += outgoingMod;
                        logHP += $" +{Mathf.RoundToInt(outgoingMod * 100)}%(DamageBuff)";
                    }
                    
                    // Stat buffs applied to scaling
                    float powerMod = attackerEffects.GetPowerModifier();
                    float aimMod = attackerEffects.GetAimModifier();
                    float statBonus = isMelee ? powerMod * 0.03f : aimMod * 0.03f;
                    if (statBonus != 0f)
                    {
                        hpDamageMod += statBonus;
                        logHP += $" +{Mathf.RoundToInt(statBonus * 100)}%(StatBuff)";
                    }
                }
            }
            
            // === PASSIVE RELIC OUTGOING DAMAGE MODIFIER ===
            if (attackerStatus != null)
            {
                var attackerPassive = attackerStatus.GetComponent<PassiveRelicManager>();
                if (attackerPassive != null)
                {
                    float passiveOutgoing = attackerPassive.GetOutgoingDamageModifier(targetStatus);
                    if (passiveOutgoing != 1f)
                    {
                        hpDamageMod *= passiveOutgoing;
                        logHP += $" x{passiveOutgoing:F2}(PassiveRelic)";
                    }
                }
            }
            
            // === STATUS EFFECT INCOMING DAMAGE MODIFIER (target debuffs/buffs) ===
            if (targetStatus != null)
            {
                var targetEffects = targetStatus.GetComponent<StatusEffectManager>();
                if (targetEffects != null)
                {
                    // Vulnerable/Protected
                    float incomingMod = targetEffects.GetIncomingDamageModifier();
                    if (incomingMod != 0f)
                    {
                        hpDamageMod += incomingMod;
                        if (incomingMod > 0)
                            logHP += $" +{Mathf.RoundToInt(incomingMod * 100)}%(Vulnerable)";
                        else
                            logHP += $" {Mathf.RoundToInt(incomingMod * 100)}%(Protected)";
                    }
                    
                    // Marked bonus
                    float markedBonus = targetEffects.GetMarkedBonus();
                    if (markedBonus > 0f)
                    {
                        hpDamageMod += markedBonus;
                        logHP += $" +{Mathf.RoundToInt(markedBonus * 100)}%(Marked)";
                    }
                    
                    // Ranged damage reduction
                    if (!isMelee)
                    {
                        float rangedReduction = targetEffects.GetRangedDamageReduction();
                        if (rangedReduction > 0f)
                        {
                            hpDamageMod -= rangedReduction;
                            logHP += $" -{Mathf.RoundToInt(rangedReduction * 100)}%(RangedShield)";
                        }
                    }
                }
            }
            
            // === PASSIVE RELIC INCOMING DAMAGE MODIFIER ===
            if (targetStatus != null)
            {
                var targetPassive = targetStatus.GetComponent<PassiveRelicManager>();
                if (targetPassive != null)
                {
                    float passiveIncoming = targetPassive.GetIncomingDamageModifier(attackerStatus);
                    if (passiveIncoming != 1f)
                    {
                        hpDamageMod *= passiveIncoming;
                        logHP += $" x{passiveIncoming:F2}(DefensiveRelic)";
                    }
                }
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
            result.FinalHPDamage = Mathf.Max(0, calculatedHPDamage);
            
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
            
            // === STATUS EFFECT MORALE DAMAGE REDUCTION ===
            if (targetStatus != null)
            {
                var targetEffects = targetStatus.GetComponent<StatusEffectManager>();
                if (targetEffects != null)
                {
                    float moraleReduction = targetEffects.GetMoraleDamageReduction();
                    if (moraleReduction > 0f)
                    {
                        moraleDamageMod -= moraleReduction;
                        logMorale += $" -{Mathf.RoundToInt(moraleReduction * 100)}%(MoraleShield)";
                    }
                }
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
            result.FinalMoraleDamage = Mathf.Max(0, calculatedMoraleDamage);
            
            return result;
        }

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

        public static int GetMeleeBaseDamage(UnitStatus attacker, int weaponBaseDamage = 0)
        {
            var config = GameConfig.Instance;
            
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : config.meleeBaseDamage;
            
            // Get effective Power with status effect modifier
            float effectivePower = attacker.Power;
            var effects = attacker.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                effectivePower += effects.GetPowerModifier();
            }
            
            float powerMultiplier = 1f + (effectivePower * config.powerScalingPercent);
            int scaledDamage = Mathf.RoundToInt(baseDmg * powerMultiplier);
            
            // Apply drunk penalty (unless buzz downside is disabled)
            float drunkMod = 1.0f;
            if (attacker.IsTooDrunk && !HasNoBuzzDownside(attacker))
            {
                drunkMod = config.drunkDamageMultiplier;
            }
            
            return Mathf.RoundToInt(scaledDamage * drunkMod);
        }

        public static int GetRangedBaseDamage(UnitStatus attacker, int weaponBaseDamage = 0)
        {
            var config = GameConfig.Instance;
            
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : config.rangedBaseDamage;
            
            // Get effective Aim with status effect modifier
            float effectiveAim = attacker.Aim;
            var effects = attacker.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                effectiveAim += effects.GetAimModifier();
            }
            
            float aimMultiplier = 1f + (effectiveAim * config.aimScalingPercent);
            int scaledDamage = Mathf.RoundToInt(baseDmg * aimMultiplier);
            
            // Apply drunk penalty (unless buzz downside is disabled)
            float drunkMod = 1.0f;
            if (attacker.IsTooDrunk && !HasNoBuzzDownside(attacker))
            {
                drunkMod = config.drunkDamageMultiplier;
            }
            
            return Mathf.RoundToInt(scaledDamage * drunkMod);
        }

        public static float GetComboMultiplier(int skill, int comboCount, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            int n = Mathf.Clamp(comboCount, 1, config.maxComboChain);
            
            if (n <= 1) return 1f;
            
            float comboStep = Mathf.Clamp(
                skill * config.skillComboMultiplier,
                config.comboStepMin,
                config.comboStepMax
            );
            
            float comboMult = 1f + ((n - 1) * comboStep);
            
            return comboMult;
        }

        public static float GetGritDamageReduction(UnitStatus target, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            float hpPercent = target.HPPercent;
            float moralePercent = target.MoralePercent;
            
            // Get effective Grit with status effect modifier
            float effectiveGrit = target.Grit;
            var effects = target.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                effectiveGrit += effects.GetGritModifier();
            }
            
            float gritFactor = ((1f - hpPercent) * config.gritLowHPWeight) + 
                               (moralePercent * config.gritMoraleWeight);
            
            float dr = gritFactor * (effectiveGrit * config.gritPerPointPercent);
            
            return Mathf.Min(dr, config.gritDRCap);
        }

        public static float GetTacticsPotencyMultiplier(int tactics, GameConfig config = null)
        {
            if (config == null) config = GameConfig.Instance;
            
            return 1f + (tactics * config.tacticsScalingPercent);
        }

        public static int ApplyTacticsScaling(int baseValue, int tactics)
        {
            float multiplier = GetTacticsPotencyMultiplier(tactics);
            return Mathf.RoundToInt(baseValue * multiplier);
        }

        /// <summary>
        /// Check if attacker should miss due to effects.
        /// </summary>
        public static bool ShouldMiss(UnitStatus attacker)
        {
            if (attacker == null) return false;
            
            var effects = attacker.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                float missChance = effects.GetMissChance();
                if (missChance > 0 && Random.value < missChance)
                {
                    Debug.Log($"<color=red>{attacker.name} missed due to {missChance*100}% miss chance!</color>");
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Check if target should be ignored (invisible effect).
        /// </summary>
        public static bool IsTargetIgnored(UnitStatus target)
        {
            if (target == null) return false;
            
            var effects = target.GetComponent<StatusEffectManager>();
            return effects != null && effects.HasEffect(StatusEffectType.IgnoredByEnemies);
        }

        /// <summary>
        /// Check if attacker must target closest enemy.
        /// </summary>
        public static bool MustTargetClosest(UnitStatus attacker)
        {
            if (attacker == null) return false;
            
            var effects = attacker.GetComponent<StatusEffectManager>();
            return effects != null && effects.HasEffect(StatusEffectType.ForceTargetClosest);
        }

        #region Passive Relic Helpers
        
        public static float GetSurrenderThreshold(UnitStatus unit)
        {
            if (unit == null) return 0.2f;
            
            var passiveManager = unit.GetComponent<PassiveRelicManager>();
            if (passiveManager != null)
            {
                return passiveManager.GetSurrenderThreshold();
            }
            
            return 0.2f;
        }

        public static bool IsImmuneMoraleFocusFire(UnitStatus unit)
        {
            if (unit == null) return false;
            
            var passiveManager = unit.GetComponent<PassiveRelicManager>();
            return passiveManager != null && passiveManager.IsImmuneMoraleFocusFire();
        }

        public static bool HasNoBuzzDownside(UnitStatus unit)
        {
            if (unit == null) return false;
            
            var passiveManager = unit.GetComponent<PassiveRelicManager>();
            return passiveManager != null && passiveManager.HasNoBuzzDownside();
        }
        
        #endregion
    }
}