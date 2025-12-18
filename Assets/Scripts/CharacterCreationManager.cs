using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CharacterCreationManager : MonoBehaviour
{
    [System.Serializable]
    public class UnitGenerationPanel
    {
        public TMP_Dropdown roleDropdown;
        public Button generateButton;
        
        public TMP_Text healthText;
        public TMP_Text moraleText;
        public TMP_Text gritText;
        public TMP_Text buzzText;
        public TMP_Text powerText;
        public TMP_Text aimText;
        public TMP_Text proficiencyText;
        public TMP_Text skillText;
        public TMP_Text tacticsText;
        public TMP_Text speedText;
        public TMP_Text hullText;
        public TMP_Text weaponText;

        [HideInInspector] public UnitData generatedData;
    }

    public List<UnitGenerationPanel> playerPanels;
    public List<UnitGenerationPanel> enemyPanels;

    public Button startGameButton;
    public GameObject creationCanvas;
    public GameObject battleCanvas;

    public DeploymentManager deploymentManager;

    private string[] roles = { 
        "Captain", "Quartermaster", "Boatswain", "Shipwright", 
        "Helmsmaster", "Master Gunner", "Master-at-arms", 
        "Navigator", "Surgeon", "Cook", "Swashbuckler", "Deckhand" 
    };

    private void Start()
    {
        SetupPanels(playerPanels, true);
        SetupPanels(enemyPanels, false);

        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (creationCanvas) creationCanvas.SetActive(true);
        if (battleCanvas) battleCanvas.SetActive(false);
    }

    void SetupPanels(List<UnitGenerationPanel> panels, bool isPlayer)
    {
        foreach (var panel in panels)
        {
            panel.roleDropdown.ClearOptions();
            panel.roleDropdown.AddOptions(roles.ToList());
            panel.generateButton.onClick.AddListener(() => GenerateUnit(panel, isPlayer));
        }
    }

    void GenerateUnit(UnitGenerationPanel panel, bool isPlayer)
    {
        string role = panel.roleDropdown.options[panel.roleDropdown.value].text;
        UnitData data = new UnitData();
        data.role = role;
        data.isPlayer = isPlayer;
        data.unitName = isPlayer ? "Player_" + role : "Enemy_" + role;

        Dictionary<string, int> stats = new Dictionary<string, int>
        {
            {"Health", Random.Range(80, 101)},
            {"Morale", Random.Range(80, 101)},
            {"Grit", Random.Range(80, 101)},
            {"Buzz", Random.Range(80, 101)},
            {"Power", Random.Range(80, 101)},
            {"Aim", Random.Range(80, 101)},
            {"Proficiency", Random.Range(80, 101)},
            {"Skill", Random.Range(80, 101)},
            {"Tactics", Random.Range(80, 101)},
            {"Speed", Random.Range(80, 101)},
            {"Hull", Random.Range(80, 101)}
        };

        List<string> statKeys = stats.Keys.ToList();
        List<string> mainStats = GetMainStatsForRole(role, statKeys);
        string secondaryStat = GetRandomSecondaryStat(statKeys, mainStats);

        foreach (var key in statKeys)
        {
            if (mainStats.Contains(key)) stats[key] = Mathf.RoundToInt(stats[key] * 1.2f);
            else if (key == secondaryStat) stats[key] = Mathf.RoundToInt(stats[key] * 1.1f);
        }

        data.health = stats["Health"];
        data.morale = stats["Morale"];
        data.grit = stats["Grit"];
        data.buzz = stats["Buzz"];
        data.power = stats["Power"];
        data.aim = stats["Aim"];
        data.proficiency = stats["Proficiency"];
        data.skill = stats["Skill"];
        data.tactics = stats["Tactics"];
        data.speed = stats["Speed"];
        data.hull = stats["Hull"];

        if (role == "Master-at-arms") data.weaponType = "Melee";
        else if (role == "Master Gunner") data.weaponType = "Ranged";
        else data.weaponType = Random.value > 0.5f ? "Melee" : "Ranged";

        panel.generatedData = data;
        UpdatePanelUI(panel, data);
    }

    List<string> GetMainStatsForRole(string role, List<string> allStats)
    {
        List<string> mains = new List<string>();
        switch (role)
        {
            case "Quartermaster": mains.Add("Health"); break;
            case "Boatswain": mains.Add("Morale"); break;
            case "Shipwright": mains.Add("Grit"); break;
            case "Helmsmaster": mains.Add("Buzz"); break;
            case "Master Gunner": mains.Add("Aim"); break;
            case "Master-at-arms": mains.Add("Power"); break;
            case "Navigator": mains.Add("Skill"); break;
            case "Surgeon": mains.Add("Tactics"); break;
            case "Cook": mains.Add("Proficiency"); break;
            case "Swashbuckler": mains.Add("Speed"); break;
            case "Deckhand": mains.Add("Hull"); break;
            case "Captain":
                mains.Add(allStats[Random.Range(0, allStats.Count)]);
                string second = allStats[Random.Range(0, allStats.Count)];
                while (mains.Contains(second)) second = allStats[Random.Range(0, allStats.Count)];
                mains.Add(second);
                break;
        }
        return mains;
    }

    string GetRandomSecondaryStat(List<string> allStats, List<string> exclude)
    {
        string sec = allStats[Random.Range(0, allStats.Count)];
        while (exclude.Contains(sec)) sec = allStats[Random.Range(0, allStats.Count)];
        return sec;
    }

    void UpdatePanelUI(UnitGenerationPanel panel, UnitData data)
    {
        if(panel.healthText) panel.healthText.text = "Health: " + data.health;
        if(panel.moraleText) panel.moraleText.text = "Morale: " + data.morale;
        if(panel.gritText) panel.gritText.text = "Grit: " + data.grit;
        if(panel.buzzText) panel.buzzText.text = "Buzz: " + data.buzz;
        if(panel.powerText) panel.powerText.text = "Power: " + data.power;
        if(panel.aimText) panel.aimText.text = "Aim: " + data.aim;
        if(panel.proficiencyText) panel.proficiencyText.text = "Proficiency: " + data.proficiency;
        if(panel.skillText) panel.skillText.text = "Skill: " + data.skill;
        if(panel.tacticsText) panel.tacticsText.text = "Tactics: " + data.tactics;
        if(panel.speedText) panel.speedText.text = "Speed: " + data.speed;
        if(panel.hullText) panel.hullText.text = "Hull: " + data.hull;
        
        if(panel.weaponText) panel.weaponText.text = "Weapon: " + data.weaponType;
    }

    void OnStartGameClicked()
    {
        List<UnitData> pUnits = new List<UnitData>();
        List<UnitData> eUnits = new List<UnitData>();

        foreach (var p in playerPanels) if (p.generatedData != null) pUnits.Add(p.generatedData);
        foreach (var e in enemyPanels) if (e.generatedData != null) eUnits.Add(e.generatedData);

        if (pUnits.Count == 0 || eUnits.Count == 0) return;

        if (creationCanvas) creationCanvas.SetActive(false);
        if (battleCanvas) battleCanvas.SetActive(true);

        deploymentManager.StartManualDeployment(pUnits, eUnits);
    }
}