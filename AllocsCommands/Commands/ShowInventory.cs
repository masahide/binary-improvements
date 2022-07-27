using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace AllocsFixes.CustomCommands {
	public class ShowInventory : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "list inventory of a given player";
		}

		public override string GetHelp () {
			return "Usage:\n" +
			       "   showinventory <user id / player name / entity id> [tag]\n" +
			       "Show the inventory of the player given by his UserID, player name or\n" +
			       "entity id (as given by e.g. \"lpi\").\n" +
			       "Optionally specify a tag that is included in each line of the output. In\n" +
			       "this case output is designed to be easily parseable by tools.\n" +
			       "Note: This only shows the player's inventory after it was first sent to\n" +
			       "the server which happens at least every 30 seconds.";
		}

		public override string[] GetCommands () {
			return new[] {"showinventory", "si"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count < 1) {
				SdtdConsole.Instance.Output ("Usage: showinventory <steamid|playername|entityid> [tag]");
				return;
			}

			PlatformUserIdentifierAbs steamid = PersistentContainer.Instance.Players.GetSteamID (_params [0], true);
			if (steamid == null) {
				SdtdConsole.Instance.Output (
					"Playername or entity/steamid id not found or no inventory saved (first saved after a player has been online for 30s).");
				return;
			}

			string tag = null;
			if (_params.Count > 1 && _params [1].Length > 0) {
				tag = _params [1];
			}

			Player p = PersistentContainer.Instance.Players [steamid, false];
			PersistentData.Inventory inv = p.Inventory;

			if (tag == null) {
				SdtdConsole.Instance.Output ("Belt of player " + p.Name + ":");
			}

			PrintInv (inv.belt, p.EntityID, "belt", tag);
			if (tag == null) {
				SdtdConsole.Instance.Output (string.Empty);
			}

			if (tag == null) {
				SdtdConsole.Instance.Output ("Bagpack of player " + p.Name + ":");
			}

			PrintInv (inv.bag, p.EntityID, "backpack", tag);
			if (tag == null) {
				SdtdConsole.Instance.Output (string.Empty);
			}

			if (tag == null) {
				SdtdConsole.Instance.Output ("Equipment of player " + p.Name + ":");
			}

			PrintEquipment (inv.equipment, p.EntityID, "equipment", tag);

			if (tag != null) {
				SdtdConsole.Instance.Output ("tracker_item id=" + p.EntityID + ", tag=" + tag +
				                             ", SHOWINVENTORY DONE");
			}
		}

		private void PrintInv (List<InvItem> _inv, int _entityId, string _location, string _tag) {
			for (int i = 0; i < _inv.Count; i++) {
				if (_inv [i] != null) {
					if (_tag == null) {
						// no Tag defined -> readable output
						if (_inv [i].quality < 0) {
							SdtdConsole.Instance.Output (string.Format ("    Slot {0}: {1:000} * {2}", i,
								_inv [i].count, _inv [i].itemName));
						} else {
							SdtdConsole.Instance.Output (string.Format ("    Slot {0}: {1:000} * {2} - quality: {3}", i,
								_inv [i].count, _inv [i].itemName, _inv [i].quality));
						}

						DoParts (_inv [i].parts, 1, null);
					} else {
						// Tag defined -> parseable output
						string partsMsg = DoParts (_inv [i].parts, 1, "");
						string msg = "tracker_item id=" + _entityId + ", tag=" + _tag + ", location=" + _location +
						             ", slot=" + i + ", item=" + _inv [i].itemName + ", qnty=" + _inv [i].count +
						             ", quality=" + _inv [i].quality + ", parts=(" + partsMsg + ")";
						SdtdConsole.Instance.Output (msg);
					}
				}
			}
		}

		private void PrintEquipment (InvItem[] _equipment, int _entityId, string _location, string _tag) {
			AddEquipment ("head", _equipment, EquipmentSlots.Headgear, _entityId, _location, _tag);
			AddEquipment ("eyes", _equipment, EquipmentSlots.Eyewear, _entityId, _location, _tag);
			AddEquipment ("face", _equipment, EquipmentSlots.Face, _entityId, _location, _tag);

			AddEquipment ("armor", _equipment, EquipmentSlots.ChestArmor, _entityId, _location, _tag);
			AddEquipment ("jacket", _equipment, EquipmentSlots.Jacket, _entityId, _location, _tag);
			AddEquipment ("shirt", _equipment, EquipmentSlots.Shirt, _entityId, _location, _tag);

			AddEquipment ("legarmor", _equipment, EquipmentSlots.LegArmor, _entityId, _location, _tag);
			AddEquipment ("pants", _equipment, EquipmentSlots.Legs, _entityId, _location, _tag);
			AddEquipment ("boots", _equipment, EquipmentSlots.Feet, _entityId, _location, _tag);

			AddEquipment ("gloves", _equipment, EquipmentSlots.Hands, _entityId, _location, _tag);
		}

		private void AddEquipment (string _slotname, InvItem[] _items, EquipmentSlots _slot, int _entityId,
			string _location, string _tag) {
			int[] slotindices = XUiM_PlayerEquipment.GetSlotIndicesByEquipmentSlot (_slot);

			for (int i = 0; i < slotindices.Length; i++) {
				if (_items != null && _items [slotindices [i]] != null) {
					InvItem item = _items [slotindices [i]];
					if (_tag == null) {
						// no Tag defined -> readable output
						if (item.quality < 0) {
							SdtdConsole.Instance.Output (string.Format ("    Slot {0:8}: {1:000}", _slotname,
								item.itemName));
						} else {
							SdtdConsole.Instance.Output (string.Format ("    Slot {0:8}: {1:000} - quality: {2}",
								_slotname, item.itemName, item.quality));
						}

						DoParts (_items [slotindices [i]].parts, 1, null);
					} else {
						// Tag defined -> parseable output
						string partsMsg = DoParts (_items [slotindices [i]].parts, 1, "");
						string msg = "tracker_item id=" + _entityId + ", tag=" + _tag + ", location=" + _location +
						             ", slot=" + _slotname + ", item=" + item.itemName + ", qnty=1, quality=" +
						             item.quality + ", parts=(" + partsMsg + ")";
						SdtdConsole.Instance.Output (msg);
					}

					return;
				}
			}
		}

		private string DoParts (InvItem[] _parts, int _indent, string _currentMessage) {
			if (_parts != null && _parts.Length > 0) {
				string indenter = new string (' ', _indent * 4);
				for (int i = 0; i < _parts.Length; i++) {
					if (_parts [i] != null) {
						if (_currentMessage == null) {
							// no currentMessage given -> readable output
							if (_parts [i].quality < 0) {
								SdtdConsole.Instance.Output (string.Format ("{0}         - {1}", indenter,
									_parts [i].itemName));
							} else {
								SdtdConsole.Instance.Output (string.Format ("{0}         - {1} - quality: {2}",
									indenter, _parts [i].itemName, _parts [i].quality));
							}

							DoParts (_parts [i].parts, _indent + 1, _currentMessage);
						} else {
							// currentMessage given -> parseable output
							if (_currentMessage.Length > 0) {
								_currentMessage += ",";
							}

							_currentMessage += _parts [i].itemName + "@" + _parts [i].quality;
							_currentMessage = DoParts (_parts [i].parts, _indent + 1, _currentMessage);
						}
					}
				}
			}

			return _currentMessage;
		}
	}
}