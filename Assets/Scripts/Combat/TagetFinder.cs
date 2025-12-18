using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Enums;
using TacticalGame.Units;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Utility class for finding valid attack targets.
    /// </summary>
    public static class TargetFinder
    {
        /// <summary>
        /// Find the nearest enemy unit using Manhattan distance.
        /// </summary>
        public static UnitStatus FindNearestEnemy(UnitStatus attacker)
        {
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
            UnitStatus nearest = null;
            float minDistance = float.MaxValue;
            Team attackerTeam = attacker.Team;

            foreach (GameObject unitObj in allUnits)
            {
                if (unitObj == attacker.gameObject) continue;
                
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue;
                
                bool isEnemy = (attackerTeam == Team.Player && status.Team == Team.Enemy) ||
                               (attackerTeam == Team.Enemy && status.Team == Team.Player);
                
                if (isEnemy)
                {
                    float distX = Mathf.Abs(attacker.transform.position.x - unitObj.transform.position.x);
                    float distZ = Mathf.Abs(attacker.transform.position.z - unitObj.transform.position.z);
                    float dist = distX + distZ;
                    
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = status;
                    }
                }
            }
            
            return nearest;
        }

        /// <summary>
        /// Get all enemy units.
        /// </summary>
        public static List<UnitStatus> GetAllEnemies(Team myTeam)
        {
            List<UnitStatus> enemies = new List<UnitStatus>();
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");

            foreach (GameObject unitObj in allUnits)
            {
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue;
                
                bool isEnemy = (myTeam == Team.Player && status.Team == Team.Enemy) ||
                               (myTeam == Team.Enemy && status.Team == Team.Player);
                
                if (isEnemy)
                {
                    enemies.Add(status);
                }
            }
            
            return enemies;
        }

        /// <summary>
        /// Get all allies (including self optionally).
        /// </summary>
        public static List<UnitStatus> GetAllAllies(Team myTeam, bool includeSelf = true, UnitStatus self = null)
        {
            List<UnitStatus> allies = new List<UnitStatus>();
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");

            foreach (GameObject unitObj in allUnits)
            {
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue;
                
                if (!includeSelf && status == self) continue;
                
                if (status.Team == myTeam)
                {
                    allies.Add(status);
                }
            }
            
            return allies;
        }

        /// <summary>
        /// Get all active units.
        /// </summary>
        public static List<UnitStatus> GetAllUnits(bool includeSurrendered = false)
        {
            List<UnitStatus> units = new List<UnitStatus>();
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");

            foreach (GameObject unitObj in allUnits)
            {
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (!includeSurrendered && status.HasSurrendered) continue;
                
                units.Add(status);
            }
            
            return units;
        }

        /// <summary>
        /// Check if any enemies remain active.
        /// </summary>
        public static bool HasActiveEnemies(Team myTeam)
        {
            return GetAllEnemies(myTeam).Count > 0;
        }

        /// <summary>
        /// Check if any allies remain active.
        /// </summary>
        public static bool HasActiveAllies(Team myTeam)
        {
            return GetAllAllies(myTeam).Count > 0;
        }
    }
}