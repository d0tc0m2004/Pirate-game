using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Entry for tracking the test status of a single relic effect variant.
    /// </summary>
    [System.Serializable]
    public class RelicTestEntry
    {
        [HideInInspector] public string relicName;
        [HideInInspector] public RelicCategory category;
        [HideInInspector] public UnitRole roleTag;
        [HideInInspector] public bool isVariant2;
        [HideInInspector] public RelicEffectType effectType;

        [Tooltip("Test status for this relic variant")]
        public string status;

        [TextArea(1, 3)]
        [Tooltip("Notes or comments about this relic")]
        public string comment;
    }

    /// <summary>
    /// Groups V1 and V2 entries for a single relic (category + role).
    /// </summary>
    [System.Serializable]
    public class RelicTestPair
    {
        [HideInInspector] public string displayName;
        public RelicTestEntry v1;
        public RelicTestEntry v2;
    }

    /// <summary>
    /// ScriptableObject for tracking relic testing progress.
    /// Shows all relics grouped by category+role, with V1 and V2 entries.
    /// Each entry has editable status and comment fields.
    ///
    /// Create via: Assets > Create > Tactical > Testing > Relic Test Tracker
    /// Use the context menu "Populate All Relics" to fill entries from RelicEffectsDatabase.
    /// </summary>
    [CreateAssetMenu(fileName = "RelicTestTracker", menuName = "Tactical/Testing/Relic Test Tracker")]
    public class RelicTestTracker : ScriptableObject
    {
        [Header("Filter (optional)")]
        [Tooltip("Filter relics by category. Set to Weapon to show all.")]
        public RelicCategory filterCategory;

        [Tooltip("Filter relics by role.")]
        public UnitRole filterRole;

        [Header("All Relic Test Entries")]
        public List<RelicTestPair> relicTests = new List<RelicTestPair>();

        /// <summary>
        /// Populate the tracker with all relics from RelicEffectsDatabase.
        /// Call via context menu in Inspector.
        /// </summary>
        [ContextMenu("Populate All Relics")]
        public void PopulateFromDatabase()
        {
            relicTests.Clear();

            var db = RelicEffectsDatabase.Instance;
            if (db == null || db.allEffects == null)
            {
                Debug.LogError("RelicEffectsDatabase not found or empty!");
                return;
            }

            // Group effects by category + role
            var grouped = db.allEffects
                .GroupBy(e => new { e.category, e.roleTag })
                .OrderBy(g => g.Key.category)
                .ThenBy(g => g.Key.roleTag);

            foreach (var group in grouped)
            {
                var v1Effect = group.FirstOrDefault(e => !e.isVariant2);
                var v2Effect = group.FirstOrDefault(e => e.isVariant2);

                var pair = new RelicTestPair
                {
                    displayName = $"{group.Key.category} - {group.Key.roleTag}",
                    v1 = CreateEntry(v1Effect, group.Key.category, group.Key.roleTag, false),
                    v2 = CreateEntry(v2Effect, group.Key.category, group.Key.roleTag, true)
                };

                relicTests.Add(pair);
            }

            Debug.Log($"<color=green>RelicTestTracker: Populated {relicTests.Count} relic pairs ({relicTests.Count * 2} total entries)</color>");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private RelicTestEntry CreateEntry(RelicEffectData effectData, RelicCategory category, UnitRole role, bool isV2)
        {
            var entry = new RelicTestEntry
            {
                category = category,
                roleTag = role,
                isVariant2 = isV2,
                status = "",
                comment = ""
            };

            if (effectData != null)
            {
                entry.relicName = effectData.GetDisplayName();
                entry.effectType = effectData.effectType;
            }
            else
            {
                string variant = isV2 ? " V2" : "";
                entry.relicName = $"{role} {category}{variant} (missing)";
                entry.effectType = RelicEffectType.None;
            }

            return entry;
        }

        /// <summary>
        /// Get completion summary.
        /// </summary>
        [ContextMenu("Print Summary")]
        public void PrintSummary()
        {
            int total = relicTests.Count * 2;
            int withStatus = relicTests.Sum(p =>
                (string.IsNullOrEmpty(p.v1?.status) ? 0 : 1) +
                (string.IsNullOrEmpty(p.v2?.status) ? 0 : 1));

            Debug.Log($"<color=cyan>RelicTestTracker: {withStatus}/{total} relics have status entries</color>");
        }
    }
}
