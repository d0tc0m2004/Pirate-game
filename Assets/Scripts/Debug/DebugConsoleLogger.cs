using UnityEngine;
using System.Text;
using TacticalGame.Units;
using TacticalGame.Combat;
using TacticalGame.Managers;
using TacticalGame.Equipment;
using TacticalGame.Grid;
using TacticalGame.Hazards;
using TacticalGame.Core;

namespace TacticalGame.DebugTools
{
    public class DebugConsoleLogger : MonoBehaviour
    {
        [Header("Controls")]
        [Tooltip("Hover over a unit and press this to log their exact state and tile data")]
        public KeyCode inspectUnitKey = KeyCode.F1;
        [Tooltip("Press this anywhere to log Global Resources (Energy, Grog, Deck)")]
        public KeyCode inspectGlobalKey = KeyCode.F2;
        
        [Tooltip("Make sure this matches the physics layer your units are on")]
        public LayerMask unitLayerMask = ~0;

        private UnityEngine.Camera mainCam;

        private void Start()
        {
            mainCam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (Input.GetKeyDown(inspectUnitKey))
            {
                TryLogTargetState(); // UPGRADED: Now targets both Units AND Tiles
            }

            if (Input.GetKeyDown(inspectGlobalKey))
            {
                LogGlobalState();
            }
        }

        private void TryLogTargetState()
        {
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, unitLayerMask))
            {
                // 1. Did we hit a Unit?
                UnitStatus unit = hit.collider.GetComponent<UnitStatus>();
                if (unit == null) unit = hit.collider.GetComponentInParent<UnitStatus>();

                if (unit != null)
                {
                    GenerateUnitReport(unit);
                    return;
                }

                // 2. If no unit, did we hit a Hazard or a Grid Cell directly?
                GridCell cell = hit.collider.GetComponent<GridCell>();
                if (cell == null) cell = hit.collider.GetComponentInParent<GridCell>();

                if (cell != null)
                {
                    GenerateCellReport(cell);
                    return;
                }

                Debug.LogWarning("Debug: No Unit or GridCell found under mouse cursor.");
            }
        }

        private void GenerateCellReport(GridCell cell)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color=#a29bfe><b>=== TILE DIAGNOSTIC: X:{cell.XPosition}, Y:{cell.YPosition} ===</b></color>");
            sb.AppendLine($"- Is Middle Column: {cell.IsMiddleColumn}");
            sb.AppendLine($"- Is Occupied by Unit: {cell.IsOccupied}");
            
            if (cell.HasHazard)
            {
                sb.AppendLine($"\n<color=red><b>[HAZARD DATA]</b></color>");
                sb.AppendLine($"Static Hazard: {cell.CurrentHazardName}");

                var hazardManager = ServiceLocator.Get<HazardManager>();
                if (hazardManager != null)
                {
                    // Check for Runtime Hazards (like the Cannon)
                    var runtimeHaz = hazardManager.GetRuntimeHazard(cell);
                    if (runtimeHaz != null)
                    {
                        sb.AppendLine($"- Runtime Type: {runtimeHaz.Type}");
                        sb.AppendLine($"- Duration: {(runtimeHaz.Duration < 0 ? "Permanent" : $"{runtimeHaz.Duration} turns left")}");
                        
                        // Expose specific hazard math!
                        if (runtimeHaz.Type == RuntimeHazardType.CannonObstacle)
                        {
                            sb.AppendLine($"- <color=orange>Cannon HP: {runtimeHaz.Value}</color>");
                            sb.AppendLine($"- <color=orange>Cannon Damage: {runtimeHaz.ExtraValue}</color>");
                        }
                        else if (runtimeHaz.Type == RuntimeHazardType.ExplodingBarrel)
                        {
                            sb.AppendLine($"- <color=orange>Barrel Explosion Damage: {runtimeHaz.Value}</color>");
                        }
                        else if (runtimeHaz.Type == RuntimeHazardType.Poison || runtimeHaz.Type == RuntimeHazardType.Fire)
                        {
                            sb.AppendLine($"- <color=orange>Damage Per Turn: {runtimeHaz.Value}</color>");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("\n<color=green>- Tile is clear (No Hazards)</color>");
            }

            Debug.Log(sb.ToString());
        }

        private void GenerateUnitReport(UnitStatus unit)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"<color=cyan><b>=== DIAGNOSTIC REPORT: {unit.UnitName} ({unit.Team}) ===</b></color>");

            // 1. UNIT BASE & HIDDEN STATS
            sb.AppendLine("\n<color=orange><b>[1. UNIT STATS]</b></color>");
            sb.AppendLine($"- HP: {unit.CurrentHP} / {unit.MaxHP}");
            sb.AppendLine($"- Morale: {Mathf.RoundToInt(unit.MaxMorale * unit.MoralePercent)} / {unit.MaxMorale}");
            sb.AppendLine($"- Hull (Shield): {unit.CurrentHullPool} / {unit.MaxHullPool}");
            sb.AppendLine($"- Buzz: {unit.CurrentBuzz}");
            sb.AppendLine($"- Grit: {unit.Grit}");
            sb.AppendLine($"- Speed: {unit.Speed}");

            // 2. STATUS EFFECTS
            sb.AppendLine("\n<color=yellow><b>[2. STATUS EFFECTS]</b></color>");
            var effectsManager = unit.GetComponent<StatusEffectManager>();
            if (effectsManager != null)
            {
                var buffs = effectsManager.GetActiveBuffs();
                if (buffs.Count > 0)
                {
                    sb.AppendLine("  <color=#00FF00>Buffs:</color>");
                    foreach (var b in buffs) 
                    {
                        string details = GetEffectDetails(b);
                        string src = b.source != null ? $" (from: {b.source.name})" : "";
                        string dur = b.remainingTurns < 0 ? "permanent" : $"{b.remainingTurns}t left";
                        sb.AppendLine($"   + {b.effectName} <color=white>{details}</color> <color=#888888>[{dur}]{src}</color>");
                    }
                }

                var debuffs = effectsManager.GetActiveDebuffs();
                if (debuffs.Count > 0)
                {
                    sb.AppendLine("  <color=#FF4444>Debuffs:</color>");
                    foreach (var d in debuffs) 
                    {
                        string details = GetEffectDetails(d);
                        string src = d.source != null ? $" (from: {d.source.name})" : "";
                        string dur = d.remainingTurns < 0 ? "permanent" : $"{d.remainingTurns}t left";
                        sb.AppendLine($"   - {d.effectName} <color=white>{details}</color> <color=#888888>[{dur}]{src}</color>");
                    }
                }

                if (buffs.Count == 0 && debuffs.Count == 0) sb.AppendLine("  None Active");
            }

            // 3. EQUIPMENT & PASSIVES
            sb.AppendLine("\n<color=#74b9ff><b>[3. EQUIPMENT & PASSIVES]</b></color>");
            var flexEquip = unit.GetComponent<FlexibleUnitEquipment>();
            if (flexEquip != null)
            {
                var slots = flexEquip.GetEquippedSlots();
                if (slots != null)
                {
                    foreach (var slot in slots)
                    {
                        if (slot.hasWeapon && slot.weaponRelic != null)
                        {
                            sb.AppendLine($"  [Weapon] {slot.weaponRelic.relicName}");
                        }
                        else if (slot.categoryRelic != null)
                        {
                            string cat = slot.categoryRelic.category.ToString();
                            string desc = slot.categoryRelic.effectData?.description ?? slot.categoryRelic.fullDescription;
                            string passive = slot.categoryRelic.IsPassive() ? " (Passive)" : "";
                            sb.AppendLine($"  [{cat}] {desc}{passive}");
                        }
                    }
                }
            }
            var passiveMgr = unit.GetComponent<PassiveRelicManager>();
            if (passiveMgr != null && passiveMgr.ActivePassives.Count > 0)
            {
                sb.AppendLine("  <color=#ffeaa7>Active Passives:</color>");
                foreach (var p in passiveMgr.ActivePassives)
                    sb.AppendLine($"   * {p}");
                if (passiveMgr.HullsDestroyedThisGame > 0)
                    sb.AppendLine($"   * Hulls Destroyed This Game: {passiveMgr.HullsDestroyedThisGame}");
            }

            // 4. RECENT DAMAGE HISTORY
            sb.AppendLine("\n<color=#fdcb6e><b>[4. RECENT DAMAGE HISTORY]</b></color>");
            if (unit.RecentDamageLog != null && unit.RecentDamageLog.Count > 0)
            {
                foreach (var log in unit.RecentDamageLog)
                    sb.AppendLine($"  - {log}");
            }
            else
            {
                sb.AppendLine("  - No recent damage taken.");
            }

            // 5. GRID & HAZARD LAYER
            sb.AppendLine("\n<color=#a29bfe><b>[5. GRID & TILE DATA]</b></color>");
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
                GridCell cell = gridManager.GetCell(pos.x, pos.y);
                
                if (cell != null)
                {
                    sb.AppendLine($"- Position: X:{cell.XPosition}, Y:{cell.YPosition}");
                    sb.AppendLine($"- Is Middle Column: {cell.IsMiddleColumn}");
                    
                    if (cell.HasHazard)
                    {
                        sb.AppendLine($"  <color=red>! TILE HAZARD DETECTED !</color>");
                        sb.AppendLine($"  - Static Hazard Name: {cell.CurrentHazardName}");
                        
                        var hazardManager = ServiceLocator.Get<HazardManager>();
                        if (hazardManager != null)
                        {
                            var runtimeHaz = hazardManager.GetRuntimeHazard(cell);
                            if (runtimeHaz != null)
                            {
                                sb.AppendLine($"  - Runtime Hazard Type: {runtimeHaz.Type}");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("- Tile is safe (No Hazards)");
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

        private void LogGlobalState()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color=#55efc4><b>=== GLOBAL MATCH STATE ===</b></color>");

            // RESOURCES
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (energyManager != null)
            {
                sb.AppendLine("\n<color=orange><b>[RESOURCES]</b></color>");
                sb.AppendLine($"- Current Energy: {energyManager.CurrentEnergy} / {energyManager.MaxEnergy}");
                sb.AppendLine($"- Grog Tokens: {energyManager.GrogTokens}");
            }

            // DECK STATE & HAND INSPECTION
            if (BattleDeckManager.Instance != null)
            {
                sb.AppendLine("\n<color=cyan><b>[DECK & HAND]</b></color>");
                sb.AppendLine($"- Cards in Deck: {BattleDeckManager.Instance.DeckCount}");
                sb.AppendLine($"- Cards in Discard: {BattleDeckManager.Instance.DiscardCount}");
                
                int stowed = BattleDeckManager.Instance.GetStowedCount();
                if (stowed > 0) sb.AppendLine($"- Stowed Cards: {stowed}");

                sb.AppendLine($"\n  <color=yellow>--- Current Hand ({BattleDeckManager.Instance.HandCount} cards) ---</color>");
                var currentHand = BattleDeckManager.Instance.Hand;
                
                if (currentHand.Count == 0)
                {
                    sb.AppendLine("  (Hand is empty)");
                }
                else
                {
                    foreach (var card in currentHand)
                    {
                        string stowedStr = card.isStowed ? " <color=blue>[STOWED]</color>" : "";
                        sb.AppendLine($"  [{card.energyCost} Energy] {card.GetDisplayName()} (Owner: {card.GetOwnerName()}){stowedStr}");
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Translates the raw float values of a Status Effect into human-readable text.
        /// </summary>
        private string GetEffectDetails(StatusEffect effect)
        {
            switch (effect.type)
            {
                // === DAMAGE OVER TIME ===
                case StatusEffectType.Fire:
                    return $"[Takes {effect.value1} fire dmg per turn]";
                case StatusEffectType.Poison:
                    return $"[Takes {effect.value1} poison dmg per turn]";
                case StatusEffectType.Bleed:
                    return $"[Takes {effect.value1} bleed dmg when moving]";
                case StatusEffectType.MovementTrap:
                    return $"[Takes {effect.value1 * 100}% HP dmg if unit moves]";

                // === STAT BOOSTS ===
                case StatusEffectType.GritBoost:
                    return $"[Grit +{effect.value1} → more damage reduction]";
                case StatusEffectType.AimBoost:
                    return $"[Aim +{effect.value1} → more hit chance]";
                case StatusEffectType.PowerBoost:
                    return $"[Power +{effect.value1} → more weapon dmg]";
                case StatusEffectType.SpeedBoost:
                    return $"[Speed +{effect.value1} → faster turn order]";
                case StatusEffectType.ProficiencyBoost:
                    return $"[Proficiency +{effect.value1 * 100}% → more effective relics]";

                // === STAT REDUCTIONS ===
                case StatusEffectType.GritReduction:
                    return $"[Grit -{effect.value1} → less damage reduction]";
                case StatusEffectType.AimReduction:
                    return $"[Aim -{effect.value1} → less hit chance]";
                case StatusEffectType.PowerReduction:
                    return $"[Power -{effect.value1} → less weapon dmg]";
                case StatusEffectType.SpeedReduction:
                    return $"[Speed -{effect.value1} → slower turn order]";
                case StatusEffectType.HealthStatBoost:
                    return $"[Max HP +{effect.value1 * 100}%]";
                case StatusEffectType.HealthStatReduction:
                    return $"[Max HP -{effect.value1 * 100}%]";

                // === DAMAGE MODIFIERS ===
                case StatusEffectType.DamageBoost:
                    return $"[Weapon dmg +{effect.value1 * 100}%]";
                case StatusEffectType.DamageReduction:
                    return $"[Incoming dmg -{effect.value1 * 100}%]";
                case StatusEffectType.Vulnerable:
                    return $"[Incoming dmg +{effect.value1 * 100}% (takes more)]";
                case StatusEffectType.Weakness:
                    return $"[Weapon dmg -{effect.value1 * 100}% (deals less)]";
                case StatusEffectType.RangedDamageReduction:
                    return $"[Ranged dmg taken -{effect.value1 * 100}%]";
                case StatusEffectType.MoraleDamageReduction:
                    return $"[Morale dmg taken -{effect.value1 * 100}%]";
                case StatusEffectType.MaxHPBoost:
                    return $"[Max HP +{effect.value1 * 100}%]";

                // === DODGE & MISS ===
                case StatusEffectType.Dodge:
                    return $"[{effect.value1 * 100}% chance to dodge attacks]";
                case StatusEffectType.MissChance:
                    return $"[{effect.value1 * 100}% chance attacks miss]";

                // === HEALING & SUSTAIN ===
                case StatusEffectType.Regeneration:
                    return $"[Heals {effect.value1} HP per turn]";
                case StatusEffectType.HealOnCardPlay:
                    return $"[Heals {effect.value1 * 100}% max HP per card played]";
                case StatusEffectType.RumHealBoost:
                    return $"[Rum heals +{effect.value1 * 100}% more]";
                case StatusEffectType.FoodEffectBoost:
                    return $"[Food effects +{effect.value1 * 100}% stronger]";

                // === SHIELDS & REFLECT ===
                case StatusEffectType.Shielded:
                    return $"[Absorbs {effect.value1} dmg before HP]";
                case StatusEffectType.MoraleShield:
                    return $"[Absorbs {effect.value1} morale dmg]";
                case StatusEffectType.Thorns:
                    return $"[Reflects {effect.value1} dmg to attackers]";
                case StatusEffectType.ReturnDamage:
                    return $"[Reflects dmg back, {effect.value1} charges left]";

                // === RESOURCE MODIFIERS ===
                case StatusEffectType.ReduceAllCosts:
                    return $"[All cards cost {effect.value1} less energy]";
                case StatusEffectType.ReduceNextRangedCost:
                    return $"[Next ranged card -{effect.value1} energy]";
                case StatusEffectType.IncreaseCost:
                    return $"[Cards cost +{effect.value1} more energy]";
                case StatusEffectType.EnemyWeaponCostIncrease:
                    return $"[Enemy weapons cost +{effect.value1} more]";
                case StatusEffectType.EnergyDrain:
                    return $"[Loses {effect.value1} energy per turn]";
                case StatusEffectType.ReduceCardDraw:
                    return $"[Draws {effect.value1} fewer cards]";

                // === ON-KILL BONUSES ===
                case StatusEffectType.MoraleOnKill:
                    return $"[+{effect.value1 * 100}% morale on kill]";
                case StatusEffectType.GrogOnKill:
                    return $"[+{effect.value1} grog on kill]";

                // === CHARGES & TRIGGERS ===
                case StatusEffectType.DrawOnEnemyAttack:
                    return $"[Draw card when attacked, {effect.value1} charges | attacker discards {effect.value2}]";
                case StatusEffectType.FreeStows:
                    return $"[{effect.value1} free stows remaining]";
                case StatusEffectType.FreeRumUsage:
                    return $"[{effect.value1} free rum uses remaining]";
                case StatusEffectType.RangedBlock:
                    return $"[Blocks {effect.value1} ranged attacks]";
                case StatusEffectType.WeaponUseTwice:
                    return $"[Next {effect.value1} weapon(s) can be used twice]";
                case StatusEffectType.BuzzGainReduction:
                    return $"[Buzz gain -{effect.value1 * 100}%]";

                // === CROWD CONTROL ===
                case StatusEffectType.Stun:
                    return "[Cannot act this turn]";
                case StatusEffectType.Trapped:
                    return "[Cannot move, takes +10% dmg]";
                case StatusEffectType.Slowed:
                    return $"[Movement reduced by {effect.value1}]";
                case StatusEffectType.Taunt:
                    return "[Enemies must target this unit]";
                case StatusEffectType.Marked:
                    return $"[Takes +{effect.value1 * 100}% dmg, morale target]";
                case StatusEffectType.Cursed:
                    return "[Takes bonus dmg from all sources]";
                case StatusEffectType.HealBlock:
                    return "[Cannot restore HP or morale]";
                case StatusEffectType.Stasis:
                    return "[Cannot act or be damaged]";
                case StatusEffectType.Invincible:
                    return "[Cannot take any damage]";
                case StatusEffectType.FreeMove:
                    return "[Next move costs 0 energy]";
                case StatusEffectType.PreventDisplacement:
                    return "[Cannot be knocked back]";
                case StatusEffectType.PreventSurrender:
                    return "[Cannot surrender]";
                case StatusEffectType.DeathPrevention:
                    return "[Survives lethal hit once]";
                case StatusEffectType.BuzzImmunity:
                    return "[Immune to buzz effects]";
                case StatusEffectType.WeaponDisabled:
                    return "[Cannot use weapon/gloves relics]";
                case StatusEffectType.DisablePassives:
                    return "[Passive effects disabled]";
                case StatusEffectType.DisableNonWeaponRelics:
                    return "[Can only use weapon relics]";
                case StatusEffectType.StunOnMoveTracker:
                    return "[Stunned for 1 turn if this unit moves]";

                // Fallback
                default:
                    if (effect.value1 != 0 || effect.value2 != 0)
                    {
                        string fallback = $"[v1={effect.value1}";
                        if (effect.value2 != 0) fallback += $", v2={effect.value2}";
                        return fallback + "]";
                    }
                    return "";
            }
        }
    }
}