using System;
using System.Collections.Generic;

namespace AllocsFixes.PersistentData {
	[Serializable]
	public class Inventory {
		public List<InvItem> bag;
		public List<InvItem> belt;
		public InvItem[] equipment;

		public Inventory () {
			bag = new List<InvItem> ();
			belt = new List<InvItem> ();
			equipment = null;
		}

		public void Update (PlayerDataFile _pdf) {
			lock (this) {
				//Log.Out ("Updating player inventory - player id: " + pdf.id);
				ProcessInv (bag, _pdf.bag, _pdf.id);
				ProcessInv (belt, _pdf.inventory, _pdf.id);
				ProcessEqu (_pdf.equipment, _pdf.id);
			}
		}

		private void ProcessInv (List<InvItem> _target, ItemStack[] _sourceFields, int _id) {
			_target.Clear ();
			for (int i = 0; i < _sourceFields.Length; i++) {
				InvItem item = CreateInvItem (_sourceFields [i].itemValue, _sourceFields [i].count, _id);
				if (item != null && _sourceFields [i].itemValue.Modifications != null) {
					ProcessParts (_sourceFields [i].itemValue.Modifications, item, _id);
				}

				_target.Add (item);
			}
		}

		private void ProcessEqu (Equipment _sourceEquipment, int _playerId) {
			equipment = new InvItem[_sourceEquipment.GetSlotCount ()];
			for (int i = 0; i < _sourceEquipment.GetSlotCount (); i++) {
				equipment [i] = CreateInvItem (_sourceEquipment.GetSlotItem (i), 1, _playerId);
			}
		}

		private void ProcessParts (ItemValue[] _parts, InvItem _item, int _playerId) {
			InvItem[] itemParts = new InvItem[_parts.Length];
			for (int i = 0; i < _parts.Length; i++) {
				InvItem partItem = CreateInvItem (_parts [i], 1, _playerId);
				if (partItem != null && _parts [i].Modifications != null) {
					ProcessParts (_parts [i].Modifications, partItem, _playerId);
				}

				itemParts [i] = partItem;
			}

			_item.parts = itemParts;
		}

		private InvItem CreateInvItem (ItemValue _itemValue, int _count, int _playerId) {
			if (_count <= 0 || _itemValue == null || _itemValue.Equals (ItemValue.None)) {
				return null;
			}

			ItemClass itemClass = ItemClass.list [_itemValue.type];
			int maxAllowed = itemClass.Stacknumber.Value;
			string name = itemClass.GetItemName ();

			if (_count > maxAllowed) {
				Log.Out ("Player with ID " + _playerId + " has stack for \"" + name + "\" greater than allowed (" +
				         _count + " > " + maxAllowed + ")");
			}

			InvItem item;
			if (_itemValue.HasQuality) {
				item = new InvItem (name, _count, _itemValue.Quality, _itemValue.MaxUseTimes, _itemValue.UseTimes);
			} else {
				item = new InvItem (name, _count, -1, _itemValue.MaxUseTimes, _itemValue.UseTimes);
			}

			item.icon = itemClass.GetIconName ();

			item.iconcolor = AllocsUtils.ColorToHex (itemClass.GetIconTint ());

			return item;
		}
	}
}