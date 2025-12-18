using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Config;
using TacticalGame.Units;
using TacticalGame.Enums;

namespace TacticalGame.Managers
{
    public class CharacterCreationManager : MonoBehaviour
    {
        [System.Serializable]
        public class UnitGenerationPanel
        {
            public TMP_Dropdown roleDropdown;
            public Button generateButton;
            public TMP_Text healthText, moraleText, gritText, buzzText;
            public TMP_Text powerText, aimText, proficiencyText, skillText;
            public TMP_Text tacticsText, speedText, hullText, weaponText;
            [HideInInspector] public UnitData generatedData;
        }

        [SerializeField] private List<UnitGenerationPanel> playerPanels;
        [SerializeField] private List<UnitGenerationPanel> enemyPanels;
        [SerializeField] private Button startGameButton;
        [SerializeField] private GameObject creationCanvas;
        [SerializeField] private GameObject battleCanvas;
        [SerializeField] private DeploymentManager deploymentManager;

        private static readonly UnitRole[] AllRoles = {
            UnitRole.Captain, UnitRole.Quartermaster, UnitRole.Boatswain,
            UnitRole.Shipwright, UnitRole.Helmsmaster, UnitRole.MasterGunner,
            UnitRole.MasterAtArms, UnitRole.Navigator, UnitRole.Surgeon,
            UnitRole.Cook, UnitRole.Swashbuckler, UnitRole.Deckhand
        };

        private static readonly string[] StatNames = {
            "Health", "Morale", "Grit", "Buzz", "Power",
            "Aim", "Proficiency", "Skill", "Tactics", "Speed", "Hull"
        };

        private void Start()
        {
            SetupPanels(playerPanels, Team.Player);
            SetupPanels(enemyPanels, Team.Enemy);
            if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
            if (creationCanvas) creationCanvas.SetActive(true);
            if (battleCanvas) battleCanvas.SetActive(false);
        }

        private void SetupPanels(List<UnitGenerationPanel> panels, Team team)
        {
            var roleNames = AllRoles.Select(r => r == UnitRole.MasterGunner ? "Master Gunner" : 
                                                  r == UnitRole.MasterAtArms ? "Master-at-arms" : r.ToString()).ToList();
            foreach (var panel in panels)
            {
                panel.roleDropdown.ClearOptions();
                panel.roleDropdown.AddOptions(roleNames);
                panel.generateButton.onClick.AddListener(() => GenerateUnit(panel, team));
            }
        }

        private void GenerateUnit(UnitGenerationPanel panel, Team team)
        {
            var config = GameConfig.Instance;
            UnitRole role = AllRoles[panel.roleDropdown.value];

            var data = new UnitData {
                role = role,
                team = team,
                unitName = $"{team}_{role}"
            };

            var stats = StatNames.ToDictionary(s => s, s => Random.Range(config.minBaseStat, config.maxBaseStat + 1));
            var mainStats = GetMainStatsForRole(role);
            string secondaryStat = StatNames.First(s => !mainStats.Contains(s));

            foreach (var stat in StatNames)
            {
                if (mainStats.Contains(stat))
                    stats[stat] = Mathf.RoundToInt(stats[stat] * config.mainStatMultiplier);
                else if (stat == secondaryStat)
                    stats[stat] = Mathf.RoundToInt(stats[stat] * config.secondaryStatMultiplier);
            }

            data.health = stats["Health"]; data.morale = stats["Morale"];
            data.grit = stats["Grit"]; data.buzz = stats["Buzz"];
            data.power = stats["Power"]; data.aim = stats["Aim"];
            data.proficiency = stats["Proficiency"]; data.skill = stats["Skill"];
            data.tactics = stats["Tactics"]; data.speed = stats["Speed"];
            data.hull = stats["Hull"];

            data.weaponType = role == UnitRole.MasterAtArms ? WeaponType.Melee :
                              role == UnitRole.MasterGunner ? WeaponType.Ranged :
                              Random.value > 0.5f ? WeaponType.Melee : WeaponType.Ranged;

            panel.generatedData = data;
            UpdatePanelUI(panel, data);
        }

        private List<string> GetMainStatsForRole(UnitRole role)
        {
            return role switch {
                UnitRole.Quartermaster => new List<string> { "Health" },
                UnitRole.Boatswain => new List<string> { "Morale" },
                UnitRole.Shipwright => new List<string> { "Grit" },
                UnitRole.Helmsmaster => new List<string> { "Buzz" },
                UnitRole.MasterGunner => new List<string> { "Aim" },
                UnitRole.MasterAtArms => new List<string> { "Power" },
                UnitRole.Navigator => new List<string> { "Skill" },
                UnitRole.Surgeon => new List<string> { "Tactics" },
                UnitRole.Cook => new List<string> { "Proficiency" },
                UnitRole.Swashbuckler => new List<string> { "Speed" },
                UnitRole.Deckhand => new List<string> { "Hull" },
                UnitRole.Captain => new List<string> { 
                    StatNames[Random.Range(0, StatNames.Length)], 
                    StatNames[Random.Range(0, StatNames.Length)] 
                },
                _ => new List<string>()
            };
        }

        private void UpdatePanelUI(UnitGenerationPanel panel, UnitData data)
        {
            if (panel.healthText) panel.healthText.text = $"Health: {data.health}";
            if (panel.moraleText) panel.moraleText.text = $"Morale: {data.morale}";
            if (panel.gritText) panel.gritText.text = $"Grit: {data.grit}";
            if (panel.buzzText) panel.buzzText.text = $"Buzz: {data.buzz}";
            if (panel.powerText) panel.powerText.text = $"Power: {data.power}";
            if (panel.aimText) panel.aimText.text = $"Aim: {data.aim}";
            if (panel.proficiencyText) panel.proficiencyText.text = $"Proficiency: {data.proficiency}";
            if (panel.skillText) panel.skillText.text = $"Skill: {data.skill}";
            if (panel.tacticsText) panel.tacticsText.text = $"Tactics: {data.tactics}";
            if (panel.speedText) panel.speedText.text = $"Speed: {data.speed}";
            if (panel.hullText) panel.hullText.text = $"Hull: {data.hull}";
            if (panel.weaponText) panel.weaponText.text = $"Weapon: {data.weaponType}";
        }

        private void OnStartGameClicked()
        {
            var playerUnits = playerPanels.Where(p => p.generatedData != null).Select(p => p.generatedData).ToList();
            var enemyUnits = enemyPanels.Where(p => p.generatedData != null).Select(p => p.generatedData).ToList();

            if (playerUnits.Count == 0 || enemyUnits.Count == 0) return;

            if (creationCanvas) creationCanvas.SetActive(false);
            if (battleCanvas) battleCanvas.SetActive(true);
            if (deploymentManager) deploymentManager.StartManualDeployment(playerUnits, enemyUnits);
        }
    }
}