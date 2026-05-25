using UnityEngine;
using TacticalGame.AI;
using TacticalGame.Grid;

namespace TacticalGame.UI
{
    [RequireComponent(typeof(EnemyBrain))]
    public class IntentWarningUI : MonoBehaviour
    {
        private EnemyBrain brain;
        private LineRenderer moveLine;
        private LineRenderer attackLine;

        private void Awake()
        {
            brain = GetComponent<EnemyBrain>();

            // Move Line — clear cyan
            GameObject moveLineObj = new GameObject("MoveIntentLine");
            moveLineObj.transform.SetParent(transform);
            moveLine = moveLineObj.AddComponent<LineRenderer>();
            moveLine.startWidth = 0.08f;
            moveLine.endWidth = 0.08f;
            moveLine.material = new Material(Shader.Find("Sprites/Default"));
            moveLine.startColor = new Color(0f, 0.9f, 1f, 0.9f);
            moveLine.endColor = new Color(0f, 0.9f, 1f, 0.9f);
            moveLine.positionCount = 0;
            moveLine.sortingOrder = 10;

            // Attack Line — clear red (or green for support)
            GameObject attackLineObj = new GameObject("AttackIntentLine");
            attackLineObj.transform.SetParent(transform);
            attackLine = attackLineObj.AddComponent<LineRenderer>();
            attackLine.startWidth = 0.1f;
            attackLine.endWidth = 0.03f;
            attackLine.material = new Material(Shader.Find("Sprites/Default"));
            attackLine.startColor = Color.red;
            attackLine.endColor = new Color(1f, 0f, 0f, 0.4f);
            attackLine.positionCount = 0;
            attackLine.sortingOrder = 11;
        }

        private void Update()
        {
            if (brain.CurrentIntent == null)
            {
                moveLine.positionCount = 0;
                attackLine.positionCount = 0;
                return;
            }

            var intent = brain.CurrentIntent;
            Vector3 startPos = transform.position + Vector3.up * 0.3f;

            // 1. Move Intent — clear straight line to destination tile
            if (intent.TargetMoveCell != null)
            {
                Vector3 moveTarget = intent.TargetMoveCell.GetWorldPosition() + Vector3.up * 0.3f;
                if (Vector3.Distance(startPos, moveTarget) > 0.1f)
                {
                    moveLine.positionCount = 2;
                    moveLine.SetPosition(0, startPos);
                    moveLine.SetPosition(1, moveTarget);
                }
                else
                {
                    moveLine.positionCount = 0;
                }

                // Attack line starts from where the enemy will be after moving
                startPos = moveTarget;
            }
            else
            {
                moveLine.positionCount = 0;
            }

            // 2. Attack / Card Intent — clear straight line to target
            if (intent.Action == EnemyBrain.IntentAction.Attack || intent.Action == EnemyBrain.IntentAction.PlayCard)
            {
                Vector3 attackTarget = startPos;
                bool hasTarget = false;

                if (intent.TargetUnit != null)
                {
                    attackTarget = intent.TargetUnit.transform.position + Vector3.up * 0.3f;
                    hasTarget = true;

                    if (intent.TargetUnit.Team == brain.GetComponent<TacticalGame.Units.UnitStatus>().Team)
                    {
                        // Green for support/heal
                        attackLine.startColor = new Color(0f, 1f, 0.3f, 0.9f);
                        attackLine.endColor = new Color(0f, 1f, 0.3f, 0.4f);
                    }
                    else
                    {
                        // Red for attack
                        attackLine.startColor = new Color(1f, 0.1f, 0.1f, 0.9f);
                        attackLine.endColor = new Color(1f, 0.1f, 0.1f, 0.4f);
                    }
                }
                else if (intent.TargetLocker != null)
                {
                    attackTarget = intent.TargetLocker.transform.position + Vector3.up * 0.3f;
                    hasTarget = true;
                    attackLine.startColor = new Color(1f, 0.1f, 0.1f, 0.9f);
                    attackLine.endColor = new Color(1f, 0.1f, 0.1f, 0.4f);
                }

                if (hasTarget)
                {
                    attackLine.positionCount = 2;
                    attackLine.SetPosition(0, startPos);
                    attackLine.SetPosition(1, attackTarget);
                }
                else
                {
                    attackLine.positionCount = 0;
                }
            }
            else
            {
                attackLine.positionCount = 0;
            }
        }
    }
}
