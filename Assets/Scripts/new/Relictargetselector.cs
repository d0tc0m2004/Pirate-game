using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Core;
using TMPro;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Handles target selection for relics that need player input.
    /// Used for teleport, ally movement, multi-target effects, etc.
    /// </summary>
    public class RelicTargetSelector : MonoBehaviour
    {
        #region Singleton
        
        private static RelicTargetSelector _instance;
        public static RelicTargetSelector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RelicTargetSelector>();
                    if (_instance == null)
                    {
                        var go = new GameObject("RelicTargetSelector");
                        _instance = go.AddComponent<RelicTargetSelector>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Events
        
        public static event Action<UnitStatus> OnUnitTargetSelected;
        public static event Action<GridCell> OnTileTargetSelected;
        public static event Action OnTargetingCancelled;
        
        #endregion
        
        #region State
        
        public enum SelectionMode
        {
            None,
            SingleAlly,
            SingleEnemy,
            SingleTile,
            MultipleAllies,
            MultipleTiles,
            AllyThenTile    // Select ally, then select destination tile
        }
        
        private SelectionMode currentMode = SelectionMode.None;
        private Action<UnitStatus> onUnitSelected;
        private Action<GridCell> onTileSelected;
        private Action<List<UnitStatus>> onMultipleUnitsSelected;
        private Action<UnitStatus, GridCell> onAllyAndTileSelected;
        private Action onCancelled;
        
        private List<UnitStatus> selectedUnits = new List<UnitStatus>();
        private UnitStatus firstSelectedUnit;
        private int maxSelections = 1;
        private string promptText = "Select a target";
        
        private bool isSelecting = false;
        
        // Highlight tracking
        private List<GridCell> highlightedCells = new List<GridCell>();
        private List<UnitStatus> validTargets = new List<UnitStatus>();
        
        #endregion
        
        #region UI References
        
        [Header("UI References (Auto-created if null)")]
        [SerializeField] private GameObject selectionUI;
        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private Button cancelButton;
        
        #endregion
        
        #region Properties
        
        public bool IsSelecting => isSelecting;
        public SelectionMode CurrentMode => currentMode;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            CreateUIIfNeeded();
            HideUI();
        }
        
        private void Update()
        {
            if (!isSelecting) return;
            
            // Cancel with right-click or Escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
                return;
            }
            
            // Handle click
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }
        
        #endregion
        
        #region Public Selection Methods
        
        /// <summary>
        /// Select a single ally unit.
        /// </summary>
        public void SelectAlly(string prompt, Action<UnitStatus> callback, Action cancelCallback = null)
        {
            currentMode = SelectionMode.SingleAlly;
            promptText = prompt;
            onUnitSelected = callback;
            onCancelled = cancelCallback;
            maxSelections = 1;
            
            StartSelection();
            HighlightAllies();
        }
        
        /// <summary>
        /// Select a single enemy unit.
        /// </summary>
        public void SelectEnemy(string prompt, Action<UnitStatus> callback, Action cancelCallback = null)
        {
            currentMode = SelectionMode.SingleEnemy;
            promptText = prompt;
            onUnitSelected = callback;
            onCancelled = cancelCallback;
            maxSelections = 1;
            
            StartSelection();
            HighlightEnemies();
        }
        
        /// <summary>
        /// Select a single tile.
        /// </summary>
        public void SelectTile(string prompt, Action<GridCell> callback, Action cancelCallback = null, bool onlyEmpty = true)
        {
            currentMode = SelectionMode.SingleTile;
            promptText = prompt;
            onTileSelected = callback;
            onCancelled = cancelCallback;
            
            StartSelection();
            HighlightValidTiles(onlyEmpty);
        }
        
        /// <summary>
        /// Select multiple allies (up to max count).
        /// </summary>
        public void SelectMultipleAllies(string prompt, int maxCount, Action<List<UnitStatus>> callback, Action cancelCallback = null)
        {
            currentMode = SelectionMode.MultipleAllies;
            promptText = prompt;
            onMultipleUnitsSelected = callback;
            onCancelled = cancelCallback;
            maxSelections = maxCount;
            selectedUnits.Clear();
            
            StartSelection();
            HighlightAllies();
        }
        
        /// <summary>
        /// Select an ally, then select a tile for them to move/teleport to.
        /// </summary>
        public void SelectAllyThenTile(string prompt, Action<UnitStatus, GridCell> callback, Action cancelCallback = null)
        {
            currentMode = SelectionMode.AllyThenTile;
            promptText = prompt;
            onAllyAndTileSelected = callback;
            onCancelled = cancelCallback;
            firstSelectedUnit = null;
            
            StartSelection();
            HighlightAllies();
        }
        
        /// <summary>
        /// Cancel current selection.
        /// </summary>
        public void CancelSelection()
        {
            if (!isSelecting) return;
            
            ClearHighlights();
            HideUI();
            
            isSelecting = false;
            currentMode = SelectionMode.None;
            
            onCancelled?.Invoke();
            OnTargetingCancelled?.Invoke();
            
            Debug.Log("Target selection cancelled");
        }
        
        #endregion
        
        #region Selection Logic
        
        private void StartSelection()
        {
            isSelecting = true;
            ShowUI();
            UpdatePrompt();
        }
        
        private void HandleClick()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (!Physics.Raycast(ray, out hit)) return;
            
            // Check for unit click
            var unit = hit.collider.GetComponent<UnitStatus>();
            if (unit == null)
            {
                unit = hit.collider.GetComponentInParent<UnitStatus>();
            }
            
            // Check for tile click
            var cell = hit.collider.GetComponent<GridCell>();
            if (cell == null)
            {
                cell = hit.collider.GetComponentInParent<GridCell>();
            }
            
            switch (currentMode)
            {
                case SelectionMode.SingleAlly:
                    if (unit != null && IsValidAlly(unit))
                    {
                        CompleteUnitSelection(unit);
                    }
                    break;
                    
                case SelectionMode.SingleEnemy:
                    if (unit != null && IsValidEnemy(unit))
                    {
                        CompleteUnitSelection(unit);
                    }
                    break;
                    
                case SelectionMode.SingleTile:
                    if (cell != null && highlightedCells.Contains(cell))
                    {
                        CompleteTileSelection(cell);
                    }
                    break;
                    
                case SelectionMode.MultipleAllies:
                    if (unit != null && IsValidAlly(unit))
                    {
                        HandleMultipleAllySelection(unit);
                    }
                    break;
                    
                case SelectionMode.AllyThenTile:
                    if (firstSelectedUnit == null)
                    {
                        if (unit != null && IsValidAlly(unit))
                        {
                            firstSelectedUnit = unit;
                            promptText = $"Select destination for {unit.UnitName}";
                            UpdatePrompt();
                            ClearHighlights();
                            HighlightValidTiles(true);
                        }
                    }
                    else
                    {
                        if (cell != null && highlightedCells.Contains(cell))
                        {
                            CompleteAllyAndTileSelection(firstSelectedUnit, cell);
                        }
                    }
                    break;
            }
        }
        
        private void CompleteUnitSelection(UnitStatus unit)
        {
            ClearHighlights();
            HideUI();
            isSelecting = false;
            currentMode = SelectionMode.None;
            
            onUnitSelected?.Invoke(unit);
            OnUnitTargetSelected?.Invoke(unit);
            
            Debug.Log($"Selected unit: {unit.UnitName}");
        }
        
        private void CompleteTileSelection(GridCell cell)
        {
            ClearHighlights();
            HideUI();
            isSelecting = false;
            currentMode = SelectionMode.None;
            
            onTileSelected?.Invoke(cell);
            OnTileTargetSelected?.Invoke(cell);
            
            Debug.Log($"Selected tile: ({cell.XPosition}, {cell.YPosition})");
        }
        
        private void HandleMultipleAllySelection(UnitStatus unit)
        {
            if (selectedUnits.Contains(unit))
            {
                selectedUnits.Remove(unit);
            }
            else
            {
                selectedUnits.Add(unit);
            }
            
            UpdatePrompt();
            
            // Check if we've reached max or if player confirms
            if (selectedUnits.Count >= maxSelections)
            {
                CompleteMultipleSelection();
            }
        }
        
        private void CompleteMultipleSelection()
        {
            ClearHighlights();
            HideUI();
            isSelecting = false;
            currentMode = SelectionMode.None;
            
            onMultipleUnitsSelected?.Invoke(new List<UnitStatus>(selectedUnits));
            
            Debug.Log($"Selected {selectedUnits.Count} units");
            selectedUnits.Clear();
        }
        
        private void CompleteAllyAndTileSelection(UnitStatus unit, GridCell cell)
        {
            ClearHighlights();
            HideUI();
            isSelecting = false;
            currentMode = SelectionMode.None;
            firstSelectedUnit = null;
            
            onAllyAndTileSelected?.Invoke(unit, cell);
            
            Debug.Log($"Selected {unit.UnitName} to move to ({cell.XPosition}, {cell.YPosition})");
        }
        
        #endregion
        
        #region Validation
        
        private bool IsValidAlly(UnitStatus unit)
        {
            return unit != null && 
                   unit.Team == Team.Player && 
                   !unit.HasSurrendered &&
                   validTargets.Contains(unit);
        }
        
        private bool IsValidEnemy(UnitStatus unit)
        {
            return unit != null && 
                   unit.Team == Team.Enemy && 
                   !unit.HasSurrendered &&
                   validTargets.Contains(unit);
        }
        
        #endregion
        
        #region Highlighting
        
        private void HighlightAllies()
        {
            validTargets.Clear();
            
            var allies = GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == Team.Player && !u.HasSurrendered)
                .ToList();
            
            foreach (var ally in allies)
            {
                validTargets.Add(ally);
                // Visual highlight
                HighlightUnit(ally, Color.green);
            }
        }
        
        private void HighlightEnemies()
        {
            validTargets.Clear();
            
            var enemies = GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == Team.Enemy && !u.HasSurrendered)
                .ToList();
            
            foreach (var enemy in enemies)
            {
                validTargets.Add(enemy);
                HighlightUnit(enemy, Color.red);
            }
        }
        
        private void HighlightValidTiles(bool onlyEmpty)
        {
            highlightedCells.Clear();
            
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;
                    
                    bool valid = !cell.IsMiddleColumn;
                    if (onlyEmpty)
                    {
                        valid = valid && cell.CanPlaceUnit();
                    }
                    
                    if (valid)
                    {
                        highlightedCells.Add(cell);
                        HighlightCell(cell, Color.cyan);
                    }
                }
            }
        }
        
        private void HighlightUnit(UnitStatus unit, Color color)
        {
            // Add a highlight effect - could be outline, glow, etc.
            var renderer = unit.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Store original color and set highlight
                // For now just log
            }
        }
        
        private void HighlightCell(GridCell cell, Color color)
        {
            var renderer = cell.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Store original and apply highlight
                renderer.material.color = color * 0.5f + renderer.material.color * 0.5f;
            }
        }
        
        private void ClearHighlights()
        {
            // Reset all highlighted cells
            foreach (var cell in highlightedCells)
            {
                if (cell != null)
                {
                    var renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Reset color - ideally store original
                        renderer.material.color = Color.white;
                    }
                }
            }
            
            highlightedCells.Clear();
            validTargets.Clear();
        }
        
        #endregion
        
        #region UI
        
        private void CreateUIIfNeeded()
        {
            if (selectionUI != null) return;
            
            // Create canvas
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // Create selection UI panel
            selectionUI = new GameObject("SelectionUI");
            selectionUI.transform.SetParent(transform, false);
            
            var rt = selectionUI.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -80);
            rt.sizeDelta = new Vector2(400, 80);
            
            var bg = selectionUI.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
            
            // Prompt text
            var textGO = new GameObject("PromptText");
            textGO.transform.SetParent(selectionUI.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0.5f);
            textRT.anchorMax = new Vector2(0.7f, 1f);
            textRT.offsetMin = new Vector2(10, 5);
            textRT.offsetMax = new Vector2(-5, -5);
            
            promptLabel = textGO.AddComponent<TextMeshProUGUI>();
            promptLabel.text = "Select a target";
            promptLabel.fontSize = 16;
            promptLabel.color = Color.white;
            promptLabel.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Cancel button
            var btnGO = new GameObject("CancelButton");
            btnGO.transform.SetParent(selectionUI.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.7f, 0.2f);
            btnRT.anchorMax = new Vector2(0.95f, 0.8f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;
            
            var btnBG = btnGO.AddComponent<Image>();
            btnBG.color = new Color(0.6f, 0.2f, 0.2f);
            
            cancelButton = btnGO.AddComponent<Button>();
            cancelButton.onClick.AddListener(CancelSelection);
            
            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.AddComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;
            
            var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnText.text = "Cancel";
            btnText.fontSize = 14;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;
        }
        
        private void ShowUI()
        {
            if (selectionUI != null)
                selectionUI.SetActive(true);
        }
        
        private void HideUI()
        {
            if (selectionUI != null)
                selectionUI.SetActive(false);
        }
        
        private void UpdatePrompt()
        {
            if (promptLabel == null) return;
            
            string text = promptText;
            
            if (currentMode == SelectionMode.MultipleAllies)
            {
                text += $" ({selectedUnits.Count}/{maxSelections})";
            }
            
            text += "\n<size=12><color=#aaaaaa>Right-click or ESC to cancel</color></size>";
            
            promptLabel.text = text;
        }
        
        #endregion
    }
}