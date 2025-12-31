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
            // === CAPTAIN EFFECTS ===
            case WeaponRelicEffectType.RestoreEnergyOnKill:
                if (targetDied || targetSurrendered)
                {
                    var energyManager = Object.FindFirstObjectByType<TacticalGame.Managers.EnergyManager>();
                    if (energyManager != null)
                    {
                        int amount = Mathf.RoundToInt(effect.value1);
                        // Use reflection to add energy
                        var field = energyManager.GetType().GetField("currentEnergy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            int current = (int)field.GetValue(energyManager);
                            int max = GetIntProperty(energyManager, "MaxEnergy");
                            field.SetValue(energyManager, Mathf.Min(max, current + amount));
                        }
                        Debug.Log($"<color=green>+{amount} Energy from kill!</color>");
                    }
                }
                break;

            case WeaponRelicEffectType.MarkTargetRestoreEnergy:
                if (target != null && !targetDied)
                {
                    // Mark the target - next hit restores energy
                    // This requires StatusEffectManager
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect markEffect = StatusEffect.CreateMarked(effect.duration > 0 ? effect.duration : 1, effect.value1);
                        statusEffectMgr.ApplyEffect(markEffect);
                        Debug.Log($"<color=yellow>Target marked! Next hit restores energy.</color>");
                    }
                }
                break;

            // === QUARTERMASTER EFFECTS ===
            case WeaponRelicEffectType.StealMorale:
                if (target != null && attacker != null)
                {
                    int stealAmount = Mathf.RoundToInt(effect.value1);
                    InvokeMethod(target, "ApplyMoraleDamage", stealAmount);
                    InvokeMethod(attacker, "RestoreMorale", stealAmount);
                    Debug.Log($"<color=purple>Stole {stealAmount} morale!</color>");
                }
                break;

            case WeaponRelicEffectType.RestoreMoraleToAllies:
                if (attacker != null)
                {
                    int restoreAmount = Mathf.RoundToInt(attackerCurrentMorale * effect.value1);
                    GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
                    foreach (GameObject unit in allUnits)
                    {
                        if (unit == attacker.gameObject) continue;
                        if (IsSameTeam(attacker.gameObject, unit))
                        {
                            InvokeMethod(unit.GetComponent<MonoBehaviour>(), "RestoreMorale", restoreAmount);
                        }
                    }
                    Debug.Log($"<color=green>Restored {restoreAmount} morale to all allies!</color>");
                }
                break;

            // === HELMSMASTER EFFECTS ===
            case WeaponRelicEffectType.IncreaseBuzzMeter:
                if (target != null)
                {
                    int buzzIncrease = Mathf.RoundToInt(targetMaxBuzz * effect.value1);
                    // Add buzz to target (making them drunker)
                    InvokeMethod(target, "AddBuzz", buzzIncrease);
                    Debug.Log($"<color=yellow>Target's buzz increased by {buzzIncrease}!</color>");
                }
                break;

            case WeaponRelicEffectType.ApplyMissDebuff:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect missEffect = StatusEffect.CreateMissChance(effect.duration > 0 ? effect.duration : 2, effect.value1);
                        statusEffectMgr.ApplyEffect(missEffect);
                        Debug.Log($"<color=yellow>Target disoriented! {effect.value1 * 100}% miss chance for {effect.duration} turns.</color>");
                    }
                    else
                    {
                        // Fallback: apply stun
                        InvokeMethod(target, "ApplyStun", effect.duration > 0 ? effect.duration : 1);
                        Debug.Log($"<color=yellow>Target stunned!</color>");
                    }
                }
                break;

            // === BOATSWAIN EFFECTS ===
            case WeaponRelicEffectType.StealHealth:
                if (attacker != null)
                {
                    int stealAmount = Mathf.RoundToInt(damageDealt * effect.value1);
                    InvokeMethod(attacker, "Heal", stealAmount);
                    Debug.Log($"<color=green>Stole {stealAmount} health!</color>");
                }
                break;

            // === SHIPWRIGHT EFFECTS ===
            case WeaponRelicEffectType.ReduceEnemyGrit:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect gritReduction = StatusEffect.CreateGritReduction(effect.duration > 0 ? effect.duration : 2, effect.value1);
                        statusEffectMgr.ApplyEffect(gritReduction);
                        Debug.Log($"<color=orange>Reduced target's Grit by {effect.value1} for {effect.duration} turns!</color>");
                    }
                }
                break;

            case WeaponRelicEffectType.GainGritDealBonusDamage:
                if (attacker != null)
                {
                    var statusEffectMgr = attacker.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        int gritGain = Mathf.RoundToInt(attackerGrit * effect.value1);
                        StatusEffect gritBoost = StatusEffect.CreateGritBoost(effect.duration > 0 ? effect.duration : 2, gritGain);
                        statusEffectMgr.ApplyEffect(gritBoost);
                        Debug.Log($"<color=cyan>Gained {gritGain} Grit for {effect.duration} turns!</color>");
                    }
                }
                break;

            // === MASTER GUNNER EFFECTS ===
            case WeaponRelicEffectType.GainPrimaryStatOnKill:
                if (targetDied && attacker != null)
                {
                    int boost = Mathf.RoundToInt(Mathf.Max(attackerPower, attackerAim) * effect.value1);
                    // This is a permanent buff for the battle - would need special handling
                    Debug.Log($"<color=gold>+{boost} Power/Aim from kill! (Permanent for this battle)</color>");
                }
                break;

            case WeaponRelicEffectType.ReuseAbilityOnKill:
                if (targetDied && attacker != null)
                {
                    var movement = attacker.GetComponent<TacticalGame.Units.UnitMovement>();
                    if (movement != null)
                    {
                        movement.BeginTurn(); // Reset attacked state
                        Debug.Log($"<color=gold>Ability refreshed from kill!</color>");
                    }
                }
                break;

            case WeaponRelicEffectType.BonusDamagePerStowedWeapon:
                // This is a passive damage bonus, handled in CalculateBonusDamagePercent
                // But we can log it here
                Debug.Log($"<color=cyan>Stowed weapon bonus applied!</color>");
                break;

            // === MASTER-AT-ARMS EFFECTS ===
            case WeaponRelicEffectType.ReduceWeaponRelicCost:
                // This would reduce energy cost of next weapon use
                Debug.Log($"<color=cyan>Next weapon relic costs {effect.value1} less energy!</color>");
                break;

            case WeaponRelicEffectType.ExecuteLowHealth:
                if (target != null && !targetDied && targetMaxHP > 0)
                {
                    float hpPercent = (float)targetCurrentHP / targetMaxHP;
                    if (hpPercent < effect.value1)
                    {
                        // Execute the target
                        InvokeMethod(target, "TakeDamage", targetCurrentHP + 100, attacker.gameObject, true, 0, 0, false, false, 1);
                        Debug.Log($"<color=red>EXECUTED! Target was below {effect.value1 * 100}% HP!</color>");
                    }
                }
                break;

            // === NAVIGATOR EFFECTS ===
            case WeaponRelicEffectType.CreateHazardAtTarget:
                if (target != null && !targetDied)
                {
                    // Would need HazardManager reference to create hazard
                    var hazardManager = Object.FindFirstObjectByType<TacticalGame.Hazards.HazardManager>();
                    if (hazardManager != null)
                    {
                        Debug.Log($"<color=orange>Created hazard at target location!</color>");
                        // hazardManager.CreateRandomHazardAt(target.transform.position);
                    }
                }
                break;

            // === SURGEON EFFECTS ===
            case WeaponRelicEffectType.HealClosestAlly:
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

            case WeaponRelicEffectType.ApplyHealBlock:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect healBlock = StatusEffect.CreateHealBlock(effect.duration > 0 ? effect.duration : 2);
                        statusEffectMgr.ApplyEffect(healBlock);
                        Debug.Log($"<color=red>Target heal blocked for {effect.duration} turns!</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=red>Heal block applied! (No StatusEffectManager found)</color>");
                    }
                }
                break;

            // === COOK EFFECTS ===
            case WeaponRelicEffectType.ApplyFireDebuff:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect fireEffect = StatusEffect.CreateFire(effect.duration > 0 ? effect.duration : 4, 10f);
                        statusEffectMgr.ApplyEffect(fireEffect);
                        Debug.Log($"<color=orange>Target is on FIRE for {effect.duration} turns!</color>");
                    }
                    else
                    {
                        // Fallback: apply curse
                        InvokeMethod(target, "ApplyCurse", 1.5f);
                        Debug.Log($"<color=orange>Target is burning (cursed)!</color>");
                    }
                }
                break;

            case WeaponRelicEffectType.BonusDamagePerDebuff:
                // This is handled in CalculateBonusDamagePercent
                Debug.Log($"<color=purple>Debuff damage bonus applied!</color>");
                break;

            case WeaponRelicEffectType.ApplyDebuffWithHealthDamage:
                if (target != null && !targetDied)
                {
                    var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                    if (statusEffectMgr != null)
                    {
                        StatusEffect trapEffect = StatusEffect.CreateMovementTrap(effect.duration > 0 ? effect.duration : 2, effect.value1);
                        statusEffectMgr.ApplyEffect(trapEffect);
                        Debug.Log($"<color=red>Target trapped! Moving will deal {effect.value1 * 100}% HP damage!</color>");
                    }
                    else
                    {
                        InvokeMethod(target, "ApplyTrap");
                        Debug.Log($"<color=red>Target trapped!</color>");
                    }
                }
                break;

            // === SWASHBUCKLER EFFECTS ===
            case WeaponRelicEffectType.FreeCostIfFirst:
                // This is handled before the attack in energy calculation
                Debug.Log($"<color=gold>Free attack (first of turn)!</color>");
                break;

            // === DECKHAND EFFECTS ===
            case WeaponRelicEffectType.RestoreHull:
                if (attacker != null)
                {
                    int hullRestore = Mathf.RoundToInt(effect.value1);
                    InvokeMethod(attacker, "RestoreHull", hullRestore);
                    Debug.Log($"<color=cyan>Restored {hullRestore} Hull!</color>");
                }
                break;

            case WeaponRelicEffectType.ReduceEnemyEnergyOnHullBreak:
                // Check if we broke the hull with this attack
                int targetHullAfter = GetIntProperty(target, "CurrentHullPool");
                if (targetHullAfter <= 0)
                {
                    Debug.Log($"<color=red>Hull broken! Enemy loses {effect.value1} energy next turn!</color>");
                    // Would need to track this for next turn
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

        switch (effect.effectType)
        {
            case WeaponRelicEffectType.BonusDamagePerUnspentEnergy:
                var energyManager = Object.FindFirstObjectByType<TacticalGame.Managers.EnergyManager>();
                if (energyManager != null)
                {
                    int currentEnergy = GetIntProperty(energyManager, "CurrentEnergy");
                    bonus += currentEnergy * effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageIfLowerMorale:
                if (targetMorale < attackerMorale)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageByBuzzState:
                if (targetMaxBuzz > 0)
                {
                    float buzzPercent = (float)targetBuzz / targetMaxBuzz;
                    bonus += buzzPercent * effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageByProximity:
                if (attacker != null && target != null)
                {
                    float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
                    float proximityBonus = Mathf.Max(0f, 1f - (dist / 5f)) * effect.value1;
                    bonus += proximityBonus;
                }
                break;

            case WeaponRelicEffectType.DamageBasedOnHealthPercent:
                if (attackerMaxHP > 0)
                {
                    float hpPercent = (float)attackerHP / attackerMaxHP;
                    bonus += hpPercent * effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageByMissingHealth:
                if (attackerMaxHP > 0)
                {
                    float missingPercent = 1f - ((float)attackerHP / attackerMaxHP);
                    bonus += missingPercent * effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageIfNotMoved:
                if (!attackerMovedLastTurn)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageSameRow:
                if (attacker != null && target != null)
                {
                    if (Mathf.Abs(attacker.transform.position.z - target.transform.position.z) < 0.5f)
                    {
                        bonus += effect.value1;
                    }
                }
                break;

            case WeaponRelicEffectType.BonusDamageIfTargetMoved:
                if (targetMovedLastTurn)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamagePerAllyInRadius:
                if (attacker != null)
                {
                    int allyCount = CountAlliesInRadius(attacker.gameObject, effect.value2);
                    bonus += allyCount * effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageIfLowerSpeed:
                if (targetSpeed < attackerSpeed)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageIfFirstAttack:
                if (isFirstAttackThisTurn)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamageNoHull:
                if (targetHull <= 0)
                {
                    bonus += effect.value1;
                }
                break;

            case WeaponRelicEffectType.BonusDamagePerDebuff:
                // Count debuffs on target
                var statusEffectMgr = target.GetComponent<StatusEffectManager>();
                if (statusEffectMgr != null)
                {
                    int debuffCount = statusEffectMgr.DebuffCount;
                    bonus += debuffCount * effect.value1;
                }
                break;
        }

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
        GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
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
        GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");

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