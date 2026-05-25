using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Config;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages energy for the Enemy team.
    /// </summary>
    public class EnemyEnergyManager : MonoBehaviour
    {
        private int maxEnergy;
        private int currentEnergy;

        public int MaxEnergy => maxEnergy;
        public int CurrentEnergy => currentEnergy;

        private void Awake()
        {
            ServiceLocator.Register(this);
            maxEnergy = GameConfig.Instance.energyPerTurn;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EnemyEnergyManager>();
        }

        public void StartTurn()
        {
            currentEnergy = maxEnergy;
            Debug.Log($"[EnemyEnergyManager] Turn Started. Energy reset to {maxEnergy}");
        }

        public void EndTurn()
        {
            // Enemies don't currently use grog, so unused energy is just lost.
            currentEnergy = 0;
        }

        public bool TrySpendEnergy(int amount)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                Debug.Log($"[EnemyEnergyManager] Spent {amount} energy. Remaining: {currentEnergy}");
                return true;
            }
            
            Debug.Log("[EnemyEnergyManager] Not enough Energy!");
            return false;
        }

        public bool HasEnergy(int amount)
        {
            return currentEnergy >= amount;
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            currentEnergy += amount;
        }
    }
}
