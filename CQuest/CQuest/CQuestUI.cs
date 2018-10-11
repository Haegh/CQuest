using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CCore;

namespace CQuest {
	public partial class CQuestUI : Form {
		private Player _player;
		private Monster _currentMonster;
		private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";
		

		// Constructor
		public CQuestUI() {
			InitializeComponent();

			if (File.Exists(PLAYER_DATA_FILE_NAME))
				_player = Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME));
			else
				_player = Player.CreateDefaultPlayer();

			lblHitPoints.DataBindings.Add("Text", _player, "CurrentHitPoints");
			lblGold.DataBindings.Add("Text", _player, "Gold");
			lblExperience.DataBindings.Add("Text", _player, "ExperiencePoints");
			lblLevel.DataBindings.Add("Text", _player, "Level");

			// Inventory binding
			dgvInventory.RowHeadersVisible = false;
			dgvInventory.AutoGenerateColumns = false;
			dgvInventory.DataSource = _player.Inventory;
			dgvInventory.Columns.Add(new DataGridViewTextBoxColumn {
				HeaderText = "Name",
				Width = 197,
				DataPropertyName = "Description"
			});
			dgvInventory.Columns.Add(new DataGridViewTextBoxColumn {
				HeaderText = "Quantity",
				DataPropertyName = "Quantity"
			});

			// Quest binding
			dgvQuests.RowHeadersVisible = false;
			dgvQuests.AutoGenerateColumns = false;
			dgvQuests.DataSource = _player.Quests;
			dgvQuests.Columns.Add(new DataGridViewTextBoxColumn {
				HeaderText = "Name",
				Width = 197,
				DataPropertyName = "Name"
			});
			dgvQuests.Columns.Add(new DataGridViewTextBoxColumn {
				HeaderText = "Done?",
				DataPropertyName = "IsCompleted"
			});

			// Weapons binding
			cboWeapons.DataSource = _player.Weapons;
			cboWeapons.DisplayMember = "Name";
			cboWeapons.ValueMember = "Id";

			if (_player.CurrentWeapon != null)
				cboWeapons.SelectedItem = _player.CurrentWeapon;

			cboWeapons.SelectedIndexChanged += cboWeapons_SelectedIndexChanged;

			// Potions binding
			cboPotions.DataSource = _player.Potions;
			cboPotions.DisplayMember = "Name";
			cboPotions.ValueMember = "Id";

			_player.PropertyChanged += PlayerOnPropertyChanged;

			MoveTo(_player.CurrentLocation);
		}


		// Function & procedure
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
			Weapon currentWeapon = (Weapon)cboWeapons.SelectedItem;

			int damageToMonster = RandomNumberGenerator.NumberBetween(currentWeapon.MaximumDamage, currentWeapon.MaximumDamage);
			_currentMonster.CurrentHitPoints -= damageToMonster;

			rtbMessages.Text += "You hit the " + _currentMonster.Name + " for " + damageToMonster.ToString() + " points." + Environment.NewLine;

			// Check if the monster is killed
			if (_currentMonster.CurrentHitPoints <= 0) {
				rtbMessages.Text += Environment.NewLine;
				rtbMessages.Text += "You defeated the " + _currentMonster.Name + Environment.NewLine;

				_player.AddExperiencePoints(_currentMonster.RewardExperiencePoints);
				rtbMessages.Text += "You receive " + _currentMonster.RewardExperiencePoints.ToString() + " experience points." + Environment.NewLine;
				_player.Gold += _currentMonster.RewardGold;
				rtbMessages.Text += "You receive " + _currentMonster.RewardGold.ToString() + " gold." + Environment.NewLine;

				// Get random loot
				List<InventoryItem> lootedItems = _currentMonster.GetLootedItems();

				// Add the loot to the player's inventory
				foreach (InventoryItem inventoryItem in lootedItems) {
					_player.AddItemToInventory(inventoryItem.Details);

					if (inventoryItem.Quantity == 1)
						rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.Name + Environment.NewLine;
					else
						rtbMessages.Text += "You loot " + inventoryItem.Quantity.ToString() + " " + inventoryItem.Details.NamePlural + Environment.NewLine;
				}

				rtbMessages.Text += Environment.NewLine;

				MoveTo(_player.CurrentLocation);
			} else {
				// The monster is not kill

				int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
				rtbMessages.Text += "The " + _currentMonster.Name + " did "+ damageToPlayer.ToString() + " points of damage." + Environment.NewLine;

				_player.CurrentHitPoints -= damageToPlayer;

				if (_player.CurrentHitPoints <= 0) {
					rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;
					MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
				}
			}

			ScrollToBottomOfMessages();
		}

		private void btnUsePotion_Click(object sender, EventArgs e) {
			HealingPotion potion = (HealingPotion)cboPotions.SelectedItem;

			_player.CurrentHitPoints = (_player.CurrentHitPoints + potion.AmountToHeal);

			if (_player.CurrentHitPoints > _player.MaximumHitPoints)
				_player.CurrentHitPoints = _player.MaximumHitPoints;

			_player.RemoveItemFromInventory(potion);

			rtbMessages.Text += "You drink a " + potion.Name + Environment.NewLine;

			// Monster's turn
			int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
			rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() + " points of damage." + Environment.NewLine;

			_player.CurrentHitPoints -= damageToPlayer;

			if (_player.CurrentHitPoints <= 0) {
				rtbMessages.Text += "The " + _currentMonster.Name + " killed you." + Environment.NewLine;
				MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
			}

			ScrollToBottomOfMessages();
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

							_player.AddExperiencePoints(newLocation.QuestAvailableHere.RewardExperiencePoints);
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

				cboWeapons.Visible = _player.Weapons.Any();
				cboPotions.Visible = _player.Potions.Any();
				btnUseWeapon.Visible = _player.Weapons.Any();
				btnUsePotion.Visible = _player.Potions.Any();
			} else {
				_currentMonster = null;

				cboWeapons.Visible = false;
				cboPotions.Visible = false;
				btnUseWeapon.Visible = false;
				btnUsePotion.Visible = false;
			}

			ScrollToBottomOfMessages();
		}

		private void ScrollToBottomOfMessages() {
			rtbMessages.SelectionStart = rtbMessages.Text.Length;
			rtbMessages.ScrollToCaret();
		}

		private void CQuestUI_FormClosing(object sender, FormClosingEventArgs e) {
			File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
		}

		private void cboWeapons_SelectedIndexChanged(object sender, EventArgs e) {
			_player.CurrentWeapon = (Weapon)cboWeapons.SelectedItem;
		}

		private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
			if (propertyChangedEventArgs.PropertyName == "Weapons") {
				cboWeapons.DataSource = _player.Weapons;

				if (!_player.Weapons.Any()) {
					cboWeapons.Visible = false;
					btnUseWeapon.Visible = false;
				}
			}

			if (propertyChangedEventArgs.PropertyName == "Potions") {
				cboPotions.DataSource = _player.Potions;

				if (!_player.Potions.Any()) {
					cboPotions.Visible = false;
					btnUsePotion.Visible = false;
				}
			}
		}
	}
}
