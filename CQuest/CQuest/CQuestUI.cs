using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CCore;

namespace CQuest {
	public partial class CQuestUI : Form {
		private Player _player;
		private Monster _currentMonster;

		public CQuestUI() {
			InitializeComponent();

			_player = new Player(10, 10, 20, 0, 1);
			MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
			_player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1));

			lblHitPoints.Text = _player.CurrentHitPoints.ToString();
			lblGold.Text = _player.Gold.ToString();
			lblExperience.Text = _player.ExperiencePoints.ToString();
			lblLevel.Text = _player.Level.ToString();
		}

		private void btnNorth_Click(object sender, EventArgs e) {
			MoveTo(_player.CurrentLocation.LocationToNorth);
		}

		private void btnWest_Click(object sender, EventArgs e) {
			MoveTo(_player.CurrentLocation.LocationToWest);
		}

		private void btnEast_Click(object sender, EventArgs e) {
			MoveTo(_player.CurrentLocation.LocationToEast);
		}

		private void btnSouth_Click(object sender, EventArgs e) {
			MoveTo(_player.CurrentLocation.LocationToSouth);
		}

		private void btnUseWeapon_Click(object sender, EventArgs e) {

		}

		private void btnUsePotion_Click(object sender, EventArgs e) {

		}

		private void MoveTo(Location newLocation) {
			// Check if the location have any required item
			if (!_player.HasRequiredItemToEnterThisLocation(newLocation)) {
				rtbMessages.Text += "You must have a " + newLocation.ItemRequiredToEnter.Name + " to enter this location." + Environment.NewLine;
				return;
			}

			_player.CurrentLocation = newLocation;

			btnNorth.Visible = (newLocation.LocationToNorth != null);
			btnSouth.Visible = (newLocation.LocationToSouth != null);
			btnEast.Visible = (newLocation.LocationToEast != null);
			btnWest.Visible = (newLocation.LocationToWest != null);

			rtbLocation.Text = newLocation.Name + Environment.NewLine;
			rtbLocation.Text += newLocation.Description + Environment.NewLine;

			_player.CurrentHitPoints = _player.MaximumHitPoints;

			lblHitPoints.Text = _player.CurrentHitPoints.ToString();

			// Check if the location have a quest
			if (newLocation.QuestAvailableHere != null) {
				bool playerAlreadyHasQuest = _player.HasThisQuest(newLocation.QuestAvailableHere);
				bool playerAlreadyCompletedQuest = _player.CompletedThisQuest(newLocation.QuestAvailableHere);

				if (playerAlreadyHasQuest) {
					// Check if the player have completed the quest
					if (!playerAlreadyCompletedQuest) {
						bool playerHasAllItemsToCompleteQuest = _player.HasAllQuestCompletionItems(newLocation.QuestAvailableHere);

						// The player has all items
						if (playerHasAllItemsToCompleteQuest) {
							rtbMessages.Text += Environment.NewLine;
							rtbMessages.Text += "You complete the " + newLocation.QuestAvailableHere.Name + "quest." + Environment.NewLine;

							_player.RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

							// Quest reward
							rtbMessages.Text += "You receive : " + Environment.NewLine;
							rtbMessages.Text += newLocation.QuestAvailableHere.RewardExperiencePoints.ToString() + " experience points" + Environment.NewLine;
							rtbMessages.Text += newLocation.QuestAvailableHere.RewardGold.ToString() + " gold" + Environment.NewLine;
							rtbMessages.Text += newLocation.QuestAvailableHere.RewardItem.Name + Environment.NewLine;
							rtbMessages.Text += Environment.NewLine;

							_player.ExperiencePoints += newLocation.QuestAvailableHere.RewardExperiencePoints;
							_player.Gold += newLocation.QuestAvailableHere.RewardGold;

							_player.AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);
							_player.MarkQuestCompleted(newLocation.QuestAvailableHere);
						}
					}
				} else {
					// The player do not have completed the quest
					rtbMessages.Text += "You receive the " + newLocation.QuestAvailableHere.Name + " quest." + Environment.NewLine;
					rtbMessages.Text += newLocation.QuestAvailableHere.Description + Environment.NewLine;
					rtbMessages.Text += "To complete it, return with : " + Environment.NewLine;
					foreach (QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems) {
						if (qci.Quantity == 1)
							rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.Name + Environment.NewLine;
						else
							rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.NamePlural + Environment.NewLine;
					}
					rtbMessages.Text += Environment.NewLine;

					_player.Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));
				}
			}

			// Check if the location have monster
			if (newLocation.MonsterLivingHere != null) {
				rtbMessages.Text += "You see a " + newLocation.MonsterLivingHere.Name + Environment.NewLine;

				Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

				_currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage, standardMonster.RewardExperiencePoints, standardMonster.RewardGold, standardMonster.CurrentHitPoints, standardMonster.MaximumHitPoints);

				foreach (LootItem lootItem in standardMonster.LootTable)
					_currentMonster.LootTable.Add(lootItem);

				cboWeapons.Visible = true;
				cboPotions.Visible = true;
				btnUseWeapon.Visible = true;
				btnUsePotion.Visible = true;
			} else {
				_currentMonster = null;

				cboWeapons.Visible = false;
				cboPotions.Visible = false;
				btnUseWeapon.Visible = false;
				btnUsePotion.Visible = false;
			}

			// Refresh player's inventory
			UpdateInventoryListInUI();

			// Refresh player's quest
			UpdateQuestListInUI();

			// Refresh player's weapons
			UpdateWeaponListInUI();

			// Refresh player's potions
			UpdatePotionListInUI();
		}

		private void UpdateInventoryListInUI() {
			dgvInventory.RowHeadersVisible = false;
			dgvInventory.ColumnCount = 2;
			dgvInventory.Columns[0].Name = "Name";
			dgvInventory.Columns[0].Width = 197;
			dgvInventory.Columns[1].Name = "Quantity";

			dgvInventory.Rows.Clear();

			foreach (InventoryItem inventoryItem in _player.Inventory) {
				if (inventoryItem.Quantity > 0)
					dgvInventory.Rows.Add(new[] { inventoryItem.Details.Name, inventoryItem.Quantity.ToString() });
			}
		}

		private void UpdateQuestListInUI() {
			dgvQuests.RowHeadersVisible = false;
			dgvQuests.ColumnCount = 2;
			dgvQuests.Columns[0].Name = "Name";
			dgvQuests.Columns[0].Width = 197;
			dgvQuests.Columns[1].Name = "Quantity";

			dgvQuests.Rows.Clear();

			foreach (PlayerQuest playerQuest in _player.Quests) {
				dgvQuests.Rows.Add(new[] { playerQuest.Details.Name, playerQuest.IsCompleted.ToString() });
			}
		}

		private void UpdateWeaponListInUI() {
			List<Weapon> weapons = new List<Weapon>();

			foreach (InventoryItem inventoryItem in _player.Inventory) {
				if (inventoryItem.Details is Weapon) {
					if (inventoryItem.Quantity > 0)
						weapons.Add((Weapon)inventoryItem.Details);
				}
			}

			if (weapons.Count == 0) {
				cboWeapons.Visible = false;
				btnUseWeapon.Visible = false;
			} else {
				cboWeapons.DataSource = weapons;
				cboWeapons.DisplayMember = "Name";
				cboWeapons.ValueMember = "ID";
				cboWeapons.SelectedIndex = 0;
			}
		}

		private void UpdatePotionListInUI() {
			List<HealingPotion> healingPotion = new List<HealingPotion>();

			foreach (InventoryItem inventoryItem in _player.Inventory) {
				if (inventoryItem.Details is HealingPotion) {
					if (inventoryItem.Quantity > 0)
						healingPotion.Add((HealingPotion)inventoryItem.Details);
				}
			}

			if (healingPotion.Count == 0) {
				cboPotions.Visible = false;
				btnUsePotion.Visible = false;
			} else {
				cboPotions.DataSource = healingPotion;
				cboPotions.DisplayMember = "Name";
				cboPotions.ValueMember = "ID";
				cboPotions.SelectedIndex = 0;
			}
		}
	}
}
