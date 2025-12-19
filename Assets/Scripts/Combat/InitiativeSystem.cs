using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Config;
using TacticalGame.Units;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Handles initiative calculation to determine which team acts first.
    /// Team Initiative = Sum of all unit Speed stats on that team.
    /// </summary>
    public static class InitiativeSystem
    {
        /// <summary>
        /// Result of initiative calculation.
        /// </summary>
        public struct InitiativeResult
        {
            public int PlayerTeamInitiative;
            public int EnemyTeamInitiative;
            public Team FirstTeam;
            public bool IsTie;
        }

        /// <summary>
        /// Calculate which team goes first based on total Speed.
        /// </summary>
        public static InitiativeResult CalculateInitiative()
        {
            var result = new InitiativeResult();
            
            int playerSpeed = 0;
            int enemySpeed = 0;

            // Find all units and sum their Speed
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unitObj in allUnits)
            {
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue; // Don't count surrendered units
                
                if (status.Team == Team.Player)
                {
                    playerSpeed += status.Speed;
                }
                else if (status.Team == Team.Enemy)
                {
                    enemySpeed += status.Speed;
                }
            }

            result.PlayerTeamInitiative = playerSpeed;
            result.EnemyTeamInitiative = enemySpeed;

            // Determine winner
            if (playerSpeed > enemySpeed)
            {
                result.FirstTeam = Team.Player;
                result.IsTie = false;
            }
            else if (enemySpeed > playerSpeed)
            {
                result.FirstTeam = Team.Enemy;
                result.IsTie = false;
            }
            else
            {
                // Tie - player goes first (or could randomize)
                result.FirstTeam = Team.Player;
                result.IsTie = true;
            }

            Debug.Log($"<color=cyan><b>INITIATIVE:</b></color> Player={playerSpeed} vs Enemy={enemySpeed} → " +
                      $"{result.FirstTeam} goes first{(result.IsTie ? " (Tie)" : "")}");

            return result;
        }

        /// <summary>
        /// Get the total Speed for a team.
        /// </summary>
        public static int GetTeamSpeed(Team team)
        {
            int totalSpeed = 0;
            
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unitObj in allUnits)
            {
                UnitStatus status = unitObj.GetComponent<UnitStatus>();
                if (status == null) continue;
                if (status.HasSurrendered) continue;
                
                if (status.Team == team)
                {
                    totalSpeed += status.Speed;
                }
            }

            return totalSpeed;
        }

        /// <summary>
        /// Calculate first-action damage bonus for a unit.
        /// Formula: min(15%, Speed × 0.2%)
        /// </summary>
        public static float GetFirstActionBonus(UnitStatus unit)
        {
            var config = GameConfig.Instance;
            return Mathf.Min(
                config.firstActionBonusCap,
                unit.Speed * config.firstActionBonusPerSpeed
            );
        }
    }
}