using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Combat;
using TacticalGame.Core;
using TacticalGame.Enums;
using TacticalGame.Equipment;

namespace TacticalGame.AI
{
    public class EnemyBrain : MonoBehaviour
    {
        private UnitStatus myStatus;
        private UnitMovement myMovement;
        private UnitAttack myAttack;
        
        public enum IntentAction { None, MoveOnly, Attack, PlayCard }
        public enum CardCategory { Offense, Support, Mobility, Utility }

        [System.Serializable]
        public class AIIntent
        {
            public GridCell TargetMoveCell;
            public UnitStatus TargetUnit;
            public DeadMansLocker TargetLocker;
            public IntentAction Action;
            public int Score;
            public BattleCard AssignedCard;
        }

        private AIIntent currentIntent;
        public AIIntent CurrentIntent => currentIntent;

        private void Awake()
        {
            myStatus = GetComponent<UnitStatus>();
            myMovement = GetComponent<UnitMovement>();
            myAttack = GetComponent<UnitAttack>();
        }

        public void ClearIntent()
        {
            currentIntent = null;
        }

        public void EvaluateAndLockIntent()
        {
            if (myStatus == null || myStatus.HasSurrendered || myStatus.IsStunned) 
            {
                currentIntent = null;
                return;
            }

            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;

            Vector2Int currentPos = gridManager.WorldToGridPosition(transform.position);
            GridCell currentCell = gridManager.GetCell(currentPos.x, currentPos.y);

            List<AIIntent> possibleIntents = new List<AIIntent>();

            // Include current cell
            possibleIntents.AddRange(EvaluateCell(gridManager, currentCell, currentPos));

            // Include all reachable cells
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    if (x == currentPos.x && y == currentPos.y) continue; // Already evaluated

                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null && myMovement != null && myMovement.CanMoveToCell(cell))
                    {
                        possibleIntents.AddRange(EvaluateCell(gridManager, cell, new Vector2Int(x, y)));
                    }
                }
            }

            if (possibleIntents.Count > 0)
            {
                // Sort by score descending
                possibleIntents.Sort((a, b) => b.Score.CompareTo(a.Score));
                
                // Collect top scores to break ties randomly
                int topScore = possibleIntents[0].Score;
                List<AIIntent> bestIntents = possibleIntents.FindAll(i => i.Score == topScore);
                
                currentIntent = bestIntents[Random.Range(0, bestIntents.Count)];
                
                Debug.Log($"<color=orange>{name} Intent Locked:</color> Move to ({currentIntent.TargetMoveCell.XPosition}, {currentIntent.TargetMoveCell.YPosition}), Action: {currentIntent.Action}, Score: {currentIntent.Score}");
            }
            else
            {
                currentIntent = null;
            }
        }

        private List<AIIntent> EvaluateCell(GridManager grid, GridCell moveCell, Vector2Int cellPos)
        {
            List<AIIntent> intents = new List<AIIntent>();

            AIIntent baseMove = new AIIntent 
            { 
                TargetMoveCell = moveCell, 
                Action = IntentAction.MoveOnly,
                Score = 0 
            };

            if (moveCell.HasHazard) baseMove.Score -= 100;

            // Find nearest target from this simulated cell (Manhattan distance)
            UnitStatus nearestTarget = null;
            DeadMansLocker nearestLocker = null;
            float minDistance = float.MaxValue;

            GameObject[] allUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray();
            foreach (var unitObj in allUnits)
            {
                if (unitObj == gameObject) continue;
                
                var status = unitObj.GetComponent<UnitStatus>();
                if (status != null && status.Team == Team.Player && !status.HasSurrendered)
                {
                    Vector2Int targetPos = grid.WorldToGridPosition(unitObj.transform.position);
                    float dist = Mathf.Abs(cellPos.x - targetPos.x) + Mathf.Abs(cellPos.y - targetPos.y);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestTarget = status;
                        nearestLocker = null; // Clear locker if unit is closer
                    }
                }

                var locker = unitObj.GetComponent<DeadMansLocker>();
                if (locker != null && !locker.IsDestroyed)
                {
                    Vector2Int lockerPos = grid.WorldToGridPosition(unitObj.transform.position);
                    float dist = Mathf.Abs(cellPos.x - lockerPos.x) + Mathf.Abs(cellPos.y - lockerPos.y);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestTarget = null; // Clear unit if locker is closer
                        nearestLocker = locker;
                    }
                }
            }

            intents.Add(baseMove);

            if (nearestTarget != null)
            {
                intents.Add(new AIIntent
                {
                    TargetMoveCell = moveCell,
                    TargetUnit = nearestTarget,
                    Action = IntentAction.Attack,
                    Score = baseMove.Score + 10 // Equal priority
                });
            }
            else if (nearestLocker != null)
            {
                intents.Add(new AIIntent
                {
                    TargetMoveCell = moveCell,
                    TargetLocker = nearestLocker,
                    Action = IntentAction.Attack,
                    Score = baseMove.Score + 10 // Equal priority
                });
            }

            return intents;
        }

        public bool EvaluateAndLockCardIntent(BattleCard card)
        {
            if (myStatus == null || myStatus.HasSurrendered || myStatus.IsStunned) return false;

            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;

            Vector2Int currentPos = gridManager.WorldToGridPosition(transform.position);
            GridCell currentCell = gridManager.GetCell(currentPos.x, currentPos.y);

            CardCategory category = CategorizeCard(card);

            List<AIIntent> possibleIntents = new List<AIIntent>();
            // Always evaluate current cell
            possibleIntents.AddRange(EvaluateCellForCard(gridManager, currentCell, currentPos, card));

            // Only evaluate moving to other cells if the card provides mobility
            if (category == CardCategory.Mobility)
            {
                for (int x = 0; x < gridManager.GridWidth; x++)
                {
                    for (int y = 0; y < gridManager.GridHeight; y++)
                    {
                        if (x == currentPos.x && y == currentPos.y) continue;

                        GridCell cell = gridManager.GetCell(x, y);
                        if (cell != null && myMovement != null && myMovement.CanMoveToCell(cell))
                        {
                            possibleIntents.AddRange(EvaluateCellForCard(gridManager, cell, new Vector2Int(x, y), card));
                        }
                    }
                }
            }

            if (possibleIntents.Count > 0)
            {
                possibleIntents.Sort((a, b) => b.Score.CompareTo(a.Score));
                
                int topScore = possibleIntents[0].Score;
                if (topScore <= 0) return false; // Card isn't useful

                List<AIIntent> bestIntents = possibleIntents.FindAll(i => i.Score == topScore);
                currentIntent = bestIntents[Random.Range(0, bestIntents.Count)];
                
                Debug.Log($"<color=orange>{name} Card Intent Locked:</color> Play {card.GetDisplayName()} from ({currentIntent.TargetMoveCell.XPosition}, {currentIntent.TargetMoveCell.YPosition})");
                return true;
            }
            
            return false;
        }

        private CardCategory CategorizeCard(BattleCard card)
        {
            if (card.IsWeaponCard) return CardCategory.Offense;

            string effectName = card.effectType.ToString();
            
            if (effectName.Contains("Heal") || effectName.Contains("Restore") || effectName.Contains("Shield") || effectName.Contains("Ally"))
                return CardCategory.Support;
                
            if (effectName.Contains("Damage") || effectName.Contains("Kill") || effectName.Contains("Attack") || effectName.Contains("Strike") || card.category == RelicCategory.Ultimate || card.category == RelicCategory.Gloves)
                return CardCategory.Offense;

            if (effectName.Contains("Move") || effectName.Contains("Swap") || card.category == RelicCategory.Boots)
                return CardCategory.Mobility;

            return CardCategory.Utility;
        }

        private List<AIIntent> EvaluateCellForCard(GridManager grid, GridCell moveCell, Vector2Int cellPos, BattleCard card)
        {
            List<AIIntent> intents = new List<AIIntent>();
            AIIntent baseMove = new AIIntent { TargetMoveCell = moveCell, Action = IntentAction.PlayCard, Score = 0, AssignedCard = card };
            if (moveCell.HasHazard) baseMove.Score -= 100;

            CardCategory category = CategorizeCard(card);

            UnitStatus nearestEnemy = null;
            UnitStatus lowestHealthAlly = null;
            float minEnemyDist = float.MaxValue;
            float minAllyHp = float.MaxValue;

            GameObject[] allUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray();
            foreach (var unitObj in allUnits)
            {
                if (unitObj == gameObject) continue;
                var status = unitObj.GetComponent<UnitStatus>();
                if (status != null && !status.HasSurrendered)
                {
                    if (status.Team == Team.Player) // Enemy to us
                    {
                        Vector2Int targetPos = grid.WorldToGridPosition(unitObj.transform.position);
                        float dist = Mathf.Abs(cellPos.x - targetPos.x) + Mathf.Abs(cellPos.y - targetPos.y);
                        if (dist < minEnemyDist)
                        {
                            minEnemyDist = dist;
                            nearestEnemy = status;
                        }
                    }
                    else if (status.Team == Team.Enemy) // Ally to us
                    {
                        if (status.CurrentHP < status.MaxHP && status.CurrentHP < minAllyHp)
                        {
                            minAllyHp = status.CurrentHP;
                            lowestHealthAlly = status;
                        }
                    }
                }
            }

            if (category == CardCategory.Offense)
            {
                if (nearestEnemy != null)
                {
                    baseMove.TargetUnit = nearestEnemy;
                    baseMove.Score += 50; // High priority for offense
                }
                else baseMove.Score -= 100; // Useless if no target
            }
            else if (category == CardCategory.Support)
            {
                if (lowestHealthAlly != null)
                {
                    baseMove.TargetUnit = lowestHealthAlly;
                    baseMove.Score += 50;
                    if (lowestHealthAlly.CurrentHP < lowestHealthAlly.MaxHP / 2) baseMove.Score += 30; // Critical heal
                }
                else
                {
                    // No injured ally, maybe cast on self?
                    if (myStatus.CurrentHP < myStatus.MaxHP)
                    {
                        baseMove.TargetUnit = myStatus;
                        baseMove.Score += 40;
                    }
                    else baseMove.Score -= 100; // Useless if everyone full HP
                }
            }
            else if (category == CardCategory.Mobility)
            {
                baseMove.TargetUnit = myStatus; // Target self for movement abilities
                baseMove.Score += 30;
                if (myStatus.CurrentHP < myStatus.MaxHP / 2) baseMove.Score += 20; // Good to escape
            }
            else // Utility
            {
                baseMove.TargetUnit = myStatus;
                baseMove.Score += 20;
            }

            intents.Add(baseMove);
            return intents;
        }

        public IEnumerator ExecuteLockedIntent()
        {
            if (currentIntent == null || myStatus == null || myStatus.HasSurrendered || myStatus.IsStunned)
            {
                yield break;
            }

            // Execute Attack or Card
            if (currentIntent.Action == IntentAction.PlayCard && currentIntent.AssignedCard != null)
            {
                var deckManager = ServiceLocator.Get<EnemyDeckManager>();
                if (deckManager != null)
                {
                    deckManager.PlayAssignedCard(currentIntent.AssignedCard, currentIntent.TargetUnit, currentIntent.TargetMoveCell);
                }
            }
            else if (currentIntent.Action == IntentAction.Attack)
            {
                if (myAttack != null)
                {
                    if (myStatus != null && myStatus.WeaponType == WeaponType.Ranged)
                    {
                        myAttack.TryRangedAttack();
                    }
                    else
                    {
                        myAttack.TryMeleeAttack();
                    }
                }
            }

            // Clear intent after execution
            currentIntent = null;
            
            yield return new WaitForSeconds(0.5f);
        }
    }
}
