using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetPlayerInventories : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			GetPlayerInventory.GetInventoryArguments (_req, out bool showIconColor, out bool showIconName);

			JSONArray AllInventoriesResult = new JSONArray ();

			foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in PersistentContainer.Instance.Players.Dict) {
				Player p = kvp.Value;

				if (p == null) {
					continue;
				}

				if (p.IsOnline) {
					AllInventoriesResult.Add (GetPlayerInventory.DoPlayer (kvp.Key.CombinedString, p, showIconColor, showIconName));
				}
			}

			WriteJSON (_resp, AllInventoriesResult);
		}
	}
}