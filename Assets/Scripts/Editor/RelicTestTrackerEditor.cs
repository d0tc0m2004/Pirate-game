using UnityEngine;
using UnityEditor;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using System.Linq;

namespace TacticalGame.EditorTools
{
    [CustomEditor(typeof(RelicTestTracker))]
    public class RelicTestTrackerEditor : UnityEditor.Editor
    {
        private RelicCategory selectedCategory = RelicCategory.Boots;
        private bool filterByCategory = false;
        private Vector2 scrollPos;

        public override void OnInspectorGUI()
        {
            var tracker = (RelicTestTracker)target;

            EditorGUILayout.Space(5);

            // Populate button
            if (GUILayout.Button("Populate All Relics from Database", GUILayout.Height(30)))
            {
                tracker.PopulateFromDatabase();
            }

            if (GUILayout.Button("Print Summary"))
            {
                tracker.PrintSummary();
            }

            EditorGUILayout.Space(10);

            // Category filter dropdown
            EditorGUILayout.BeginHorizontal();
            filterByCategory = EditorGUILayout.Toggle("Filter by Category", filterByCategory);
            if (filterByCategory)
            {
                selectedCategory = (RelicCategory)EditorGUILayout.EnumPopup(selectedCategory);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (tracker.relicTests == null || tracker.relicTests.Count == 0)
            {
                EditorGUILayout.HelpBox("No relic entries. Click 'Populate All Relics from Database' to fill.", MessageType.Info);
                return;
            }

            // Summary
            int total = tracker.relicTests.Count * 2;
            int withStatus = tracker.relicTests.Sum(p =>
                (string.IsNullOrEmpty(p.v1?.status) ? 0 : 1) +
                (string.IsNullOrEmpty(p.v2?.status) ? 0 : 1));
            EditorGUILayout.LabelField($"Progress: {withStatus} / {total} relics have status", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // Draw entries
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            RelicCategory? lastCategory = null;

            for (int i = 0; i < tracker.relicTests.Count; i++)
            {
                var pair = tracker.relicTests[i];
                if (pair.v1 == null || pair.v2 == null) continue;

                // Apply filter
                if (filterByCategory && pair.v1.category != selectedCategory)
                    continue;

                // Category header
                if (lastCategory != pair.v1.category)
                {
                    lastCategory = pair.v1.category;
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"--- {pair.v1.category} ---", EditorStyles.boldLabel);
                }

                // Pair box
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(pair.displayName, EditorStyles.boldLabel);

                // V1
                DrawRelicEntry(pair.v1, "V1");

                EditorGUILayout.Space(3);

                // V2
                DrawRelicEntry(pair.v2, "V2");

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(tracker);
            }
        }

        private void DrawRelicEntry(RelicTestEntry entry, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header: label + relic name + effect type
            EditorGUILayout.LabelField($"  [{label}] {entry.relicName}", EditorStyles.miniLabel);
            if (entry.effectType != RelicEffectType.None)
            {
                EditorGUILayout.LabelField($"    Effect: {entry.effectType}", EditorStyles.miniLabel);
            }

            // Status and Comment fields
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
            entry.status = EditorGUILayout.TextField(entry.status);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Comment:", GUILayout.Width(60));
            entry.comment = EditorGUILayout.TextArea(entry.comment, GUILayout.MinHeight(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
