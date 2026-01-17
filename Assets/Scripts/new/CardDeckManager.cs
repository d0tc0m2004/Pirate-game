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
    /// Represents a single card in the deck.
    /// </summary>
    [System.Serializable]
    public class RelicCard
    {
        public string cardName;
        public RelicCategory category;
        public UnitRole roleTag;
        public int energyCost;
        public RelicEffectType effectType;
        public EquippedRelic sourceRelic;
        public WeaponRelic sourceWeaponRelic;
        
        public bool IsWeaponCard => sourceWeaponRelic != null;
        
        public string GetDisplayName()
        {
            if (IsWeaponCard)
                return sourceWeaponRelic.relicName;
            return sourceRelic?.relicName ?? $"{roleTag} {category}";
        }
    }
    
    /// <summary>
    /// Manages the card deck for a unit.
    /// </summary>
    public class CardDeckManager : MonoBehaviour
    {
        #region State
        
        [Header("Deck State")]
        [SerializeField] private List<RelicCard> availableCards = new List<RelicCard>();
        [SerializeField] private List<RelicCard> spentCards = new List<RelicCard>();
        [SerializeField] private List<RelicCard> hand = new List<RelicCard>();
        
        [Header("Settings")]
        [SerializeField] private int cardsPerTurn = 5;
        [SerializeField] private int maxHandSize = 7;
        
        private UnitStatus unitStatus;
        private UnitEquipmentUpdated equipment;
        
        // Tracking for effects
        private int cardsPlayedThisRound = 0;
        private int gunnerRelicsUsedThisGame = 0;
        
        #endregion
        
        #region Properties
        
        public IReadOnlyList<RelicCard> Hand => hand;
        public IReadOnlyList<RelicCard> AvailableCards => availableCards;
        public IReadOnlyList<RelicCard> SpentCards => spentCards;
        public int CardsInHand => hand.Count;
        public int CardsAvailable => availableCards.Count;
        public int CardsSpent => spentCards.Count;
        public int CardsPlayedThisRound => cardsPlayedThisRound;
        public int GunnerRelicsUsedThisGame => gunnerRelicsUsedThisGame;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            unitStatus = GetComponent<UnitStatus>();
            equipment = GetComponent<UnitEquipmentUpdated>();
        }
        
        private void Start()
        {
            BuildDeck();
        }
        
        private void OnEnable()
        {
            GameEvents.OnRoundStart += OnRoundStart;
        }
        
        private void OnDisable()
        {
            GameEvents.OnRoundStart -= OnRoundStart;
        }
        
        private void OnRoundStart(int round)
        {
            cardsPlayedThisRound = 0;
        }
        
        #endregion
        
        #region Deck Building
        
        public void BuildDeck()
        {
            // Get references if not set (for edit mode testing)
            if (unitStatus == null) unitStatus = GetComponent<UnitStatus>();
            if (equipment == null) equipment = GetComponent<UnitEquipmentUpdated>();
            
            availableCards.Clear();
            spentCards.Clear();
            hand.Clear();
            
            if (equipment == null)
            {
                Debug.LogWarning($"{gameObject.name}: No UnitEquipmentUpdated found");
                return;
            }
            
            // Add weapon relic cards
            if (equipment.WeaponRelic != null)
            {
                int copies = equipment.WeaponRelic.baseWeaponData?.cardCopies ?? 2;
                Debug.Log($"<color=green>Weapon: Adding {copies} copies of {equipment.WeaponRelic.relicName}</color>");
                for (int i = 0; i < copies; i++)
                {
                    availableCards.Add(CreateWeaponCard(equipment.WeaponRelic));
                }
            }
            else
            {
                Debug.Log($"<color=orange>Weapon: No weapon relic equipped</color>");
            }
            
            // Add category relic cards (non-passive only)
            AddRelicCards(equipment.BootsRelic, "Boots");
            AddRelicCards(equipment.GlovesRelic, "Gloves");
            AddRelicCards(equipment.HatRelic, "Hat");
            AddRelicCards(equipment.CoatRelic, "Coat");
            AddRelicCards(equipment.TotemRelic, "Totem");
            AddRelicCards(equipment.UltimateRelic, "Ultimate");
            
            Debug.Log($"<color=cyan>{gameObject.name} deck built: {availableCards.Count} cards total</color>");
            
            // Shuffle and draw initial hand
            if (availableCards.Count > 0)
            {
                ShuffleDeck();
                DrawCards(cardsPerTurn);
            }
            else
            {
                Debug.LogWarning($"<color=red>No cards were added to deck!</color>");
            }
        }
        
        private void AddRelicCards(EquippedRelic relic, string slotName = "")
        {
            if (relic == null)
            {
                Debug.Log($"<color=orange>{slotName}: No relic equipped</color>");
                return;
            }
            
            if (relic.IsPassive())
            {
                Debug.Log($"<color=gray>{slotName}: {relic.relicName} is passive, skipping</color>");
                return;
            }
            
            int copies = relic.GetCopies();
            Debug.Log($"<color=green>{slotName}: Adding {copies} copies of {relic.relicName}</color>");
            
            for (int i = 0; i < copies; i++)
            {
                availableCards.Add(CreateRelicCard(relic));
            }
        }
        
        private RelicCard CreateRelicCard(EquippedRelic relic)
        {
            return new RelicCard
            {
                cardName = relic.relicName,
                category = relic.category,
                roleTag = relic.roleTag,
                energyCost = relic.GetEnergyCost(),
                effectType = relic.GetEffectType(),
                sourceRelic = relic,
                sourceWeaponRelic = null
            };
        }
        
        private RelicCard CreateWeaponCard(WeaponRelic relic)
        {
            return new RelicCard
            {
                cardName = relic.relicName,
                category = RelicCategory.Weapon,
                roleTag = relic.roleTag,
                energyCost = relic.GetEnergyCost(),
                effectType = RelicEffectType.None,
                sourceRelic = null,
                sourceWeaponRelic = relic
            };
        }
        
        #endregion
        
        #region Deck Operations
        
        public void ShuffleDeck()
        {
            for (int i = availableCards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = availableCards[i];
                availableCards[i] = availableCards[j];
                availableCards[j] = temp;
            }
        }
        
        public void DrawCards(int count)
        {
            for (int i = 0; i < count && hand.Count < maxHandSize; i++)
            {
                DrawOneCard();
            }
        }
        
        private bool DrawOneCard()
        {
            if (availableCards.Count == 0)
            {
                if (spentCards.Count > 0)
                {
                    ResetDeck();
                }
                else
                {
                    return false;
                }
            }
            
            if (availableCards.Count > 0)
            {
                var card = availableCards[0];
                availableCards.RemoveAt(0);
                hand.Add(card);
                Debug.Log($"<color=green>Drew: {card.GetDisplayName()}</color>");
                return true;
            }
            
            return false;
        }
        
        public bool PlayCard(int handIndex, UnitStatus target = null, GridCell targetCell = null)
        {
            if (handIndex < 0 || handIndex >= hand.Count)
            {
                Debug.LogWarning("Invalid card index");
                return false;
            }
            
            var card = hand[handIndex];
            
            var energyManager = ServiceLocator.Get<EnergyManager>();
            if (!energyManager.HasEnergy(card.energyCost))
            {
                Debug.Log("Not enough energy!");
                return false;
            }
            
            energyManager.TrySpendEnergy(card.energyCost);
            
            // Track for effects
            cardsPlayedThisRound++;
            if (card.roleTag == UnitRole.MasterGunner)
            {
                gunnerRelicsUsedThisGame++;
            }
            
            if (card.IsWeaponCard)
            {
                ExecuteWeaponCard(card, target);
            }
            else
            {
                RelicEffectExecutor.Execute(card.sourceRelic, unitStatus, target, targetCell);
            }
            
            hand.RemoveAt(handIndex);
            spentCards.Add(card);
            
            Debug.Log($"<color=yellow>Played: {card.GetDisplayName()} ({hand.Count} in hand, {spentCards.Count} spent)</color>");
            
            CheckDeckReset();
            
            return true;
        }
        
        public bool PlayCard(RelicCard card, UnitStatus target = null, GridCell targetCell = null)
        {
            int index = hand.IndexOf(card);
            if (index >= 0)
            {
                return PlayCard(index, target, targetCell);
            }
            return false;
        }
        
        private void ExecuteWeaponCard(RelicCard card, UnitStatus target)
        {
            if (target == null)
            {
                var enemies = GameObject.FindGameObjectsWithTag("Unit")
                    .Select(go => go.GetComponent<UnitStatus>())
                    .Where(u => u != null && u.Team != unitStatus.Team && !u.HasSurrendered)
                    .ToList();
                
                if (enemies.Count > 0)
                {
                    target = enemies.OrderBy(e => 
                        Vector3.Distance(transform.position, e.transform.position))
                        .First();
                }
            }
            
            if (target == null)
            {
                Debug.Log("No valid target for weapon attack");
                return;
            }
            
            UnitAttack attack = GetComponent<UnitAttack>();
            if (attack != null)
            {
                bool isMelee = card.sourceWeaponRelic.baseWeaponData.attackType == WeaponType.Melee;
                if (isMelee)
                    attack.TryMeleeAttack();
                else
                    attack.TryRangedAttack();
            }
        }
        
        private void CheckDeckReset()
        {
            if (hand.Count == 0 && availableCards.Count == 0 && spentCards.Count > 0)
            {
                ResetDeck();
            }
        }
        
        public void ResetDeck()
        {
            Debug.Log($"<color=magenta>Deck Reset! {spentCards.Count} cards returning to deck</color>");
            
            availableCards.AddRange(spentCards);
            spentCards.Clear();
            ShuffleDeck();
        }
        
        #endregion
        
        #region Turn Management
        
        public void OnTurnStart()
        {
            DrawCards(cardsPerTurn);
            ApplyPassiveEffects();
        }
        
        private void ApplyPassiveEffects()
        {
            if (equipment == null) return;
            
            foreach (var relic in equipment.GetPassiveRelics())
            {
                Debug.Log($"<color=gray>Passive active: {relic.relicName}</color>");
            }
        }
        
        #endregion
        
        #region Card Search & Special Draw
        
        /// <summary>
        /// Find a card by category in available deck or spent pile.
        /// </summary>
        public RelicCard FindCardByCategory(RelicCategory category)
        {
            var card = availableCards.FirstOrDefault(c => c.category == category);
            if (card != null) return card;
            
            card = spentCards.FirstOrDefault(c => c.category == category);
            return card;
        }
        
        /// <summary>
        /// Find a card by effect type.
        /// </summary>
        public RelicCard FindCardByEffectType(RelicEffectType effectType)
        {
            var card = availableCards.FirstOrDefault(c => c.effectType == effectType);
            if (card != null) return card;
            
            card = spentCards.FirstOrDefault(c => c.effectType == effectType);
            return card;
        }
        
        /// <summary>
        /// Add a specific card directly to hand.
        /// </summary>
        public bool AddCardToHand(RelicCard card)
        {
            if (card == null) return false;
            if (hand.Count >= maxHandSize)
            {
                Debug.Log("Hand is full!");
                return false;
            }
            
            if (availableCards.Contains(card))
            {
                availableCards.Remove(card);
            }
            else if (spentCards.Contains(card))
            {
                spentCards.Remove(card);
            }
            
            hand.Add(card);
            Debug.Log($"<color=green>Added to hand: {card.GetDisplayName()}</color>");
            return true;
        }
        
        /// <summary>
        /// Draw a random card of a specific category.
        /// </summary>
        public bool DrawCardByCategory(RelicCategory category)
        {
            var card = FindCardByCategory(category);
            if (card != null)
            {
                return AddCardToHand(card);
            }
            return false;
        }
        
        /// <summary>
        /// Count cards of a category in hand.
        /// </summary>
        public int CountCardsInHand(RelicCategory category)
        {
            return hand.Count(c => c.category == category);
        }
        
        /// <summary>
        /// Count cards by role in hand.
        /// </summary>
        public int CountCardsInHandByRole(UnitRole role)
        {
            return hand.Count(c => c.roleTag == role);
        }
        
        #endregion
        
        #region Query Methods
        
        public List<RelicCard> GetCardsInHand(RelicCategory category)
        {
            return hand.Where(c => c.category == category).ToList();
        }
        
        public bool HasPlayableCards()
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            return hand.Any(c => energyManager.HasEnergy(c.energyCost));
        }
        
        public RelicCard GetCheapestPlayableCard()
        {
            var energyManager = ServiceLocator.Get<EnergyManager>();
            return hand
                .Where(c => energyManager.HasEnergy(c.energyCost))
                .OrderBy(c => c.energyCost)
                .FirstOrDefault();
        }
        
        #endregion
        
        #region Debug
        
        public string GetDeckSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {gameObject.name} Deck ===");
            sb.AppendLine($"Hand: {hand.Count} | Deck: {availableCards.Count} | Spent: {spentCards.Count}");
            sb.AppendLine("--- Hand ---");
            foreach (var card in hand)
            {
                sb.AppendLine($"  [{card.energyCost}] {card.GetDisplayName()}");
            }
            return sb.ToString();
        }
        
        #endregion
    }
}