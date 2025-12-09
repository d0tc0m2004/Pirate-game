using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlobalUIManager : MonoBehaviour
{
    [Header("--- TEAM STATS UI (Top Screen) ---")]
    public Slider playerTeamHPSlider;
    public TMP_Text playerTeamHPText; // Displays "450/500"
    public Slider playerTeamMoraleSlider;
    public TMP_Text playerTeamMoraleText; // Displays "100/100"

    public Slider enemyTeamHPSlider;
    public TMP_Text enemyTeamHPText;
    public Slider enemyTeamMoraleSlider;
    public TMP_Text enemyTeamMoraleText;

    [Header("--- ROUND & TURN UI ---")]
    public TMP_Text roundText;
    public TMP_Text turnStatusText;

    [Header("--- RUM & ECONOMY UI ---")]
    public TMP_Text energyText; 
    public TMP_Text grogText;   
    public Button healthRumButton; 
    public Button moraleRumButton; 

    [Header("--- ICONS ---")]
    public Transform playerIconContainer; 
    public Transform enemyIconContainer;  
    public GameObject iconPrefab;         

    [Header("--- MANAGERS ---")]
    public EnergyManager energyManager;
    public BattleManager battleManager;
    public TurnManager turnManager;

    private bool iconsGenerated = false;

    private void Start()
    {
        // Auto-find managers
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();

        // Setup Buttons
        if (healthRumButton) healthRumButton.onClick.AddListener(() => OnDrinkRum("Health"));
        if (moraleRumButton) moraleRumButton.onClick.AddListener(() => OnDrinkRum("Morale"));
    }

    private void Update()
    {
        // 1. UPDATE TEAM STATS (The missing part!)
        UpdateTeamStats();

        // 2. UPDATE ROUND & TURN TEXT
        if (turnManager)
        {
            if (roundText) roundText.text = "Round " + turnManager.currentRound;
            
            if (turnStatusText) 
            {
                turnStatusText.text = turnManager.isPlayerTurn ? "PLAYER TURN" : "ENEMY TURN";
            }
        }

        // 3. UPDATE ECONOMY TEXT
        if (energyManager)
        {
            if (energyText) energyText.text = $"Energy: {energyManager.currentEnergy}/{energyManager.maxEnergy}";
            if (grogText) grogText.text = $"Grog: {energyManager.grogTokens}";
        }
    }

    void UpdateTeamStats()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");

        int playerHP = 0, playerMaxHP = 0;
        int playerMorale = 0, playerMaxMorale = 0;

        int enemyHP = 0, enemyMaxHP = 0;
        int enemyMorale = 0, enemyMaxMorale = 0;

        foreach (GameObject unit in units)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) continue;

            if (unit.name.Contains("Enemy"))
            {
                enemyHP += status.currentHP;
                enemyMaxHP += status.maxHP;
                enemyMorale += status.currentMorale;
                enemyMaxMorale += status.maxMorale;
            }
            else
            {
                playerHP += status.currentHP;
                playerMaxHP += status.maxHP;
                playerMorale += status.currentMorale;
                playerMaxMorale += status.maxMorale;
            }
        }

        // --- UPDATE UI ELEMENTS ---
        
        // PLAYER
        if (playerTeamHPSlider) { playerTeamHPSlider.maxValue = playerMaxHP; playerTeamHPSlider.value = playerHP; }
        if (playerTeamHPText) playerTeamHPText.text = $"{playerHP}";

        if (playerTeamMoraleSlider) { playerTeamMoraleSlider.maxValue = playerMaxMorale; playerTeamMoraleSlider.value = playerMorale; }
        if (playerTeamMoraleText) playerTeamMoraleText.text = $"{playerMorale}";

        // ENEMY
        if (enemyTeamHPSlider) { enemyTeamHPSlider.maxValue = enemyMaxHP; enemyTeamHPSlider.value = enemyHP; }
        if (enemyTeamHPText) enemyTeamHPText.text = $"{enemyHP}";

        if (enemyTeamMoraleSlider) { enemyTeamMoraleSlider.maxValue = enemyMaxMorale; enemyTeamMoraleSlider.value = enemyMorale; }
        if (enemyTeamMoraleText) enemyTeamMoraleText.text = $"{enemyMorale}";
    }

    public void GenerateUnitIcons()
    {
        if (iconsGenerated) return;

        foreach(Transform t in playerIconContainer) Destroy(t.gameObject);
        foreach(Transform t in enemyIconContainer) Destroy(t.gameObject);

        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");

        foreach (GameObject u in units)
        {
            UnitStatus status = u.GetComponent<UnitStatus>();
            if (status != null)
            {
                bool isEnemy = u.name.Contains("Enemy");
                Transform parent = isEnemy ? enemyIconContainer : playerIconContainer;
                Color color = isEnemy ? Color.red : Color.blue;

                GameObject iconObj = Instantiate(iconPrefab, parent);
                Image img = iconObj.GetComponent<Image>();
                if (img) img.color = color;

                if (u.name.Contains("Captain"))
                {
                    LayoutElement layout = iconObj.GetComponent<LayoutElement>();
                    if (layout != null) { layout.minWidth = 60; layout.minHeight = 60; }
                }
            }
        }
        iconsGenerated = true;
    }

    void OnDrinkRum(string type)
    {
        GameObject selected = battleManager.GetSelectedUnit(); 
        if (selected == null) return;

        if (!energyManager.TrySpendGrog(1)) return;

        UnitStatus status = selected.GetComponent<UnitStatus>();
        if (status != null) status.DrinkRum(type);
    }
}