using UnityEngine;
using TacticalGame.Enums;
using TacticalGame.Units;
using TacticalGame.Core;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Debug tool to test the relic system.
    /// Attach to a unit and use context menu to test.
    /// </summary>
    public class RelicSystemTester : MonoBehaviour
    {
        [Header("Test Setup")]
        [SerializeField] private UnitRole testRelicRole = UnitRole.Captain;
        
        private UnitStatus unitStatus;
        private UnitEquipmentUpdated equipment;
        private CardDeckManager deckManager;
        private PassiveRelicManager passiveManager;
        
        private void Awake()
        {
            GetRequiredComponents();
        }
        
        private void GetRequiredComponents()
        {
            unitStatus = GetComponent<UnitStatus>();
            equipment = GetComponent<UnitEquipmentUpdated>();
            deckManager = GetComponent<CardDeckManager>();
            passiveManager = GetComponent<PassiveRelicManager>();
        }
        
        [ContextMenu("1. Setup Test Relics")]
        public void SetupTestRelics()
        {
            GetRequiredComponents();
            
            if (equipment == null)
            {
                Debug.LogError("No UnitEquipmentUpdated found!");
                return;
            }
            
            // Initialize if needed
            if (equipment.UnitRole == 0)
            {
                equipment.Initialize(UnitRole.Captain, WeaponFamily.Cutlass);
            }
            
            // Equip one of each category with the test role
            equipment.EquipRelic(RelicCategory.Boots, testRelicRole);
            equipment.EquipRelic(RelicCategory.Gloves, testRelicRole);
            equipment.EquipRelic(RelicCategory.Hat, testRelicRole);
            equipment.EquipRelic(RelicCategory.Coat, testRelicRole);
            equipment.EquipRelic(RelicCategory.Trinket, testRelicRole);
            equipment.EquipRelic(RelicCategory.Totem, testRelicRole);
            
            Debug.Log($"<color=green>=== TEST RELICS EQUIPPED ({testRelicRole}) ===</color>");
            Debug.Log(equipment.GetEquipmentSummary());
        }
        
        [ContextMenu("2. Build Deck")]
        public void BuildTestDeck()
        {
            GetRequiredComponents();
            
            if (deckManager == null)
            {
                Debug.LogError("No CardDeckManager found!");
                return;
            }
            
            deckManager.BuildDeck();
            Debug.Log($"<color=green>=== DECK BUILT ===</color>");
            Debug.Log(deckManager.GetDeckSummary());
        }
        
        [ContextMenu("3. Test Play Card (Index 0)")]
        public void TestPlayCard()
        {
            GetRequiredComponents();
            
            if (deckManager == null)
            {
                Debug.LogError("No CardDeckManager found!");
                return;
            }
            
            if (deckManager.CardsInHand == 0)
            {
                Debug.LogWarning("No cards in hand!");
                return;
            }
            
            var card = deckManager.Hand[0];
            Debug.Log($"<color=yellow>Attempting to play: {card.GetDisplayName()}</color>");
            
            // Find a target (any enemy)
            UnitStatus target = null;
            foreach (var unit in FindObjectsOfType<UnitStatus>())
            {
                if (unit != unitStatus && unit.Team != unitStatus.Team)
                {
                    target = unit;
                    break;
                }
            }
            
            bool success = deckManager.PlayCard(0, target, null);
            Debug.Log(success ? "<color=green>Card played successfully!</color>" : "<color=red>Failed to play card</color>");
            Debug.Log(deckManager.GetDeckSummary());
        }
        
        [ContextMenu("4. Test Passive Damage Modifier")]
        public void TestPassiveDamageModifier()
        {
            GetRequiredComponents();
            
            if (passiveManager == null)
            {
                Debug.LogError("No PassiveRelicManager found! Add it to this unit.");
                return;
            }
            
            float outgoing = passiveManager.GetOutgoingDamageModifier(null);
            float incoming = passiveManager.GetIncomingDamageModifier(null);
            
            Debug.Log($"<color=cyan>=== PASSIVE DAMAGE MODIFIERS ===</color>");
            Debug.Log($"Outgoing Damage Modifier: {outgoing:F2}x");
            Debug.Log($"Incoming Damage Modifier: {incoming:F2}x");
            Debug.Log($"Immune to Morale Focus: {passiveManager.IsImmuneMoraleFocusFire()}");
            Debug.Log($"No Buzz Downside: {passiveManager.HasNoBuzzDownside()}");
            Debug.Log($"Surrender Threshold: {passiveManager.GetSurrenderThreshold():P0}");
        }
        
        [ContextMenu("5. Simulate Turn Start")]
        public void SimulateTurnStart()
        {
            Debug.Log("<color=magenta>=== SIMULATING TURN START ===</color>");
            GameEvents.TriggerPlayerTurnStart();
            
            if (deckManager != null)
            {
                Debug.Log(deckManager.GetDeckSummary());
            }
        }
        
        [ContextMenu("6. Check Database")]
        public void CheckDatabase()
        {
            var db = RelicEffectsDatabase.Instance;
            if (db == null)
            {
                Debug.LogError("RelicEffectsDatabase not found!");
                return;
            }
            
            Debug.Log("<color=green>=== DATABASE CHECK ===</color>");
            
            // Test a few lookups
            var captainBoots = db.GetEffect(RelicCategory.Boots, UnitRole.Captain);
            var quarterTrinket = db.GetEffect(RelicCategory.Trinket, UnitRole.Quartermaster);
            var surgeonUltimate = db.GetEffect(RelicCategory.Ultimate, UnitRole.Surgeon);
            
            Debug.Log($"Captain Boots: {captainBoots?.effectType} - {captainBoots?.description}");
            Debug.Log($"Quartermaster Trinket: {quarterTrinket?.effectType} - {quarterTrinket?.description}");
            Debug.Log($"Surgeon Ultimate: {surgeonUltimate?.effectType} - {surgeonUltimate?.description}");
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            GetRequiredComponents();
            
            Debug.Log("<color=white>========================================</color>");
            Debug.Log("<color=white>   RELIC SYSTEM TEST SUITE</color>");
            Debug.Log("<color=white>========================================</color>");
            
            CheckDatabase();
            SetupTestRelics();
            BuildTestDeck();
            TestPassiveDamageModifier();
            
            Debug.Log("<color=white>========================================</color>");
            Debug.Log("<color=green>Tests complete! Check console for results.</color>");
        }
    }
}