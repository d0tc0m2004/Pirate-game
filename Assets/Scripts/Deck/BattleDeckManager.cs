using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Managers;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Manages the shared battle deck for the player's team.
    /// All active relic cards from all units are combined into one deck.
    /// </summary>
    public class BattleDeckManager : MonoBehaviour
    {
        #region Singleton
        
        private static BattleDeckManager _instance;
        public static BattleDeckManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BattleDeckManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("BattleDeckManager");
                        _instance = go.AddComponent<BattleDeckManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Events
        
        public delegate void DeckEvent();
        public delegate void CardEvent(BattleCard card);
        public delegate void HandEvent(List<BattleCard> hand);
        
        public static event DeckEvent OnDeckBuilt;
        public static event DeckEvent OnDeckShuffled;
        public static event DeckEvent OnDeckReset;
        public static event CardEvent OnCardDrawn;
        public static event CardEvent OnCardPlayed;
        public static event CardEvent OnCardDiscarded;
        public static event CardEvent OnCardStowed;
        public static event HandEvent OnHandChanged;
        public static event DeckEvent OnTurnStartDraw;
        public static event DeckEvent OnTurnEndDiscard;
        
        #endregion
        
        #region Settings
        
        [Header("Settings")]
        [SerializeField] private int handSize = 5;
        [SerializeField] private int stowCost = 1;
        [SerializeField] private int discardDrawCost = 1;
        
        #endregion
        
        #region State
        
        [Header("Deck State")]
        [SerializeField] private List<BattleCard> deck = new List<BattleCard>();
        [SerializeField] private List<BattleCard> hand = new List<BattleCard>();
        [SerializeField] private List<BattleCard> discardPile = new List<BattleCard>();
        
        [Header("Passive Tracking")]
        [SerializeField] private List<EquippedRelic> allPassiveRelics = new List<EquippedRelic>();
        
        [Header("Current Selection")]
        [SerializeField] private UnitStatus selectedUnit;
        [SerializeField] private BattleCard selectedCard;
        
        private bool isInitialized = false;
        
        #endregion
        
        #region Properties
        
        public IReadOnlyList<BattleCard> Deck => deck;
        public IReadOnlyList<BattleCard> Hand => hand;
        public IReadOnlyList<BattleCard> DiscardPile => discardPile;
        public IReadOnlyList<EquippedRelic> PassiveRelics => allPassiveRelics;
        
        public int DeckCount => deck.Count;
        public int HandCount => hand.Count;
        public int DiscardCount => discardPile.Count;
        public int TotalCards => deck.Count + hand.Count + discardPile.Count;
        
        public UnitStatus SelectedUnit => selectedUnit;
        public BattleCard SelectedCard => selectedCard;
        
        public int HandSize => handSize;
        public int StowCost => stowCost;
        public int DiscardDrawCost => discardDrawCost;
        
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
        }
        
        private void OnEnable()
        {
            GameEvents.OnPlayerTurnStart += OnPlayerTurnStart;
            GameEvents.OnPlayerTurnEnd += OnPlayerTurnEnd;
            GameEvents.OnUnitSelected += OnUnitSelected;
            GameEvents.OnUnitDeselected += OnUnitDeselected;
        }
        
        private void OnDisable()
        {
            GameEvents.OnPlayerTurnStart -= OnPlayerTurnStart;
            GameEvents.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            GameEvents.OnUnitSelected -= OnUnitSelected;
            GameEvents.OnUnitDeselected -= OnUnitDeselected;
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Build the shared deck from all player units' equipped relics.
        /// Call this at the start of battle after all units are deployed.
        /// </summary>
        public void BuildDeck(List<UnitStatus> playerUnits)
        {
            deck.Clear();
            hand.Clear();
            discardPile.Clear();
            allPassiveRelics.Clear();

            Debug.Log($"<color=cyan>=== Building Shared Battle Deck ===</color>");

            foreach (var unit in playerUnits)
            {
                if (unit == null || unit.HasSurrendered) continue;

                Debug.Log($"<color=green>Adding cards from {unit.UnitName}:</color>");

                // Try FlexibleUnitEquipment first (new slot-based system)
                var flexEquip = unit.GetComponent<FlexibleUnitEquipment>();
                if (flexEquip != null)
                {
                    AddCardsFromFlexibleEquipment(flexEquip, unit);
                    continue;
                }

                // Fall back to UnitEquipmentUpdated (old system)
                var equipment = unit.GetComponent<UnitEquipmentUpdated>();
                if (equipment == null)
                {
                    Debug.LogWarning($"{unit.UnitName}: No equipment component found");
                    continue;
                }

                // Add weapon relic cards
                AddWeaponCards(equipment.WeaponRelic, unit);

                // Add category relic cards (active only)
                AddRelicCards(equipment.BootsRelic, unit);
                AddRelicCards(equipment.GlovesRelic, unit);
                AddRelicCards(equipment.HatRelic, unit);
                AddRelicCards(equipment.CoatRelic, unit);
                AddRelicCards(equipment.TotemRelic, unit);
                AddRelicCards(equipment.UltimateRelic, unit);

                // Track passive relics (Trinket + PassiveUnique)
                AddPassiveRelic(equipment.TrinketRelic);
                AddPassiveRelic(equipment.PassiveUniqueRelic);
            }

            Debug.Log($"<color=cyan>Deck built: {deck.Count} active cards, {allPassiveRelics.Count} passives</color>");

            // Shuffle the deck
            ShuffleDeck();

            isInitialized = true;
            OnDeckBuilt?.Invoke();
        }

        /// <summary>
        /// Add cards from FlexibleUnitEquipment (new slot-based system).
        /// </summary>
        private void AddCardsFromFlexibleEquipment(FlexibleUnitEquipment flexEquip, UnitStatus owner)
        {
            // Iterate through all slots
            for (int i = 0; i < FlexibleUnitEquipment.SLOT_COUNT; i++)
            {
                var slot = flexEquip.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;

                if (slot.hasWeapon && slot.weaponRelic != null)
                {
                    // Add weapon cards
                    AddWeaponCards(slot.weaponRelic, owner);
                }
                else if (slot.categoryRelic != null)
                {
                    // Add category relic cards or track as passive
                    if (slot.categoryRelic.IsPassive())
                    {
                        AddPassiveRelic(slot.categoryRelic);
                    }
                    else
                    {
                        AddRelicCards(slot.categoryRelic, owner);
                    }
                }
            }
        }

        /// <summary>
        /// Called ONLY when the player confirms a target, or an instant card begins its effect.
        /// </summary>
        public void ConsumeCard(BattleCard card)
        {
            if (card == null || !hand.Contains(card)) return; // Safety check

            // 1. Spend the Energy
            var energyManager = ServiceLocator.Get<EnergyManager>();
            energyManager.TrySpendEnergy(card.energyCost);

            // 2. Discard the Card
            hand.Remove(card);
            card.isStowed = false;
            discardPile.Add(card);
            selectedCard = null;

            // 3. Update the UI so the card visually vanishes!
            OnCardPlayed?.Invoke(card);
            OnHandChanged?.Invoke(hand);
        }
        
        /// <summary>
        /// Auto-detect and build deck from all player units in scene.
        /// </summary>
        public void BuildDeckFromScene()
        {
            var playerUnits = GameObject.FindGameObjectsWithTag("Unit")
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == Team.Player && !u.HasSurrendered)
                .ToList();
            
            BuildDeck(playerUnits);
        }
        
        private void AddWeaponCards(WeaponRelic relic, UnitStatus owner)
        {
            if (relic == null) return;
            
            int copies = relic.baseWeaponData?.cardCopies ?? 2;
            
            for (int i = 0; i < copies; i++)
            {
                var card = BattleCard.FromWeaponRelic(relic, owner, i);
                deck.Add(card);
            }
            
            Debug.Log($"  + {copies}x {relic.relicName} (Weapon)");
        }
        
        private void AddRelicCards(EquippedRelic relic, UnitStatus owner)
        {
            if (relic == null) return;

            // Skip relics with default/invalid category (Weapon = 0 is the default for uninitialized relics)
            // Weapon relics should go through AddWeaponCards, not here
            if (relic.category == RelicCategory.Weapon)
            {
                Debug.Log($"  ~ Skipping relic with Weapon category (use AddWeaponCards for weapons)");
                return;
            }

            // Skip relics with empty/default name (uninitialized)
            if (string.IsNullOrEmpty(relic.relicName) && relic.effectData == null)
            {
                Debug.Log($"  ~ Skipping uninitialized relic ({relic.category})");
                return;
            }

            // Skip passive relics - they don't go in deck
            if (relic.IsPassive())
            {
                Debug.Log($"  ~ {relic.relicName} (Passive - not in deck)");
                return;
            }

            int copies = relic.GetCopies();
            if (copies <= 0) copies = 2; // Default

            for (int i = 0; i < copies; i++)
            {
                var card = BattleCard.FromRelic(relic, owner, i);
                deck.Add(card);
            }

            Debug.Log($"  + {copies}x {relic.relicName}");
        }
        
        private void AddPassiveRelic(EquippedRelic relic)
        {
            if (relic == null) return;
            
            if (relic.IsPassive())
            {
                allPassiveRelics.Add(relic);
                Debug.Log($"  * {relic.relicName} (Passive)");
            }
        }
        
        #endregion
        
        #region Deck Operations
        
        /// <summary>
        /// Shuffle the deck.
        /// </summary>
        public void ShuffleDeck()
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
            
            OnDeckShuffled?.Invoke();
            Debug.Log("<color=magenta>Deck shuffled</color>");
        }
        
        /// <summary>
        /// Reset deck - move all discard pile cards back to deck and shuffle.
        /// </summary>
        public void ResetDeck()
        {
            Debug.Log($"<color=magenta>Deck Reset! {discardPile.Count} cards returning to deck</color>");
            
            deck.AddRange(discardPile);
            discardPile.Clear();
            
            ShuffleDeck();
            
            OnDeckReset?.Invoke();
        }
        
        /// <summary>
        /// Draw cards at start of turn. Always draws handSize cards regardless of stowed cards.
        /// </summary>
        public void DrawToFillHand()
        {
            // Count stowed cards before un-stowing
            int stowedCount = hand.Count(c => c.isStowed);

            // Un-stow all cards at start of turn
            foreach (var card in hand)
            {
                card.isStowed = false;
            }

            // Always draw handSize new cards (stowed cards + new cards = total hand)
            int toDraw = handSize;

            Debug.Log($"<color=cyan>Drawing {toDraw} new cards (had {stowedCount} stowed, total hand will be {hand.Count + toDraw})</color>");

            for (int i = 0; i < toDraw; i++)
            {
                DrawOneCard();
            }

            OnHandChanged?.Invoke(hand);
            OnTurnStartDraw?.Invoke();
        }
        
        /// <summary>
        /// Draw a single card from deck to hand.
        /// </summary>
        public bool DrawOneCard()
        {
            if (deck.Count == 0)
            {
                if (discardPile.Count > 0)
                {
                    ResetDeck();
                }
                else
                {
                    Debug.Log("No cards to draw!");
                    return false;
                }
            }
            
            if (deck.Count == 0) return false;
            
            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
            
            OnCardDrawn?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            
            Debug.Log($"<color=green>Drew: {card.GetDisplayName()} ({card.GetOwnerName()})</color>");
            
            return true;
        }
        
        /// <summary>
        /// Draw a specific card from deck to hand (for effects that draw specific card types).
        /// </summary>
        public bool DrawSpecificCard(BattleCard card)
        {
            if (card == null || !deck.Contains(card))
            {
                return false;
            }
            
            deck.Remove(card);
            hand.Add(card);
            
            OnCardDrawn?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            
            Debug.Log($"<color=green>Drew specific: {card.GetDisplayName()}</color>");
            
            return true;
        }
        
        /// <summary>
        /// Find and draw a card of specific category belonging to a unit.
        /// </summary>
        public bool DrawCardByCategory(UnitStatus unit, RelicCategory category)
        {
            var card = deck.FirstOrDefault(c => 
                c.category == category && c.BelongsTo(unit));
            
            if (card != null)
            {
                return DrawSpecificCard(card);
            }
            
            // Also check discard pile
            card = discardPile.FirstOrDefault(c => 
                c.category == category && c.BelongsTo(unit));
            
            if (card != null)
            {
                discardPile.Remove(card);
                hand.Add(card);
                OnCardDrawn?.Invoke(card);
                OnHandChanged?.Invoke(hand);
                Debug.Log($"<color=green>Drew from discard: {card.GetDisplayName()}</color>");
                return true;
            }
            
            Debug.Log($"{unit.UnitName} has no {category} card available");
            return false;
        }
        
        /// <summary>
        /// Discard all non-stowed cards from hand at end of turn.
        /// </summary>
        public void DiscardNonStowedCards()
        {
            // Revert any temporary cost reductions before discarding
            foreach (var card in hand)
            {
                if (card.originalEnergyCost >= 0)
                {
                    card.energyCost = card.originalEnergyCost;
                    card.originalEnergyCost = -1;
                }
            }

            var toDiscard = hand.Where(c => !c.isStowed).ToList();
            
            foreach (var card in toDiscard)
            {
                hand.Remove(card);
                discardPile.Add(card);
                OnCardDiscarded?.Invoke(card);
            }
            
            Debug.Log($"<color=yellow>Discarded {toDiscard.Count} cards, {hand.Count} stowed remain</color>");
            
            OnHandChanged?.Invoke(hand);
            OnTurnEndDiscard?.Invoke();
        }
        
        #endregion
        
        #region Card Actions
        
        /// <summary>
        /// Select a card from hand.
        /// </summary>
        public void SelectCard(BattleCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand!");
                return;
            }
            
            selectedCard = card;
            Debug.Log($"Selected card: {card.GetDisplayName()}");
        }
        
        /// <summary>
        /// Deselect current card.
        /// </summary>
        public void DeselectCard()
        {
            selectedCard = null;
        }
        
        /// <summary>
        /// Validates if a card CAN be played and starts the targeting/execution phase.
        /// </summary>
        public bool PlayCard(BattleCard card, UnitStatus target = null, GridCell targetCell = null)
        {
            if (card == null || !hand.Contains(card)) return false;
            
            // Validate ownership
            if (selectedUnit != card.ownerUnit) return false;
            
            // Validate energy (Check only, DO NOT SPEND YET)
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (!energyManager.HasEnergy(card.energyCost)) return false;

            // Start the execution/targeting phase
            ExecuteCard(card, target, targetCell); 
            
            // THE SMART CHECK: 
            // If the RelicTargetSelector is NOT active, this was an instant card. Consume it!
            if (!RelicTargetSelector.Instance.IsSelecting)
            {
                ConsumeCard(card);
            }
            
            return true;
        }
        
        /// <summary>
        /// Play currently selected card.
        /// </summary>
        public bool PlaySelectedCard(UnitStatus target = null, GridCell targetCell = null)
        {
            if (selectedCard == null)
            {
                Debug.LogWarning("No card selected!");
                return false;
            }
            
            return PlayCard(selectedCard, target, targetCell);
        }
        
        /// <summary>
        /// Finish a card after multi-step movement (energy already spent, just discard).
        /// </summary>
        public void FinishCardAfterMove(BattleCard card)
        {
            if (!hand.Contains(card)) return;

            hand.Remove(card);
            card.isStowed = false;
            discardPile.Add(card);
            selectedCard = null;

            OnCardPlayed?.Invoke(card);
            OnHandChanged?.Invoke(hand);

            Debug.Log($"<color=yellow>Played (move): {card.GetDisplayName()} (Hand: {hand.Count}, Discard: {discardPile.Count})</color>");
        }

        private void ExecuteCard(BattleCard card, UnitStatus target, GridCell targetCell)
        {
            if (card.IsWeaponCard)
            {
                ExecuteWeaponCard(card, target);
            }
            else
            {
                RelicEffectExecutor.Execute(card.sourceRelic, card.ownerUnit, target, targetCell, card);
            }
        }
        
        /// <summary>
        /// Refund a card and its energy cost if a multi-step async targeter was cancelled.
        /// </summary>
        public void RefundCard(BattleCard card)
        {
            if (discardPile.Contains(card))
            {
                discardPile.Remove(card);
                hand.Add(card);
                
                var energyManager = ServiceLocator.Get<EnergyManager>();
                if (energyManager != null)
                {
                    // Spending negative energy refunds it
                    energyManager.TrySpendEnergy(-card.energyCost);
                }
                
                selectedCard = null;
                OnHandChanged?.Invoke(hand);
                
                Debug.Log($"<color=orange>Refunded card: {card.GetDisplayName()}</color>");
            }
        }

        private void ExecuteWeaponCard(BattleCard card, UnitStatus target)
        {
            if (target == null)
            {
                // Auto-target closest enemy
                var enemies = GameObject.FindGameObjectsWithTag("Unit")
                    .Select(go => go.GetComponent<UnitStatus>())
                    .Where(u => u != null && u.Team != card.ownerUnit.Team && !u.HasSurrendered)
                    .ToList();
                
                if (enemies.Count > 0)
                {
                    target = enemies.OrderBy(e => 
                        Vector3.Distance(card.ownerUnit.transform.position, e.transform.position))
                        .First();
                }
            }
            
            if (target == null)
            {
                Debug.Log("No valid target for weapon attack");
                return;
            }
            
            var attack = card.ownerUnit.GetComponent<UnitAttack>();
            if (attack != null)
            {
                // Use ExecuteCardAttack which determines melee/ranged from the CARD's weapon relic,
                // not the unit's default weapon type. This allows units to use weapon relics
                // that differ from their default weapon family.
                attack.ExecuteCardAttack(card.sourceWeaponRelic);
            }
        }
        
        /// <summary>
        /// Stow a card (costs energy, prevents discard at end of turn).
        /// </summary>
        public bool StowCard(BattleCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand!");
                return false;
            }
            
            if (card.isStowed)
            {
                Debug.Log("Card already stowed!");
                return false;
            }
            
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (!energyManager.HasEnergy(stowCost))
            {
                Debug.Log("Not enough energy to stow!");
                return false;
            }
            
            energyManager.TrySpendEnergy(stowCost);
            card.isStowed = true;
            
            OnCardStowed?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            
            Debug.Log($"<color=blue>Stowed: {card.GetDisplayName()}</color>");
            
            return true;
        }
        
        /// <summary>
        /// Discard a card and draw a new one (costs energy).
        /// </summary>
        public bool DiscardAndDraw(BattleCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand!");
                return false;
            }
            
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (!energyManager.HasEnergy(discardDrawCost))
            {
                Debug.Log("Not enough energy to discard!");
                return false;
            }
            
            energyManager.TrySpendEnergy(discardDrawCost);
            
            // Discard
            hand.Remove(card);
            card.isStowed = false;
            discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
            
            // Draw
            DrawOneCard();
            
            Debug.Log($"<color=orange>Discarded {card.GetDisplayName()} and drew new card</color>");
            
            return true;
        }
        
        /// <summary>
        /// Force discard a card without energy cost (for enemy effects).
        /// </summary>
        public bool ForceDiscardCard(BattleCard card)
        {
            if (!hand.Contains(card))
            {
                return false;
            }
            
            hand.Remove(card);
            card.isStowed = false;
            discardPile.Add(card);
            
            OnCardDiscarded?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            
            Debug.Log($"<color=red>Forced discard: {card.GetDisplayName()}</color>");
            
            return true;
        }
        
        /// <summary>
        /// Force discard random cards from a specific unit.
        /// </summary>
        public int ForceDiscardFromUnit(UnitStatus unit, int count)
        {
            var unitCards = hand.Where(c => c.BelongsTo(unit)).ToList();
            int discarded = 0;
            
            for (int i = 0; i < count && unitCards.Count > 0; i++)
            {
                var card = unitCards[Random.Range(0, unitCards.Count)];
                if (ForceDiscardCard(card))
                {
                    unitCards.Remove(card);
                    discarded++;
                }
            }
            
            return discarded;
        }
        
        #endregion
        
        #region Turn Events
        
        private void OnPlayerTurnStart()
        {
            if (!isInitialized) return;
            
            DrawToFillHand();
        }
        
        private void OnPlayerTurnEnd()
        {
            if (!isInitialized) return;
            
            DiscardNonStowedCards();
        }
        
        #endregion
        
        #region Unit Selection
        
        private void OnUnitSelected(GameObject unitGO)
        {
            selectedUnit = unitGO?.GetComponent<UnitStatus>();
            selectedCard = null;
            
            OnHandChanged?.Invoke(hand);
        }

        private void OnUnitDeselected()
        {
            selectedUnit = null;
            selectedCard = null;
            OnHandChanged?.Invoke(hand);
        }
        
        /// <summary>
        /// Manually set selected unit.
        /// </summary>
        public void SetSelectedUnit(UnitStatus unit)
        {
            selectedUnit = unit;
            selectedCard = null;
            OnHandChanged?.Invoke(hand);
        }
        
        /// <summary>
        /// Check if a card is playable by currently selected unit.
        /// Delegates to CardPlayabilityChecker which handles energy + per-effect rules.
        /// </summary>
        public bool IsCardPlayable(BattleCard card)
        {
            return CardPlayabilityChecker.Check(card, selectedUnit).isPlayable;
        }

        /// <summary>
        /// Get the full playability result (reason string + energy flag) for UI display.
        /// </summary>
        public CardPlayabilityChecker.Result GetCardPlayability(BattleCard card)
        {
            return CardPlayabilityChecker.Check(card, selectedUnit);
        }
        
        /// <summary>
        /// Get cards belonging to a specific unit.
        /// </summary>
        public List<BattleCard> GetCardsForUnit(UnitStatus unit)
        {
            return hand.Where(c => c.BelongsTo(unit)).ToList();
        }
        
        /// <summary>
        /// Get all playable cards in hand for selected unit.
        /// </summary>
        public List<BattleCard> GetPlayableCards()
        {
            if (selectedUnit == null) return new List<BattleCard>();
            
            var energyManager = ServiceLocator.Get<EnergyManager>();
            return hand
                .Where(c => c.BelongsTo(selectedUnit) && energyManager.HasEnergy(c.energyCost))
                .ToList();
        }
        
        #endregion
        
        #region Queries
        
        /// <summary>
        /// Get cards in hand by category.
        /// </summary>
        public List<BattleCard> GetCardsByCategory(RelicCategory category)
        {
            return hand.Where(c => c.category == category).ToList();
        }
        
        /// <summary>
        /// Get count of cards by owner unit.
        /// </summary>
        public int GetCardCountByOwner(UnitStatus owner)
        {
            return hand.Count(c => c.BelongsTo(owner));
        }
        
        /// <summary>
        /// Count stowed cards in hand.
        /// </summary>
        public int GetStowedCount()
        {
            return hand.Count(c => c.isStowed);
        }
        
        #endregion
        
        #region Debug
        
        public string GetDeckSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Battle Deck Summary ===");
            sb.AppendLine($"Deck: {deck.Count} | Hand: {hand.Count} | Discard: {discardPile.Count}");
            sb.AppendLine("--- Hand ---");
            foreach (var card in hand)
            {
                string stowed = card.isStowed ? " [STOWED]" : "";
                sb.AppendLine($"  [{card.energyCost}] {card.GetDisplayName()} ({card.GetOwnerName()}){stowed}");
            }
            return sb.ToString();
        }
        
        [ContextMenu("Debug Print Deck")]
        public void DebugPrintDeck()
        {
            Debug.Log(GetDeckSummary());
        }
        
        #endregion
    }
}