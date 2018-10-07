using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCore {
	public class Monster : LivingCreature {
		public int ID { get; set; }
		public string Name { get; set; }
		public int MaximumDamage { get; set; }
		public int RewardExperiencePoints { get; set; }
		public int RewardGold { get; set; }
		public List<LootItem> LootTable { get; set; }

		public Monster(int id, string name, int maximumDamage, int rewardExperiencePoints, int rewardGold, int currentHitPoints, int maximumHitPoints) : base (currentHitPoints, maximumHitPoints) {
			ID = id;
			Name = name;
			MaximumDamage = maximumDamage;
			RewardExperiencePoints = rewardExperiencePoints;
			RewardGold = rewardGold;
			LootTable = new List<LootItem>();
		}

		public List<InventoryItem> GetLootedItems() {
			List<InventoryItem> lootedItems = new List<InventoryItem>();

			foreach (LootItem lootItem in LootTable) {
				if (RandomNumberGenerator.NumberBetween(1, 100) <= lootItem.DropPercentage)
					lootedItems.Add(new InventoryItem(lootItem.Details, 1));
			}

			if (lootedItems.Count == 0) {
				foreach (LootItem lootItem in LootTable) {
					if (lootItem.IsDefaultItem)
						lootedItems.Add(new InventoryItem(lootItem.Details, 1));
				}
			}

			return lootedItems;
		}
	}
}
