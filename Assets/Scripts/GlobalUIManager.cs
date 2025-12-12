using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlobalUIManager : MonoBehaviour
{
    [Header("Team Stats UI")]
    public Slider playerTeamHPSlider;
    public TMP_Text playerTeamHPText;
    public Slider playerTeamMoraleSlider;
    public TMP_Text playerTeamMoraleText;

    public Slider enemyTeamHPSlider;
    public TMP_Text enemyTeamHPText;
    public Slider enemyTeamMoraleSlider;
    public TMP_Text enemyTeamMoraleText;

    [Header("Round & Turn UI")]
    public TMP_Text roundText;
    public TMP_Text turnStatusText;

    [Header("Rum & Economy UI")]
    public TMP_Text energyText; 
    public TMP_Text grogText;   
    public Button healthRumButton; 
    public Button moraleRumButton; 

    [Header("NEW SWAP UI")]
    public Button swapButton;

    [Header("Icon Containers")]
    public Transform playerIconContainer; 
    public Transform enemyIconContainer;  
    public GameObject iconPrefab;         

    [Header("Managers")]
    public EnergyManager energyManager;
    public BattleManager battleManager;
    public TurnManager turnManager;

    private bool iconsGenerated = false;

    private void Start()
    {
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();

        if (healthRumButton) healthRumButton.onClick.AddListener(() => OnDrinkRum("Health"));
        if (moraleRumButton) moraleRumButton.onClick.AddListener(() => OnDrinkRum("Morale"));
        
        if (swapButton) 
        {
            swapButton.onClick.AddListener(() => battleManager.InitiateSwapMode());
            swapButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateTeamStats();

        if (turnManager)
        {
            if (roundText) roundText.text = "Round " + turnManager.currentRound;
            if (turnStatusText) turnStatusText.text = turnManager.isPlayerTurn ? "PLAYER TURN" : "ENEMY TURN";
        }

        if (energyManager)
        {
            if (energyText) energyText.text = $"Energy: {energyManager.currentEnergy}";
            if (grogText) grogText.text = $"Grog: {energyManager.grogTokens}";
        }
        
        if (swapButton && battleManager)
        {
            bool showButton = battleManager.GetSelectedUnit() != null;
            if (showButton && battleManager.GetSelectedUnit().name.Contains("Enemy")) showButton = false;
            
            swapButton.gameObject.SetActive(showButton);
        }
    }

    void UpdateTeamStats()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        int pHP = 0, pMax = 0, pMorale = 0, pMaxMorale = 0;
        int eHP = 0, eMax = 0, eMorale = 0, eMaxMorale = 0;

        foreach (GameObject unit in units)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) continue;
            if (unit.name.Contains("Enemy")) { eHP += status.currentHP; eMax += status.maxHP; eMorale += status.currentMorale; eMaxMorale += status.maxMorale; }
            else { pHP += status.currentHP; pMax += status.maxHP; pMorale += status.currentMorale; pMaxMorale += status.maxMorale; }
        }

        if (playerTeamHPSlider) { playerTeamHPSlider.maxValue = pMax; playerTeamHPSlider.value = pHP; }
        if (playerTeamHPText) playerTeamHPText.text = $"{pHP}";
        if (playerTeamMoraleSlider) { playerTeamMoraleSlider.maxValue = pMaxMorale; playerTeamMoraleSlider.value = pMorale; }
        if (playerTeamMoraleText) playerTeamMoraleText.text = $"{pMorale}";

        if (enemyTeamHPSlider) { enemyTeamHPSlider.maxValue = eMax; enemyTeamHPSlider.value = eHP; }
        if (enemyTeamHPText) enemyTeamHPText.text = $"{eHP}";
        if (enemyTeamMoraleSlider) { enemyTeamMoraleSlider.maxValue = eMaxMorale; enemyTeamMoraleSlider.value = eMorale; }
        if (enemyTeamMoraleText) enemyTeamMoraleText.text = $"{eMorale}";
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
                if (u.name.Contains("Captain")) {
                    LayoutElement layout = iconObj.GetComponent<LayoutElement>();
                    if (layout != null) { layout.minWidth = 60; layout.minHeight = 60; }
                }
            }
        }
        iconsGenerated = true;
    }

    void OnDrinkRum(string type)
    {
        if (battleManager == null) return;
        GameObject selected = battleManager.GetSelectedUnit(); 
        if (selected == null) return;
        if (!energyManager.TrySpendGrog(1)) return;
        UnitStatus status = selected.GetComponent<UnitStatus>();
        if (status != null) status.DrinkRum(type);
    }
}