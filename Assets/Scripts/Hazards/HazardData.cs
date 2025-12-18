using UnityEngine;

namespace TacticalGame.Hazards
{
    /// <summary>
    /// Defines the shape pattern of a hazard when spawned.
    /// </summary>
    public enum HazardShape
    {
        Single,
        Row,
        Column,
        Square,
        Plus
    }

    /// <summary>
    /// Defines the effect type of a hazard.
    /// </summary>
    public enum HazardEffectType
    {
        None,
        Fire,
        Trap,
        Plague,
        ShiftingSand,
        Lightning,
        Cursed,
        Boulder,
        Box
    }

    /// <summary>
    /// ScriptableObject defining hazard properties.
    /// </summary>
    [CreateAssetMenu(fileName = "New Hazard", menuName = "Tactical/Hazard")]
    public class HazardData : ScriptableObject
    {
        [Header("Visuals")]
        public string hazardName;
        public GameObject hazardPrefab;

        [Header("Properties")]
        public bool isBlocking;
        public bool isDestructible;
        public int maxHealth = 2;
        public bool causesDisplacement;

        [Header("Shape")]
        public HazardShape shapePattern;

        [Header("Effect Logic")]
        public HazardEffectType effectType;

        [Header("Stats (Turn End/Enter)")]
        public int damageHP;
        public int damageMorale;
        public int effectDuration;
        public float curseMultiplier = 1.0f;

        [Header("Attack Bonuses (When Standing On)")]
        [Tooltip("Extra HP damage added to attacks if you stand on this.")]
        public int standingBonusHP = 0;

        [Tooltip("Extra Morale damage added to attacks if you stand on this.")]
        public int standingBonusMorale = 0;

        [Tooltip("If true, attacking while standing on this applies Curse to the target.")]
        public bool standingAppliesCurse = false;

        [Header("Loot")]
        public GameObject dropItem;
    }
}