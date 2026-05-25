using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Core;
using TacticalGame.Grid;
using TacticalGame.Managers;
using TacticalGame.Combat;
using TacticalGame.Equipment;

namespace TacticalGame.AI
{
    /// <summary>
    /// Manages the shared battle deck for the enemy's team.
    /// </summary>
    public class EnemyDeckManager : MonoBehaviour
    {
        private static EnemyDeckManager _instance;
        public static EnemyDeckManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EnemyDeckManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EnemyDeckManager");
                        _instance = go.AddComponent<EnemyDeckManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private int handSize = 5;

        [Header("Deck State")]
        [SerializeField] private List<BattleCard> deck = new List<BattleCard>();
        [SerializeField] private List<BattleCard> hand = new List<BattleCard>();
        [SerializeField] private List<BattleCard> discardPile = new List<BattleCard>();
        [SerializeField] private List<EquippedRelic> allPassiveRelics = new List<EquippedRelic>();

        public IReadOnlyList<BattleCard> Deck => deck;
        public IReadOnlyList<BattleCard> Hand => hand;
        public IReadOnlyList<BattleCard> DiscardPile => discardPile;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ServiceLocator.Unregister<EnemyDeckManager>();
            }
        }

        public void BuildDeckFromScene()
        {
            var enemyUnits = UnityEngine.Object.FindObjectsByType<TacticalGame.Units.UnitStatus>(UnityEngine.FindObjectsSortMode.None).Select(u => u.gameObject).ToArray()
                .Select(go => go.GetComponent<UnitStatus>())
                .Where(u => u != null && u.Team == Team.Enemy && !u.HasSurrendered)
                .ToList();
            
            BuildDeck(enemyUnits);
        }

        public void BuildDeck(List<UnitStatus> enemyUnits)
        {
            deck.Clear();
            hand.Clear();
            discardPile.Clear();
            allPassiveRelics.Clear();

            Debug.Log($"<color=red>=== Building Shared Enemy Battle Deck ===</color>");

            foreach (var unit in enemyUnits)
            {
                if (unit == null || unit.HasSurrendered) continue;

                var flexEquip = unit.GetComponent<FlexibleUnitEquipment>();
                if (flexEquip != null)
                {
                    AddCardsFromFlexibleEquipment(flexEquip, unit);
                    continue;
                }

                var equipment = unit.GetComponent<UnitEquipmentUpdated>();
                if (equipment != null)
                {
                    AddWeaponCards(equipment.WeaponRelic, unit);
                    AddRelicCards(equipment.BootsRelic, unit);
                    AddRelicCards(equipment.GlovesRelic, unit);
                    AddRelicCards(equipment.HatRelic, unit);
                    AddRelicCards(equipment.CoatRelic, unit);
                    AddRelicCards(equipment.TotemRelic, unit);
                    AddRelicCards(equipment.UltimateRelic, unit);
                }
            }

            ShuffleDeck();
        }

        private void AddCardsFromFlexibleEquipment(FlexibleUnitEquipment flexEquip, UnitStatus owner)
        {
            for (int i = 0; i < FlexibleUnitEquipment.SLOT_COUNT; i++)
            {
                var slot = flexEquip.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;

                if (slot.hasWeapon && slot.weaponRelic != null)
                {
                    AddWeaponCards(slot.weaponRelic, owner);
                }
                else if (slot.categoryRelic != null && !slot.categoryRelic.IsPassive())
                {
                    AddRelicCards(slot.categoryRelic, owner);
                }
            }
        }

        private void AddWeaponCards(WeaponRelic relic, UnitStatus owner)
        {
            if (relic == null) return;
            int copies = relic.baseWeaponData?.cardCopies ?? 2;
            for (int i = 0; i < copies; i++) deck.Add(BattleCard.FromWeaponRelic(relic, owner, i));
        }

        private void AddRelicCards(EquippedRelic relic, UnitStatus owner)
        {
            if (relic == null || string.IsNullOrEmpty(relic.relicName) || relic.IsPassive()) return;
            if (relic.category == RelicCategory.Weapon) return;

            int copies = relic.GetCopies();
            if (copies <= 0) copies = 2;
            for (int i = 0; i < copies; i++) deck.Add(BattleCard.FromRelic(relic, owner, i));
        }

        public void ShuffleDeck()
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        public void ResetDeck()
        {
            deck.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDeck();
        }

        public void DrawToFillHand()
        {
            int toDraw = handSize;
            for (int i = 0; i < toDraw; i++)
            {
                DrawOneCard();
            }
            Debug.Log($"<color=red>Enemy Hand Drawn: {hand.Count} cards</color>");
        }

        public bool DrawOneCard()
        {
            if (deck.Count == 0)
            {
                if (discardPile.Count > 0) ResetDeck();
                else return false;
            }
            if (deck.Count == 0) return false;
            
            var card = deck[0];
            deck.RemoveAt(0);
            hand.Add(card);
            return true;
        }

        public void DiscardAllCards()
        {
            var toDiscard = new List<BattleCard>(hand);
            foreach (var card in toDiscard)
            {
                hand.Remove(card);
                discardPile.Add(card);
            }
        }

        public void ConsumeCard(BattleCard card)
        {
            if (card == null || !hand.Contains(card)) return;

            var energyManager = ServiceLocator.Get<EnemyEnergyManager>();
            if (energyManager != null)
            {
                energyManager.TrySpendEnergy(card.energyCost);
            }

            hand.Remove(card);
            discardPile.Add(card);
        }

        /// <summary>
        /// Used by the AI to physically execute a card.
        /// </summary>
        public void PlayAssignedCard(BattleCard card, UnitStatus target, GridCell targetCell)
        {
            string targetName = target != null ? target.UnitName : (targetCell != null ? $"Cell ({targetCell.XPosition}, {targetCell.YPosition})" : "None");
            Debug.Log($"<color=cyan>[Enemy AI]</color> {card.ownerUnit.UnitName} is playing card: <b>{card.GetDisplayName()}</b> (Cost: {card.energyCost}) on target: {targetName}");

            if (card.IsWeaponCard)
            {
                // Weapon attacks use TryMeleeAttack or ExecuteCardAttack
                var attack = card.ownerUnit.GetComponent<UnitAttack>();
                if (attack != null) attack.ExecuteCardAttack(card.sourceWeaponRelic);
            }
            else
            {
                // Relic execution
                if (TacticalGame.Equipment.RelicTargetSelector.Instance != null)
                {
                    TacticalGame.Equipment.RelicTargetSelector.Instance.QueueAISelection(targetCell, target);
                }
                TacticalGame.Equipment.RelicEffectExecutor.Execute(card.sourceRelic, card.ownerUnit, target, targetCell, card);
            }

            ConsumeCard(card);
        }
    }
}
