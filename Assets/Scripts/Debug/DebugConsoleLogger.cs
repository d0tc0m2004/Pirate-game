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

            // 4. RECENT DAMAGE HISTORY
            sb.AppendLine("\n<color=#fdcb6e><b>[4. RECENT DAMAGE HISTORY]</b></color>");
            if (unit.RecentDamageLog != null && unit.RecentDamageLog.Count > 0)
            {
                // Loop through the unit's memory and print every recent hit
                foreach (var log in unit.RecentDamageLog)
                {
                    sb.AppendLine($"  - {log}");
                }
            }
            else
            {
                sb.AppendLine("  - No recent damage taken.");
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