using UnityEngine;
using UnityEditor;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using System.Collections.Generic;
using System.Linq;

namespace TacticalGame.EditorTools
{
    [CustomEditor(typeof(RelicTestTracker))]
    public class RelicTestTrackerEditor : UnityEditor.Editor
    {
        private RelicCategory selectedCategory = RelicCategory.Boots;
        private bool filterByCategory = false;
        private string searchQuery = "";
        private Vector2 scrollPos;

        // Cached styles
        private GUIStyle searchBarStyle;
        private GUIStyle summaryStyle;
        private GUIStyle categoryHeaderStyle;
        private GUIStyle richLabelStyle;
        private bool stylesReady = false;

        private void InitStyles()
        {
            if (stylesReady) return;

            searchBarStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fontSize = 13,
                fixedHeight = 24
            };

            summaryStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                richText = true,
                padding = new RectOffset(10, 10, 8, 8)
            };

            categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };

            richLabelStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            stylesReady = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();

            var tracker = (RelicTestTracker)target;
            serializedObject.Update();

            EditorGUILayout.Space(5);

            // === HEADER ===
            EditorGUILayout.LabelField("⚓ Relic Test Tracker", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            });

            EditorGUILayout.Space(4);

            // === SUMMARY ===
            DrawSummary(tracker);

            EditorGUILayout.Space(6);

            // === SEARCH BAR ===
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search", GUILayout.Width(48));
            string newSearch = EditorGUILayout.TextField(searchQuery, searchBarStyle);
            if (newSearch != searchQuery)
            {
                searchQuery = newSearch;
            }
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(22)))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                EditorGUILayout.LabelField(
                    $"  Tip: multi-word search (e.g. \"captain boots\") matches entries containing ALL words",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            // === CATEGORY FILTER ===
            EditorGUILayout.BeginHorizontal();
            filterByCategory = EditorGUILayout.Toggle("Filter by Category", filterByCategory);
            if (filterByCategory)
            {
                selectedCategory = (RelicCategory)EditorGUILayout.EnumPopup(selectedCategory);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // === BUTTONS ROW ===
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate All Relics from Database", GUILayout.Height(26)))
            {
                tracker.PopulateFromDatabase();
            }
            if (GUILayout.Button("Print Summary", GUILayout.Height(26)))
            {
                tracker.PrintSummary();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // === ENTRIES ===
            if (tracker.relicTests == null || tracker.relicTests.Count == 0)
            {
                EditorGUILayout.HelpBox("No relic entries. Click 'Populate All Relics from Database' to fill.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Get filtered indices
            List<int> matchingIndices = GetFilteredIndices(tracker);

            EditorGUILayout.LabelField($"Showing {matchingIndices.Count} / {tracker.relicTests.Count} pairs", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            // Draw entries
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            RelicCategory? lastCategory = null;

            foreach (int i in matchingIndices)
            {
                var pair = tracker.relicTests[i];
                if (pair.v1 == null || pair.v2 == null) continue;

                // Category header
                if (lastCategory != pair.v1.category)
                {
                    lastCategory = pair.v1.category;
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"━━━ {pair.v1.category} ━━━", categoryHeaderStyle);
                }

                // Pair box
                EditorGUILayout.BeginVertical("box");

                // Header with status dots
                string v1Dot = GetStatusDot(pair.v1?.status);
                string v2Dot = GetStatusDot(pair.v2?.status);
                EditorGUILayout.LabelField($"{v1Dot}{v2Dot} {pair.displayName}", EditorStyles.boldLabel);

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

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Get indices of relic pairs matching search query + category filter.
        /// Multi-word search: "captain boots" matches entries containing BOTH words.
        /// </summary>
        private List<int> GetFilteredIndices(RelicTestTracker tracker)
        {
            var indices = new List<int>();
            string query = searchQuery?.Trim().ToLowerInvariant() ?? "";
            string[] queryWords = string.IsNullOrEmpty(query)
                ? new string[0]
                : query.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tracker.relicTests.Count; i++)
            {
                var pair = tracker.relicTests[i];
                if (pair.v1 == null || pair.v2 == null) continue;

                // Category filter
                if (filterByCategory && pair.v1.category != selectedCategory)
                    continue;

                // Search filter
                if (queryWords.Length > 0)
                {
                    string searchable = BuildSearchableText(pair);
                    bool allMatch = queryWords.All(w => searchable.Contains(w));
                    if (!allMatch) continue;
                }

                indices.Add(i);
            }

            return indices;
        }

        /// <summary>
        /// Build a single searchable string from all fields in a pair.
        /// </summary>
        private string BuildSearchableText(RelicTestPair pair)
        {
            var parts = new List<string>();

            if (pair.displayName != null) parts.Add(pair.displayName);

            void AddEntry(RelicTestEntry e)
            {
                if (e == null) return;
                if (e.relicName != null) parts.Add(e.relicName);
                parts.Add(e.category.ToString());
                parts.Add(e.roleTag.ToString());
                parts.Add(e.effectType.ToString());
                if (e.status != null) parts.Add(e.status);
                if (e.comment != null) parts.Add(e.comment);
            }

            AddEntry(pair.v1);
            AddEntry(pair.v2);

            return string.Join(" ", parts).ToLowerInvariant();
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

        /// <summary>
        /// Status dot based on status text content.
        /// </summary>
        private string GetStatusDot(string status)
        {
            if (string.IsNullOrEmpty(status)) return "[ ] ";

            string lower = status.ToLowerInvariant().Trim();

            if (lower.Contains("pass") || lower.Contains("done") || lower.Contains("ok"))
                return "[+] ";
            if (lower.Contains("fail") || lower.Contains("bug") || lower.Contains("broken"))
                return "[X] ";
            if (lower.Contains("wip") || lower.Contains("partial") || lower.Contains("progress"))
                return "[~] ";
            if (lower.Contains("skip") || lower.Contains("n/a"))
                return "[-] ";

            return "[?] "; // Has status but unrecognized
        }

        /// <summary>
        /// Draw completion summary at the top.
        /// </summary>
        private void DrawSummary(RelicTestTracker tracker)
        {
            if (tracker.relicTests == null || tracker.relicTests.Count == 0) return;

            int totalEntries = tracker.relicTests.Count * 2;
            int withStatus = 0;
            int passed = 0;
            int failed = 0;
            int wip = 0;

            foreach (var pair in tracker.relicTests)
            {
                CountStatus(pair.v1?.status, ref withStatus, ref passed, ref failed, ref wip);
                CountStatus(pair.v2?.status, ref withStatus, ref passed, ref failed, ref wip);
            }

            int untested = totalEntries - withStatus;
            float pct = totalEntries > 0 ? (float)withStatus / totalEntries * 100f : 0f;

            string summary = $"<b>Progress:</b> {withStatus}/{totalEntries} ({pct:F0}%)  |  " +
                             $"Pass: {passed}  |  Fail: {failed}  |  WIP: {wip}  |  Untested: {untested}";

            EditorGUILayout.LabelField(summary, summaryStyle);
        }

        private void CountStatus(string status, ref int total, ref int passed, ref int failed, ref int wip)
        {
            if (string.IsNullOrEmpty(status)) return;

            total++;
            string lower = status.ToLowerInvariant().Trim();

            if (lower.Contains("pass") || lower.Contains("done") || lower.Contains("ok"))
                passed++;
            else if (lower.Contains("fail") || lower.Contains("bug") || lower.Contains("broken"))
                failed++;
            else if (lower.Contains("wip") || lower.Contains("partial") || lower.Contains("progress"))
                wip++;
        }
    }
}
