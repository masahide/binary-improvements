using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetPlayerInventory : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			if (_req.QueryString ["userid"] == null) {
				_resp.StatusCode = (int) HttpStatusCode.BadRequest;
				Web.SetResponseTextContent (_resp, "No user id given");
				return;
			}

			string userIdString = _req.QueryString ["userid"];
			if (!PlatformUserIdentifierAbs.TryFromCombinedString (userIdString, out PlatformUserIdentifierAbs userId)) {
				_resp.StatusCode = (int) HttpStatusCode.BadRequest;
				Web.SetResponseTextContent (_resp, "Invalid user id given");
				return;
			}

			Player p = PersistentContainer.Instance.Players [userId, false];
			if (p == null) {
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				Web.SetResponseTextContent (_resp, "Unknown user id given");
				return;
			}

			GetInventoryArguments (_req, out bool showIconColor, out bool showIconName);

			JSONObject result = DoPlayer (userIdString, p, showIconColor, showIconName);

			WriteJSON (_resp, result);
		}

		internal static void GetInventoryArguments (HttpListenerRequest _req, out bool _showIconColor, out bool _showIconName) {
			if (_req.QueryString ["showiconcolor"] == null || !bool.TryParse (_req.QueryString ["showiconcolor"], out _showIconColor)) {
				_showIconColor = true;
			}
			
			if (_req.QueryString ["showiconname"] == null || !bool.TryParse (_req.QueryString ["showiconname"], out _showIconName)) {
				_showIconName = true;
			}
		}

		internal static JSONObject DoPlayer (string _steamId, Player _player, bool _showIconColor, bool _showIconName) {
			PersistentData.Inventory inv = _player.Inventory;

			JSONObject result = new JSONObject ();

			JSONArray bag = new JSONArray ();
			JSONArray belt = new JSONArray ();
			JSONObject equipment = new JSONObject ();
			result.Add ("userid", new JSONString (_steamId));
			result.Add ("entityid", new JSONNumber (_player.EntityID));
			result.Add ("playername", new JSONString (_player.Name));
			result.Add ("bag", bag);
			result.Add ("belt", belt);
			result.Add ("equipment", equipment);

			DoInventory (belt, inv.belt, _showIconColor, _showIconName);
			DoInventory (bag, inv.bag, _showIconColor, _showIconName);

			AddEquipment (equipment, "head", inv.equipment, EquipmentSlots.Headgear, _showIconColor, _showIconName);
			AddEquipment (equipment, "eyes", inv.equipment, EquipmentSlots.Eyewear, _showIconColor, _showIconName);
			AddEquipment (equipment, "face", inv.equipment, EquipmentSlots.Face, _showIconColor, _showIconName);

			AddEquipment (equipment, "armor", inv.equipment, EquipmentSlots.ChestArmor, _showIconColor, _showIconName);
			AddEquipment (equipment, "jacket", inv.equipment, EquipmentSlots.Jacket, _showIconColor, _showIconName);
			AddEquipment (equipment, "shirt", inv.equipment, EquipmentSlots.Shirt, _showIconColor, _showIconName);

			AddEquipment (equipment, "legarmor", inv.equipment, EquipmentSlots.LegArmor, _showIconColor, _showIconName);
			AddEquipment (equipment, "pants", inv.equipment, EquipmentSlots.Legs, _showIconColor, _showIconName);
			AddEquipment (equipment, "boots", inv.equipment, EquipmentSlots.Feet, _showIconColor, _showIconName);

			AddEquipment (equipment, "gloves", inv.equipment, EquipmentSlots.Hands, _showIconColor, _showIconName);

			return result;
		}

		private static void DoInventory (JSONArray _jsonRes, List<InvItem> _inv, bool _showIconColor, bool _showIconName) {
			for (int i = 0; i < _inv.Count; i++) {
				_jsonRes.Add (GetJsonForItem (_inv [i], _showIconColor, _showIconName));
			}
		}

		private static void AddEquipment (JSONObject _eq, string _slotname, InvItem[] _items, EquipmentSlots _slot, bool _showIconColor, bool _showIconName) {
			int[] slotindices = XUiM_PlayerEquipment.GetSlotIndicesByEquipmentSlot (_slot);

			for (int i = 0; i < slotindices.Length; i++) {
				if (_items? [slotindices [i]] != null) {
					InvItem item = _items [slotindices [i]];
					_eq.Add (_slotname, GetJsonForItem (item, _showIconColor, _showIconName));
					return;
				}
			}

			_eq.Add (_slotname, new JSONNull ());
		}

		private static JSONNode GetJsonForItem (InvItem _item, bool _showIconColor, bool _showIconName) {
			if (_item == null) {
				return new JSONNull ();
			}

			JSONObject jsonItem = new JSONObject ();
			jsonItem.Add ("count", new JSONNumber (_item.count));
			jsonItem.Add ("name", new JSONString (_item.itemName));
			
			if (_showIconName) {
				jsonItem.Add ("icon", new JSONString (_item.icon));
			}

			if (_showIconColor) {
				jsonItem.Add ("iconcolor", new JSONString (_item.iconcolor));
			}

			jsonItem.Add ("quality", new JSONNumber (_item.quality));
			if (_item.quality >= 0) {
				jsonItem.Add ("qualitycolor", new JSONString (QualityInfo.GetQualityColorHex (_item.quality)));
			}

			return jsonItem;

		}
	}
}