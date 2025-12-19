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
    /// <summary>
    /// Manages the character creation / unit generation screen.
    /// Uses the new stat generation system with primary/secondary stats based on role.
    /// </summary>
    public class CharacterCreationManager : MonoBehaviour
    {
        #region Nested Types

        [System.Serializable]
        public class UnitGenerationPanel
        {
            [Header("Role Selection")]
            public TMP_Dropdown roleDropdown;
            public Button generateButton;

            [Header("Stat Display Texts")]
            public TMP_Text healthText;
            public TMP_Text moraleText;
            public TMP_Text buzzText;
            public TMP_Text powerText;
            public TMP_Text aimText;
            public TMP_Text tacticsText;
            public TMP_Text skillText;
            public TMP_Text proficiencyText;
            public TMP_Text gritText;
            public TMP_Text hullText;
            public TMP_Text speedText;

            [Header("Other Info")]
            public TMP_Text weaponText;
            public TMP_Text primaryStatText;    // Shows which stat is primary
            public TMP_Text secondaryStatText;  // Shows which stat is secondary

            [HideInInspector] public UnitData generatedData;
        }

        #endregion

        #region Serialized Fields

        [Header("Panels")]
        [SerializeField] private List<UnitGenerationPanel> playerPanels;
        [SerializeField] private List<UnitGenerationPanel> enemyPanels;

        [Header("UI")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private GameObject creationCanvas;
        [SerializeField] private GameObject battleCanvas;

        [Header("References")]
        [SerializeField] private DeploymentManager deploymentManager;

        [Header("Display Settings")]
        [SerializeField] private Color primaryStatColor = new Color(0.2f, 1f, 0.2f);   // Green
        [SerializeField] private Color secondaryStatColor = new Color(1f, 0.8f, 0.2f); // Yellow
        [SerializeField] private Color normalStatColor = Color.white;

        #endregion

        #region Constants

        private static readonly UnitRole[] AllRoles =
        {
            UnitRole.Captain,
            UnitRole.Quartermaster,
            UnitRole.Boatswain,
            UnitRole.Shipwright,
            UnitRole.Helmsmaster,
            UnitRole.MasterGunner,
            UnitRole.MasterAtArms,
            UnitRole.Navigator,
            UnitRole.Surgeon,
            UnitRole.Cook,
            UnitRole.Swashbuckler,
            UnitRole.Deckhand
        };

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SetupPanels(playerPanels, Team.Player);
            SetupPanels(enemyPanels, Team.Enemy);

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (creationCanvas != null) creationCanvas.SetActive(true);
            if (battleCanvas != null) battleCanvas.SetActive(false);
        }

        #endregion

        #region Panel Setup

        private void SetupPanels(List<UnitGenerationPanel> panels, Team team)
        {
            var roleNames = AllRoles.Select(r => GetRoleDisplayName(r)).ToList();
            
            foreach (var panel in panels)
            {
                if (panel.roleDropdown != null)
                {
                    panel.roleDropdown.ClearOptions();
                    panel.roleDropdown.AddOptions(roleNames);
                }

                if (panel.generateButton != null)
                {
                    panel.generateButton.onClick.AddListener(() => GenerateUnit(panel, team));
                }
            }
        }

        private string GetRoleDisplayName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => role.ToString()
            };
        }

        private string GetStatDisplayName(StatType stat)
        {
            return stat.ToString();
        }

        #endregion

        #region Unit Generation

        private void GenerateUnit(UnitGenerationPanel panel, Team team)
        {
            // Get selected role
            UnitRole role = AllRoles[panel.roleDropdown.value];

            // Generate stats using the new system
            UnitData data = StatGenerator.GenerateStats(role, team);

            // Store and display
            panel.generatedData = data;
            UpdatePanelUI(panel, data);
        }

        #endregion

        #region UI Updates

        private void UpdatePanelUI(UnitGenerationPanel panel, UnitData data)
        {
            // Update stat texts with color coding
            UpdateStatText(panel.healthText, "Health", data.health, data, StatType.Health);
            UpdateStatText(panel.moraleText, "Morale", data.morale, data, StatType.Morale);
            UpdateStatText(panel.buzzText, "Buzz", data.buzz, data, StatType.Buzz);
            UpdateStatText(panel.powerText, "Power", data.power, data, StatType.Power);
            UpdateStatText(panel.aimText, "Aim", data.aim, data, StatType.Aim);
            UpdateStatText(panel.tacticsText, "Tactics", data.tactics, data, StatType.Tactics);
            UpdateStatText(panel.skillText, "Skill", data.skill, data, StatType.Skill);
            UpdateStatText(panel.gritText, "Grit", data.grit, data, StatType.Grit);
            UpdateStatText(panel.hullText, "Hull", data.hull, data, StatType.Hull);
            UpdateStatText(panel.speedText, "Speed", data.speed, data, StatType.Speed);

            // Proficiency is displayed as a multiplier
            if (panel.proficiencyText != null)
            {
                float multiplier = data.proficiency / 100f;
                string suffix = GetStatSuffix(data, StatType.Proficiency);
                panel.proficiencyText.text = $"Proficiency: {multiplier:F2}x{suffix}";
            }

            // Weapon type
            if (panel.weaponText != null)
            {
                panel.weaponText.text = $"Weapon: {data.weaponType}";
            }

            // Primary stat info
            if (panel.primaryStatText != null)
            {
                if (data.hasTwoPrimaryStats)
                {
                    panel.primaryStatText.text = $"Primary: {data.primaryStat}, {data.secondaryPrimaryStat}";
                }
                else
                {
                    panel.primaryStatText.text = $"Primary: {data.primaryStat}";
                }
            }

            // Secondary stat info
            if (panel.secondaryStatText != null)
            {
                panel.secondaryStatText.text = $"Secondary: {data.secondaryStat}";
            }
        }

        private void UpdateStatText(TMP_Text textElement, string statName, int value, UnitData data, StatType statType)
        {
            if (textElement == null) return;

            string suffix = GetStatSuffix(data, statType);
            textElement.text = $"{statName}: {value}{suffix}";
        }

        private string GetStatColorHex(UnitData data, StatType stat)
        {
            // Check if primary
            if (stat == data.primaryStat)
                return ColorUtility.ToHtmlStringRGB(primaryStatColor);
            
            // Check if second primary (Captain)
            if (data.hasTwoPrimaryStats && stat == data.secondaryPrimaryStat)
                return ColorUtility.ToHtmlStringRGB(primaryStatColor);
            
            // Check if secondary
            if (stat == data.secondaryStat)
                return ColorUtility.ToHtmlStringRGB(secondaryStatColor);
            
            // Normal stat
            return ColorUtility.ToHtmlStringRGB(normalStatColor);
        }

        private string GetStatSuffix(UnitData data, StatType stat)
        {
            // Check if primary
            if (stat == data.primaryStat)
                return " (p)";
            
            // Check if second primary (Captain)
            if (data.hasTwoPrimaryStats && stat == data.secondaryPrimaryStat)
                return " (p)";
            
            // Check if secondary
            if (stat == data.secondaryStat)
                return " (s)";
            
            return "";
        }

        #endregion

        #region Game Start

        private void OnStartGameClicked()
        {
            var playerUnits = playerPanels
                .Where(p => p.generatedData != null)
                .Select(p => p.generatedData)
                .ToList();
                
            var enemyUnits = enemyPanels
                .Where(p => p.generatedData != null)
                .Select(p => p.generatedData)
                .ToList();

            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            {
                Debug.LogWarning("Need at least one unit on each team!");
                return;
            }

            if (creationCanvas != null) creationCanvas.SetActive(false);
            if (battleCanvas != null) battleCanvas.SetActive(true);

            if (deploymentManager != null)
            {
                deploymentManager.StartManualDeployment(playerUnits, enemyUnits);
            }
        }

        #endregion
    }
}