using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalGame.Core;
using TacticalGame.Units;
using TacticalGame.Enums;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages global UI elements: team stats, round info, resources, and unit icons.
    /// Uses event-driven updates where possible to reduce per-frame overhead.
    /// </summary>
    public class GlobalUIManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Team Stats UI")]
        [SerializeField] private Slider playerTeamHPSlider;
        [SerializeField] private TMP_Text playerTeamHPText;
        [SerializeField] private Slider playerTeamMoraleSlider;
        [SerializeField] private TMP_Text playerTeamMoraleText;
        [SerializeField] private Slider enemyTeamHPSlider;
        [SerializeField] private TMP_Text enemyTeamHPText;
        [SerializeField] private Slider enemyTeamMoraleSlider;
        [SerializeField] private TMP_Text enemyTeamMoraleText;

        [Header("Round & Turn UI")]
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text turnStatusText;

        [Header("Resource UI")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text grogText;

        [Header("Action Buttons")]
        [SerializeField] private Button healthRumButton;
        [SerializeField] private Button moraleRumButton;
        [SerializeField] private Button swapButton;

        [Header("Icon Containers")]
        [SerializeField] private Transform playerIconContainer;
        [SerializeField] private Transform enemyIconContainer;
        [SerializeField] private GameObject iconPrefab;

        #endregion

        #region Private State

        private EnergyManager energyManager;
        private BattleManager battleManager;
        private TurnManager turnManager;
        private bool iconsGenerated = false;
        private bool isDirty = true; // Flag to reduce per-frame updates

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GlobalUIManager>();
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            CacheReferences();
            SetupButtons();
            SubscribeToEvents();
            
            if (swapButton != null)
            {
                swapButton.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Only update expensive operations when dirty
            if (isDirty)
            {
                UpdateTeamStats();
                isDirty = false;
            }

            UpdateTurnInfo();
            UpdateSwapButtonVisibility();
        }

        #endregion

        #region Initialization

        private void CacheReferences()
        {
            energyManager = ServiceLocator.Get<EnergyManager>();
            battleManager = ServiceLocator.Get<BattleManager>();
            turnManager = ServiceLocator.Get<TurnManager>();
        }

        private void SetupButtons()
        {
            if (healthRumButton != null)
            {
                healthRumButton.onClick.AddListener(() => OnDrinkRum("Health"));
            }
            
            if (moraleRumButton != null)
            {
                moraleRumButton.onClick.AddListener(() => OnDrinkRum("Morale"));
            }

            if (swapButton != null)
            {
                swapButton.onClick.AddListener(() => battleManager?.InitiateSwapMode());
            }
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            GameEvents.OnUnitDamaged += OnUnitDamaged;
            GameEvents.OnUnitHealed += OnUnitHealed;
            GameEvents.OnMoraleDamaged += OnMoraleDamaged;
            GameEvents.OnUnitDeath += OnUnitDeath;
            GameEvents.OnUnitSurrender += OnUnitSurrender;
            GameEvents.OnEnergyChanged += OnEnergyChanged;
            GameEvents.OnGrogChanged += OnGrogChanged;
            GameEvents.OnPlayerTurnStart += OnTurnChanged;
            GameEvents.OnEnemyTurnStart += OnTurnChanged;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnUnitDamaged -= OnUnitDamaged;
            GameEvents.OnUnitHealed -= OnUnitHealed;
            GameEvents.OnMoraleDamaged -= OnMoraleDamaged;
            GameEvents.OnUnitDeath -= OnUnitDeath;
            GameEvents.OnUnitSurrender -= OnUnitSurrender;
            GameEvents.OnEnergyChanged -= OnEnergyChanged;
            GameEvents.OnGrogChanged -= OnGrogChanged;
            GameEvents.OnPlayerTurnStart -= OnTurnChanged;
            GameEvents.OnEnemyTurnStart -= OnTurnChanged;
        }

        #endregion

        #region Event Handlers

        private void OnUnitDamaged(GameObject unit, int amount) => isDirty = true;
        private void OnUnitHealed(GameObject unit, int amount) => isDirty = true;
        private void OnMoraleDamaged(GameObject unit, int amount) => isDirty = true;
        private void OnUnitDeath(GameObject unit) => isDirty = true;
        private void OnUnitSurrender(GameObject unit) => isDirty = true;
        private void OnTurnChanged() => isDirty = true;

        private void OnEnergyChanged(int newValue)
        {
            if (energyText != null && energyManager != null)
            {
                energyText.text = $"Energy: {energyManager.CurrentEnergy}/{energyManager.MaxEnergy}";
            }
        }

        private void OnGrogChanged(int newValue)
        {
            if (grogText != null)
            {
                grogText.text = $"Grog: {newValue}";
            }
        }

        #endregion

        #region UI Updates

        private void UpdateTeamStats()
        {
            GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
            
            int playerHP = 0, playerMaxHP = 0, playerMorale = 0, playerMaxMorale = 0;
            int enemyHP = 0, enemyMaxHP = 0, enemyMorale = 0, enemyMaxMorale = 0;

            foreach (GameObject unit in units)
            {
                UnitStatus status = unit.GetComponent<UnitStatus>();
                if (status == null) continue;

                if (status.Team == Team.Enemy)
                {
                    enemyHP += status.CurrentHP;
                    enemyMaxHP += status.MaxHP;
                    enemyMorale += status.CurrentMorale;
                    enemyMaxMorale += status.MaxMorale;
                }
                else
                {
                    playerHP += status.CurrentHP;
                    playerMaxHP += status.MaxHP;
                    playerMorale += status.CurrentMorale;
                    playerMaxMorale += status.MaxMorale;
                }
            }

            // Update player UI
            if (playerTeamHPSlider != null)
            {
                playerTeamHPSlider.maxValue = playerMaxHP;
                playerTeamHPSlider.value = playerHP;
            }
            if (playerTeamHPText != null) playerTeamHPText.text = playerHP.ToString();

            if (playerTeamMoraleSlider != null)
            {
                playerTeamMoraleSlider.maxValue = playerMaxMorale;
                playerTeamMoraleSlider.value = playerMorale;
            }
            if (playerTeamMoraleText != null) playerTeamMoraleText.text = playerMorale.ToString();

            // Update enemy UI
            if (enemyTeamHPSlider != null)
            {
                enemyTeamHPSlider.maxValue = enemyMaxHP;
                enemyTeamHPSlider.value = enemyHP;
            }
            if (enemyTeamHPText != null) enemyTeamHPText.text = enemyHP.ToString();

            if (enemyTeamMoraleSlider != null)
            {
                enemyTeamMoraleSlider.maxValue = enemyMaxMorale;
                enemyTeamMoraleSlider.value = enemyMorale;
            }
            if (enemyTeamMoraleText != null) enemyTeamMoraleText.text = enemyMorale.ToString();
        }

        private void UpdateTurnInfo()
        {
            if (turnManager == null) return;

            if (roundText != null)
            {
                roundText.text = $"Round {turnManager.CurrentRound}";
            }

            if (turnStatusText != null)
            {
                turnStatusText.text = turnManager.IsPlayerTurn ? "PLAYER TURN" : "ENEMY TURN";
            }
        }

        private void UpdateSwapButtonVisibility()
        {
            if (swapButton == null || battleManager == null || turnManager == null) return;

            GameObject selected = battleManager.GetSelectedUnit();
            bool showButton = false;

            if (selected != null)
            {
                UnitStatus status = selected.GetComponent<UnitStatus>();
                if (status != null && status.Team == Team.Player)
                {
                    showButton = turnManager.CanSwap() && status.SwapCooldown == 0;
                }
            }

            swapButton.gameObject.SetActive(showButton);
        }

        #endregion

        #region Unit Icons

        /// <summary>
        /// Generate unit icons after deployment.
        /// </summary>
        public void GenerateUnitIcons()
        {
            if (iconsGenerated) return;
            
            ClearIconContainers();

            GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unit in units)
            {
                UnitStatus status = unit.GetComponent<UnitStatus>();
                if (status == null) continue;

                bool isEnemy = status.Team == Team.Enemy;
                Transform parent = isEnemy ? enemyIconContainer : playerIconContainer;
                Color color = isEnemy ? Color.red : Color.blue;

                CreateIcon(parent, color);
            }

            iconsGenerated = true;
        }

        private void ClearIconContainers()
        {
            if (playerIconContainer != null)
            {
                foreach (Transform t in playerIconContainer) Destroy(t.gameObject);
            }
            
            if (enemyIconContainer != null)
            {
                foreach (Transform t in enemyIconContainer) Destroy(t.gameObject);
            }
        }

        private void CreateIcon(Transform parent, Color color)
        {
            if (iconPrefab == null || parent == null) return;

            GameObject iconObj = Instantiate(iconPrefab, parent);
            Image img = iconObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }

        #endregion

        #region Rum Actions

        private void OnDrinkRum(string type)
        {
            if (battleManager == null || energyManager == null) return;

            GameObject selected = battleManager.GetSelectedUnit();
            if (selected == null) return;

            if (!energyManager.TrySpendGrog(1)) return;

            UnitStatus status = selected.GetComponent<UnitStatus>();
            if (status != null)
            {
                status.DrinkRum(type);
                isDirty = true;
            }
        }

        #endregion
    }
}