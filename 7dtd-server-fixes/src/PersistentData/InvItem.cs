using System;
using System.Runtime.Serialization;

namespace AllocsFixes.PersistentData {
	[Serializable]
	public class InvItem {
		public string itemName;
		public int count;
		public int quality;
		public InvItem[] parts;
		public string icon = "";
		public string iconcolor = "";
		[OptionalField]
		public int maxUseTimes;
		[OptionalField]
		public float useTimes;

		public InvItem (string _itemName, int _count, int _quality, int _maxUseTimes, float _maxUse) {
			itemName = _itemName;
			count = _count;
			quality = _quality;
			maxUseTimes = _maxUseTimes;
			useTimes = _maxUse;
		}
	}
}