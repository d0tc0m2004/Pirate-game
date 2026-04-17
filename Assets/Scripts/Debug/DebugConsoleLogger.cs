using UnityEngine;
using System.Text;
using TacticalGame.Units;
using TacticalGame.Combat;
using TacticalGame.Managers;
using TacticalGame.Equipment;
using TacticalGame.Grid;
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

        // Explicitly telling Unity to use its built-in Camera class
        private UnityEngine.Camera mainCam;

        private void Start()
        {
            // Explicitly using UnityEngine.Camera here too
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

            // 2. STATUS EFFECTS (Buffs/Debuffs)
            sb.AppendLine("\n<color=yellow><b>[2. STATUS EFFECTS]</b></color>");
            var effectsManager = unit.GetComponent<StatusEffectManager>();
            if (effectsManager != null)
            {
                var buffs = effectsManager.GetActiveBuffs();
                if (buffs.Count > 0)
                {
                    sb.AppendLine("  <color=#00FF00>Buffs:</color>");
                    // FIXED: Changed b.duration to b.remainingTurns
                    foreach (var b in buffs) sb.AppendLine($"   + {b.effectName} ({b.remainingTurns} turns left)");
                }

                var debuffs = effectsManager.GetActiveDebuffs();
                if (debuffs.Count > 0)
                {
                    sb.AppendLine("  <color=#FF4444>Debuffs:</color>");
                    // FIXED: Changed d.duration to d.remainingTurns
                    foreach (var d in debuffs) sb.AppendLine($"   - {d.effectName} ({d.remainingTurns} turns left)");
                }

                if (buffs.Count == 0 && debuffs.Count == 0) sb.AppendLine("  None Active");
            }

            // 3. GRID & HAZARD LAYER (Where they stand)
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
                        sb.AppendLine($"  - Cell is currently marked as having an active hazard/trap.");
                    }
                    else
                    {
                        sb.AppendLine("- Tile is safe (No Hazards)");
                    }
                }
            }

            // Log it as one single block so the console doesn't clutter
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

            // DECK STATE
            if (BattleDeckManager.Instance != null)
            {
                sb.AppendLine("\n<color=cyan><b>[DECK & HAND]</b></color>");
                sb.AppendLine($"- Cards in Hand: {BattleDeckManager.Instance.HandCount}");
                sb.AppendLine($"- Cards in Deck: {BattleDeckManager.Instance.DeckCount}");
                sb.AppendLine($"- Cards in Discard: {BattleDeckManager.Instance.DiscardCount}");
                
                int stowed = BattleDeckManager.Instance.GetStowedCount();
                if (stowed > 0) sb.AppendLine($"- Stowed Cards: {stowed}");
            }

            Debug.Log(sb.ToString());
        }
    }
}