using System.Linq;
using UnityEngine;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Combat;

/// <summary>
/// Handles execution of weapon relic effects during combat.
/// Uses reflection to work with UnitStatus fields.
/// </summary>
public static class WeaponRelicEffectHandler
{
    /// <summary>
    /// Calculate bonus damage multiplier from weapon relic effects.
    /// Returns multiplier (e.g., 1.2 for +20%).
    /// </summary>
    public static float CalculateBonusDamageMultiplier(
        MonoBehaviour attacker,
        MonoBehaviour target,
        WeaponRelic weaponRelic,
        bool isFirstAttackThisTurn,
        bool attackerMovedLastTurn,
        bool targetMovedLastTurn)
    {
        if (weaponRelic == null) return 1f;

        float bonus = CalculateBonusDamagePercent(
            attacker, target, weaponRelic, 
            isFirstAttackThisTurn, attackerMovedLastTurn, targetMovedLastTurn
        );
        
        return 1f + bonus;
    }

    /// <summary>
    /// Apply the on-hit effect from a weapon relic.
    /// </summary>
    public static void ApplyOnHitEffect(
        MonoBehaviour attacker,
        MonoBehaviour target,
        WeaponRelic weaponRelic,
        int damageDealt,
        bool targetDied)
    {
        if (weaponRelic == null) return;

        WeaponRelicEffectData effect = weaponRelic.effectData;
        WeaponRelicEffectType effectType = effect.effectType;

        Debug.Log($"<color=cyan>Weapon Relic Effect: {effect.effectName} ({weaponRelic.roleTag})</color>");

        // Get component values using reflection
        int targetCurrentMorale = GetIntProperty(target, "CurrentMorale");
        int targetMaxMorale = GetIntProperty(target, "MaxMorale");
        int targetCurrentHP = GetIntProperty(target, "CurrentHP");
        int targetMaxHP = GetIntProperty(target, "MaxHP");
        int targetCurrentBuzz = GetIntProperty(target, "CurrentBuzz");
        int targetMaxBuzz = GetIntProperty(target, "MaxBuzz");
        bool targetSurrendered = GetBoolProperty(target, "HasSurrendered");

        int attackerCurrentMorale = GetIntProperty(attacker, "CurrentMorale");
        int attackerMaxMorale = GetIntProperty(attacker, "MaxMorale");
        int attackerCurrentHP = GetIntProperty(attacker, "CurrentHP");
        int attackerMaxHP = GetIntProperty(attacker, "MaxHP");
        int attackerGrit = GetIntProperty(attacker, "Grit");
        int attackerHull = GetIntProperty(attacker, "Hull");
        int attackerPower = GetIntProperty(attacker, "Power");
        int attackerAim = GetIntProperty(attacker, "Aim");

        switch (effectType)
        {
            case WeaponRelicEffectType.SurgeonHealAlly:
                if (attacker != null)
                {
                    int healAmount = Mathf.RoundToInt(effect.value1);
                    GameObject closestAlly = FindClosestAlly(attacker.gameObject);
                    if (closestAlly != null)
                    {
                        var allyStatus = closestAlly.GetComponent<MonoBehaviour>();
                        InvokeMethod(allyStatus, "Heal", healAmount);
                        Debug.Log($"<color=green>Healed {closestAlly.name} for {healAmount}!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.CookReduceTactics:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int tacticsRed = Mathf.RoundToInt(effect.value1);
                        StatusEffect debuff = StatusEffect.CreateTacticsDebuff(1, tacticsRed); // Next turn
                        statusEffectMgr.ApplyEffect(debuff);
                        Debug.Log($"<color=orange>Target Tactics reduced by {tacticsRed} next turn!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.NavigatorAddMove:
                if (attacker != null)
                {
                    var statusEffectMgr = attacker.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int moveAdd = Mathf.RoundToInt(effect.value1);
                        StatusEffect buff = StatusEffect.CreateMoveBuff(1, moveAdd); // Next turn
                        statusEffectMgr.ApplyEffect(buff);
                        Debug.Log($"<color=cyan>+{moveAdd} Move next turn!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.CaptainAddMorale:
                if (attacker != null)
                {
                    int moraleAdd = Mathf.RoundToInt(effect.value1);
                    GameObject[] allUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray();
                    foreach (GameObject unit in allUnits)
                    {
                        if (IsSameTeam(attacker.gameObject, unit))
                        {
                            InvokeMethod(unit.GetComponent<MonoBehaviour>(), "RestoreMorale", moraleAdd);
                        }
                    }
                    Debug.Log($"<color=green>Restored {moraleAdd} morale to all allies!</color>");
                }
                break;
                
            case WeaponRelicEffectType.QuartermasterStealMorale:
                if (target != null && attacker != null && !targetDied)
                {
                    int stealAmount = Mathf.RoundToInt(effect.value1);
                    InvokeMethod(target, "ApplyMoraleDamage", stealAmount);
                    InvokeMethod(attacker, "RestoreMorale", stealAmount);
                    Debug.Log($"<color=purple>Stole {stealAmount} morale!</color>");
                }
                break;
                
            case WeaponRelicEffectType.SwashbucklerAddSpeed:
                if (attacker != null)
                {
                    var statusEffectMgr = attacker.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int speedAdd = Mathf.RoundToInt(effect.value1);
                        StatusEffect buff = StatusEffect.CreateSpeedBoost(1, speedAdd); // Next turn
                        statusEffectMgr.ApplyEffect(buff);
                        Debug.Log($"<color=cyan>+{speedAdd} Speed next turn!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.BoatswainAddThreat:
                if (attacker != null)
                {
                    int threatAdd = Mathf.RoundToInt(effect.value1);
                    // Add threat logic here (pseudo-implementation)
                    Debug.Log($"<color=red>+{threatAdd} Grid Threat generation!</color>");
                }
                break;
                
            case WeaponRelicEffectType.MasterAtArmsAddCombo:
                if (attacker != null)
                {
                    var statusEffectMgr = attacker.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int comboAdd = Mathf.RoundToInt(effect.value1);
                        StatusEffect buff = StatusEffect.CreateComboMultiplierBuff(1, comboAdd); // Next turn
                        statusEffectMgr.ApplyEffect(buff);
                        Debug.Log($"<color=gold>+{comboAdd} Combo Multiplier next turn!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.MasterGunnerReduceAim:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int aimRed = Mathf.RoundToInt(effect.value1);
                        StatusEffect debuff = StatusEffect.CreateAimReduction(1, aimRed); // Next turn
                        statusEffectMgr.ApplyEffect(debuff);
                        Debug.Log($"<color=orange>Target Aim reduced by {aimRed} next turn!</color>");
                    }
                }
                break;
                
            case WeaponRelicEffectType.HelmsmasterAddBuzz:
                if (attacker != null)
                {
                    int buzzAdd = Mathf.RoundToInt(effect.value1);
                    InvokeMethod(attacker, "AddBuzz", buzzAdd);
                    Debug.Log($"<color=yellow>+{buzzAdd} Buzz to self!</color>");
                }
                break;
                
            case WeaponRelicEffectType.ShipwrightRestoreHull:
                if (attacker != null)
                {
                    int hullAdd = Mathf.RoundToInt(effect.value1);
                    InvokeMethod(attacker, "RestoreHull", hullAdd);
                    Debug.Log($"<color=cyan>Restored {hullAdd} Hull!</color>");
                }
                break;
                
            case WeaponRelicEffectType.DeckhandReduceMove:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int moveRed = Mathf.RoundToInt(effect.value1);
                        StatusEffect debuff = StatusEffect.CreateSlow(1, moveRed); // Next turn
                        statusEffectMgr.ApplyEffect(debuff);
                        Debug.Log($"<color=orange>Target Move reduced by {moveRed} next turn!</color>");
                    }
                }
                break;
                
            default:
                break;
        }
    }

    /// <summary>
    /// Calculate bonus damage percent from weapon relic effects.
    /// </summary>
    private static float CalculateBonusDamagePercent(
        MonoBehaviour attacker,
        MonoBehaviour target,
        WeaponRelic weaponRelic,
        bool isFirstAttackThisTurn,
        bool attackerMovedLastTurn,
        bool targetMovedLastTurn)
    {
        if (weaponRelic == null) return 0f;

        float bonus = 0f;
        WeaponRelicEffectData effect = weaponRelic.effectData;

        // Always add rarity bonus
        bonus += effect.bonusDamagePercent;

        // Get values using properties
        int attackerMorale = GetIntProperty(attacker, "CurrentMorale");
        int attackerHP = GetIntProperty(attacker, "CurrentHP");
        int attackerMaxHP = GetIntProperty(attacker, "MaxHP");
        int attackerSpeed = GetIntProperty(attacker, "Speed");

        int targetMorale = GetIntProperty(target, "CurrentMorale");
        int targetBuzz = GetIntProperty(target, "CurrentBuzz");
        int targetMaxBuzz = GetIntProperty(target, "MaxBuzz");
        int targetSpeed = GetIntProperty(target, "Speed");
        int targetHull = GetIntProperty(target, "CurrentHullPool");

        // In V5, role weapon tags only apply On-Hit effects, not passive flat damage scaling
        // So we remove the old switch statement and just return the rarity bonus (if any)
        
        return bonus;
    }

    #region Reflection Helpers

    private static int GetIntProperty(object obj, string propertyName)
    {
        if (obj == null) return 0;
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop != null) return (int)prop.GetValue(obj);
        
        // Try field
        var field = obj.GetType().GetField(propertyName);
        if (field != null) return (int)field.GetValue(obj);
        
        return 0;
    }

    private static bool GetBoolProperty(object obj, string propertyName)
    {
        if (obj == null) return false;
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop != null) return (bool)prop.GetValue(obj);
        
        var field = obj.GetType().GetField(propertyName);
        if (field != null) return (bool)field.GetValue(obj);
        
        return false;
    }

    private static void InvokeMethod(object obj, string methodName, params object[] args)
    {
        if (obj == null) return;
        var method = obj.GetType().GetMethod(methodName);
        if (method != null) 
        {
            try
            {
                method.Invoke(obj, args);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to invoke {methodName}: {e.Message}");
            }
        }
    }

    #endregion

    #region Helper Methods

    private static bool IsSameTeam(GameObject a, GameObject b)
    {
        if (a == null || b == null) return false;
        
        var statusA = a.GetComponent<MonoBehaviour>();
        var statusB = b.GetComponent<MonoBehaviour>();
        
        if (statusA == null || statusB == null) return false;
        
        var teamA = GetIntProperty(statusA, "Team");
        var teamB = GetIntProperty(statusB, "Team");
        
        return teamA == teamB;
    }

    private static GameObject FindClosestAlly(GameObject unit)
    {
        GameObject[] allUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray();
        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (GameObject other in allUnits)
        {
            if (other == unit) continue;
            
            bool otherSurrendered = GetBoolProperty(other.GetComponent<MonoBehaviour>(), "HasSurrendered");
            if (!otherSurrendered && IsSameTeam(unit, other))
            {
                float dist = Vector3.Distance(unit.transform.position, other.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = other;
                }
            }
        }
        return closest;
    }

    private static int CountAlliesInRadius(GameObject unit, float radius)
    {
        int count = 0;
        GameObject[] allUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray();

        foreach (GameObject other in allUnits)
        {
            if (other == unit) continue;
            
            bool otherSurrendered = GetBoolProperty(other.GetComponent<MonoBehaviour>(), "HasSurrendered");
            if (!otherSurrendered && IsSameTeam(unit, other))
            {
                float dist = Vector3.Distance(unit.transform.position, other.transform.position);
                if (dist <= radius) count++;
            }
        }
        return count;
    }

    #endregion
}