using UnityEngine;
using TMPro;
using TacticalGame.Core;
using TacticalGame.Config;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages energy and grog resources for the player.
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text grogText;

        #endregion

        #region Private State

        private int maxEnergy;
        private int currentEnergy;
        private int grogTokens = 0;

        #endregion

        #region Public Properties

        public int MaxEnergy => maxEnergy;
        public int CurrentEnergy => currentEnergy;
        public int GrogTokens => grogTokens;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
            maxEnergy = GameConfig.Instance.energyPerTurn;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EnergyManager>();
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Called at the start of the player's turn.
        /// </summary>
        public void StartTurn()
        {
            currentEnergy = maxEnergy;
            UpdateUI();
            GameEvents.TriggerEnergyChanged(currentEnergy);
        }

        /// <summary>
        /// Called at the end of the player's turn. Converts unused energy to grog.
        /// </summary>
        public void EndTurn()
        {
            if (currentEnergy > 0)
            {
                grogTokens += currentEnergy;
                Debug.Log($"Converted {currentEnergy} Energy into Grog! Total Grog: {grogTokens}");
                
                GameEvents.TriggerGrogChanged(grogTokens);
                currentEnergy = 0;
            }
            
            UpdateUI();
        }

        #endregion

        #region Energy Spending

        /// <summary>
        /// Attempt to spend energy. Returns true if successful.
        /// </summary>
        public bool TrySpendEnergy(int amount)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                UpdateUI();
                GameEvents.TriggerEnergyChanged(currentEnergy);
                return true;
            }
            
            Debug.Log("Not enough Energy!");
            return false;
        }

        /// <summary>
        /// Check if player has enough energy.
        /// </summary>
        public bool HasEnergy(int amount)
        {
            return currentEnergy >= amount;
        }

        #endregion

        #region Grog Management

        /// <summary>
        /// Attempt to spend grog. Returns true if successful.
        /// </summary>
        public bool TrySpendGrog(int amount)
        {
            if (grogTokens >= amount)
            {
                grogTokens -= amount;
                UpdateUI();
                GameEvents.TriggerGrogChanged(grogTokens);
                return true;
            }
            
            Debug.Log("Not enough Grog!");
            return false;
        }

        /// <summary>
        /// Check if player has enough grog.
        /// </summary>
        public bool HasGrog(int amount)
        {
            return grogTokens >= amount;
        }

        /// <summary>
        /// Add grog tokens.
        /// </summary>
        public void AddGrog(int amount)
        {
            grogTokens += amount;
            UpdateUI();
            GameEvents.TriggerGrogChanged(grogTokens);
        }

        #endregion

        #region UI

        private void UpdateUI()
        {
            if (energyText != null)
            {
                energyText.text = $"Energy: {currentEnergy}/{maxEnergy}";
            }
            
            if (grogText != null)
            {
                grogText.text = $"Grog: {grogTokens}";
            }
        }

        #endregion
    }
}