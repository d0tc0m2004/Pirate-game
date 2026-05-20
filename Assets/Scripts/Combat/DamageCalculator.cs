using UnityEngine;
using TacticalGame.Config;
using TacticalGame.Units;
using TacticalGame.Equipment;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles all damage calculations according to v3 Combat Numbers Rework.
    /// RAW -> MOD -> APPLY pipeline.
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
            int baseDamage, // Already includes Stat scaling (RAW step 1)
            bool isMelee,
            UnitStatus attackerStatus,
            UnitStatus targetStatus,
            bool hasCover,
            bool isFirstAction = false,
            int comboCount = 1,
            int flatBonusHP = 0,
            int flatBonusMorale = 0)
        {
            var result = new DamageResult();
            
            // --- STEP 1: RAW ---
            int skill = attackerStatus != null ? attackerStatus.Skill : 0;
            int comboBonus = Mathf.Min(Mathf.Max(0, comboCount - 1), skill);
            
            int rawDamage = baseDamage + comboBonus;
            
            // --- STEP 2: MOD ---
            int modDamage = rawDamage;
            string breakdown = $"{baseDamage} Base";
            
            if (comboBonus > 0)
                breakdown += $" +{comboBonus}(Combo)";
                
            if (hasCover)
            {
                modDamage -= 1;
                breakdown += " -1(Cover)";
            }
            
            // Identity riders
            int hpDmg = modDamage;
            int moraleRider = 0;
            
            if (isMelee)
            {
                moraleRider += 1;
                breakdown += " +1(Melee Morale Rider)";
            }
            else
            {
                hpDmg += 1;
                breakdown += " +1(Ranged HP Rider)";
            }
            
            hpDmg = Mathf.Max(0, hpDmg + flatBonusHP);
            moraleRider += flatBonusMorale;
            
            // We pass the RAW/MOD HP damage and the flat morale riders back.
            // The APPLY step (Hull -> HP -> Morale) happens in UnitStatus.
            result.FinalHPDamage = hpDmg;
            result.FinalMoraleDamage = moraleRider; // This represents flat riders to be added AFTER HP->Morale conversion
            result.HPBreakdown = breakdown + (flatBonusHP > 0 ? $" +{flatBonusHP}(Bonus)" : "");
            result.MoraleBreakdown = (flatBonusMorale > 0 || moraleRider > 0) ? $"+{moraleRider}(Riders)" : "";
            
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
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : 5; // new default 5
            
            // Flat stat scaling: +Power
            float effectivePower = attacker.Power;
            var effects = attacker.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                // Assuming modifiers will be updated to return flat ints in the future
                effectivePower += Mathf.RoundToInt(effects.GetPowerModifier());
            }
            
            return baseDmg + Mathf.RoundToInt(effectivePower);
        }

        public static int GetRangedBaseDamage(UnitStatus attacker, int weaponBaseDamage = 0)
        {
            int baseDmg = weaponBaseDamage > 0 ? weaponBaseDamage : 5; // new default 5
            
            // Flat stat scaling: +Aim
            float effectiveAim = attacker.Aim;
            var effects = attacker.GetComponent<StatusEffectManager>();
            if (effects != null)
            {
                effectiveAim += Mathf.RoundToInt(effects.GetAimModifier());
            }
            
            return baseDmg + Mathf.RoundToInt(effectiveAim);
        }

        public static float GetComboMultiplier(int skill, int comboCount, GameConfig config = null)
        {
            // Deprecated - replaced by flat combo scaling
            return 1f;
        }

        public static float GetGritDamageReduction(UnitStatus target, GameConfig config = null)
        {
            // Deprecated - replaced by flat Grit subtraction
            return 0f;
        }
        
        public static int GetFlatGritReduction(UnitStatus target)
        {
            if (target.HPPercent < 0.5f)
            {
                return Mathf.Max(0, target.Grit / 2); // Round down, min 0
            }
            return 0;
        }

        public static float GetTacticsPotencyMultiplier(int tactics, GameConfig config = null)
        {
            // Deprecated - replaced by flat tactics
            return 1f;
        }

        public static int ApplyTacticsScaling(int baseValue, int tactics)
        {
            // Tactics: +Tactics÷2 (round down) for buff/debuff strength
            return baseValue + Mathf.FloorToInt(tactics / 2f);
        }
        
        public static int ApplyTacticsHealScaling(int baseValue, int tactics)
        {
            // Heal/Shield: +Tactics flat
            return baseValue + tactics;
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