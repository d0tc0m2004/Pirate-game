using System;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Stores all equipment data for a single unit.
    /// 6 relic slots, each with 3 jewel sockets = 18 total jewel slots.
    /// </summary>
    [Serializable]
    public class UnitEquipmentData
    {
        public const int TOTAL_SLOTS = 6;
        public const int MIXED_SLOTS = 4;
        public const int JEWELS_PER_SLOT = 3;

        private WeaponRelic[] weaponRelics = new WeaponRelic[TOTAL_SLOTS];
        private RelicData[] relics = new RelicData[TOTAL_SLOTS];
        private JewelData[,] jewels = new JewelData[TOTAL_SLOTS, JEWELS_PER_SLOT];

        public UnitEquipmentData()
        {
            weaponRelics = new WeaponRelic[TOTAL_SLOTS];
            relics = new RelicData[TOTAL_SLOTS];
            jewels = new JewelData[TOTAL_SLOTS, JEWELS_PER_SLOT];
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return true;
            return weaponRelics[slotIndex] == null && relics[slotIndex] == null;
        }

        public static string GetSlotName(int slotIndex)
        {
            return slotIndex switch { 0 => "R1", 1 => "R2", 2 => "R3", 3 => "R4", 4 => "ULT", 5 => "PAS", _ => "?" };
        }

        // Weapon Relic Methods
        public bool EquipWeaponRelic(int slotIndex, WeaponRelic relic)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return false;
            relics[slotIndex] = null;
            weaponRelics[slotIndex] = relic;
            return true;
        }

        public WeaponRelic GetWeaponRelic(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            return weaponRelics[slotIndex];
        }

        public WeaponRelic RemoveWeaponRelic(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            var relic = weaponRelics[slotIndex];
            weaponRelics[slotIndex] = null;
            return relic;
        }

        public List<WeaponRelic> GetAllWeaponRelics()
        {
            return weaponRelics.Where(r => r != null).ToList();
        }

        // Regular Relic Methods
        public bool EquipRelic(int slotIndex, RelicData relic)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return false;
            weaponRelics[slotIndex] = null;
            relics[slotIndex] = relic;
            return true;
        }

        public RelicData GetRelic(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            return relics[slotIndex];
        }

        public List<RelicData> GetAllRelics()
        {
            return relics.Where(r => r != null).ToList();
        }

        // Jewel Methods
        public bool EquipJewel(int slotIndex, int jewelIndex, JewelData jewel)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return false;
            if (jewelIndex < 0 || jewelIndex >= JEWELS_PER_SLOT) return false;
            jewels[slotIndex, jewelIndex] = jewel;
            return true;
        }

        public JewelData GetJewel(int slotIndex, int jewelIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            if (jewelIndex < 0 || jewelIndex >= JEWELS_PER_SLOT) return null;
            return jewels[slotIndex, jewelIndex];
        }

        public JewelData RemoveJewel(int slotIndex, int jewelIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
            if (jewelIndex < 0 || jewelIndex >= JEWELS_PER_SLOT) return null;
            var jewel = jewels[slotIndex, jewelIndex];
            jewels[slotIndex, jewelIndex] = null;
            return jewel;
        }

        public JewelData[] GetJewels(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS)
                return new JewelData[JEWELS_PER_SLOT];

            JewelData[] slotJewels = new JewelData[JEWELS_PER_SLOT];
            for (int i = 0; i < JEWELS_PER_SLOT; i++)
                slotJewels[i] = jewels[slotIndex, i];
            return slotJewels;
        }

        public int GetTotalEquippedJewelCount()
        {
            int count = 0;
            for (int s = 0; s < TOTAL_SLOTS; s++)
                for (int j = 0; j < JEWELS_PER_SLOT; j++)
                    if (jewels[s, j] != null) count++;
            return count;
        }

        // Jewel Budget: matching relics x 3
        public int GetJewelBudget(UnitRole unitRole)
        {
            int matchingCount = 0;
            for (int i = 0; i < MIXED_SLOTS; i++)
            {
                if (weaponRelics[i] != null && weaponRelics[i].MatchesRole(unitRole))
                    matchingCount++;
                else if (relics[i] != null && relics[i].MatchesRole(unitRole))
                    matchingCount++;
            }
            return matchingCount * 3;
        }

        public int GetRoleMatchingRelicCount(UnitRole unitRole)
        {
            int count = 0;
            for (int i = 0; i < MIXED_SLOTS; i++)
            {
                if (weaponRelics[i] != null && weaponRelics[i].MatchesRole(unitRole))
                    count++;
                else if (relics[i] != null && relics[i].MatchesRole(unitRole))
                    count++;
            }
            return count;
        }

        public void UnequipAll()
        {
            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                weaponRelics[i] = null;
                relics[i] = null;
                for (int j = 0; j < JEWELS_PER_SLOT; j++)
                    jewels[i, j] = null;
            }
        }
    }
}