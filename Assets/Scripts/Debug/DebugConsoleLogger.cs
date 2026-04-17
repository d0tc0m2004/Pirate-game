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
                TryLogUnitState();
            }

            if (Input.GetKeyDown(inspectGlobalKey))
            {
                LogGlobalState();
            }
        }

        private void TryLogUnitState()
        {
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, unitLayerMask))
            {
                UnitStatus unit = hit.collider.GetComponent<UnitStatus>();
                if (unit == null) unit = hit.collider.GetComponentInParent<UnitStatus>();

                if (unit != null)
                {
                    GenerateUnitReport(unit);
                }
            }
            else
            {
                Debug.LogWarning("Debug: No unit found under mouse cursor.");
            }
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

            // 2. STATUS EFFECTS (Now with human-readable translations!)
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
                        sb.AppendLine($"   + {b.effectName} <color=white>{details}</color> ({b.remainingTurns} turns left)");
                    }
                }

                var debuffs = effectsManager.GetActiveDebuffs();
                if (debuffs.Count > 0)
                {
                    sb.AppendLine("  <color=#FF4444>Debuffs:</color>");
                    foreach (var d in debuffs) 
                    {
                        string details = GetEffectDetails(d);
                        sb.AppendLine($"   - {d.effectName} <color=white>{details}</color> ({d.remainingTurns} turns left)");
                    }
                }

                if (buffs.Count == 0 && debuffs.Count == 0) sb.AppendLine("  None Active");
            }

            // 3. GRID & HAZARD LAYER
            sb.AppendLine("\n<color=#a29bfe><b>[3. GRID & TILE DATA]</b></color>");
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
                        
                        if (cell.HazardVisualObject != null)
                        {
                            var hazardInst = cell.HazardVisualObject.GetComponent<HazardInstance>();
                            if (hazardInst != null && (hazardInst.IsSoftObstacle || hazardInst.IsHardObstacle))
                            {
                                sb.AppendLine($"  - Obstacle HP: {hazardInst.ObstacleHP}");
                            }
                        }

                        foreach (Transform child in cell.transform)
                        {
                            if (child.name.StartsWith("RuntimeHazard_"))
                            {
                                sb.AppendLine($"  - Runtime Hazard Type: {child.name.Replace("RuntimeHazard_", "")}");
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
                // DoTs
                case StatusEffectType.Fire:
                case StatusEffectType.Poison:
                    return $"[{effect.value1} HP Dmg/Turn]";
                case StatusEffectType.Bleed:
                    return $"[{effect.value1} HP Dmg/Move]";
                case StatusEffectType.MovementTrap:
                    return $"[{effect.value1 * 100}% HP Dmg if moved]";

                // Flat Stat Boosts/Reductions
                case StatusEffectType.GritBoost:
                case StatusEffectType.AimBoost:
                case StatusEffectType.PowerBoost:
                case StatusEffectType.SpeedBoost:
                    return $"[+{effect.value1}]";
                case StatusEffectType.GritReduction:
                case StatusEffectType.AimReduction:
                case StatusEffectType.PowerReduction:
                case StatusEffectType.SpeedReduction:
                    return $"[-{effect.value1}]";

                // Percentages
                case StatusEffectType.DamageBoost:
                case StatusEffectType.DamageReduction:
                case StatusEffectType.Vulnerable:
                case StatusEffectType.RangedDamageReduction:
                case StatusEffectType.MoraleDamageReduction:
                case StatusEffectType.HealthStatBoost:
                case StatusEffectType.HealthStatReduction:
                case StatusEffectType.ProficiencyBoost:
                case StatusEffectType.RumHealBoost:
                case StatusEffectType.MaxHPBoost:
                case StatusEffectType.Weakness:
                case StatusEffectType.Dodge:
                case StatusEffectType.MissChance:
                case StatusEffectType.MoraleOnKill:
                case StatusEffectType.BuzzGainReduction:
                case StatusEffectType.FoodEffectBoost:
                    return $"[{effect.value1 * 100}%]";
                    
                case StatusEffectType.HealOnCardPlay:
                    return $"[{effect.value1 * 100}% Max HP per card]";

                // Shields / Flat Heals
                case StatusEffectType.Regeneration:
                    return $"[{effect.value1} HP Heal/Turn]";
                case StatusEffectType.MoraleShield:
                case StatusEffectType.Shielded:
                case StatusEffectType.Thorns:
                    return $"[{effect.value1} Amount]";

                // Costs & Resources
                case StatusEffectType.ReduceAllCosts:
                case StatusEffectType.ReduceNextRangedCost:
                    return $"[-{effect.value1} Energy Cost]";
                case StatusEffectType.IncreaseCost:
                case StatusEffectType.EnemyWeaponCostIncrease:
                    return $"[+{effect.value1} Energy Cost]";
                case StatusEffectType.EnergyDrain:
                    return $"[-{effect.value1} Energy/Turn]";
                case StatusEffectType.ReduceCardDraw:
                    return $"[-{effect.value1} Cards Drawn]";
                case StatusEffectType.GrogOnKill:
                    return $"[+{effect.value1} Grog/Kill]";

                // Charges & Complex Triggers
                case StatusEffectType.DrawOnEnemyAttack:
                    return $"[{effect.value1} Charges Left | Attacker discards {effect.value2}]";
                case StatusEffectType.ReturnDamage:
                case StatusEffectType.FreeStows:
                case StatusEffectType.FreeRumUsage:
                case StatusEffectType.RangedBlock:
                case StatusEffectType.WeaponUseTwice:
                    return $"[{effect.value1} Charges Left]";

                // Fallback for simple toggles (like "Stun" or "Taunt") which don't use math values
                default:
                    if (effect.value1 != 0 || effect.value2 != 0)
                    {
                        string fallback = $"[Val1: {effect.value1}";
                        if (effect.value2 != 0) fallback += $", Val2: {effect.value2}";
                        return fallback + "]";
                    }
                    return "";
            }
        }
    }
}