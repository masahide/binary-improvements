using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetPlayersLocation : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			AdminTools admTools = GameManager.Instance.adminTools;
			PlatformUserIdentifierAbs userId = _user?.UserId;

			bool listOffline = false;
			if (_req.QueryString ["offline"] != null) {
				bool.TryParse (_req.QueryString ["offline"], out listOffline);
			}

			bool bViewAll = WebConnection.CanViewAllPlayers (_permissionLevel);

			JSONArray playersJsResult = new JSONArray ();

			Players playersList = PersistentContainer.Instance.Players;

			foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in playersList.Dict) {
				if (admTools != null) {
					if (admTools.IsBanned (kvp.Key, out _, out _)) {
						continue;
					}
				}

				Player p = kvp.Value;

				if (listOffline || p.IsOnline) {
					if (bViewAll || p.PlatformId.Equals (userId)) {
						JSONObject pos = new JSONObject ();
						pos.Add ("x", new JSONNumber (p.LastPosition.x));
						pos.Add ("y", new JSONNumber (p.LastPosition.y));
						pos.Add ("z", new JSONNumber (p.LastPosition.z));

						JSONObject pJson = new JSONObject ();
						pJson.Add ("steamid", new JSONString (kvp.Key.CombinedString));

						//					pJson.Add("entityid", new JSONNumber (p.EntityID));
						//                    pJson.Add("ip", new JSONString (p.IP));
						pJson.Add ("name", new JSONString (p.Name));
						pJson.Add ("online", new JSONBoolean (p.IsOnline));
						pJson.Add ("position", pos);

						//					pJson.Add ("totalplaytime", new JSONNumber (p.TotalPlayTime));
						//					pJson.Add ("lastonline", new JSONString (p.LastOnline.ToString ("s")));
						//					pJson.Add ("ping", new JSONNumber (p.IsOnline ? p.ClientInfo.ping : -1));

						playersJsResult.Add (pJson);
					}
				}
			}

			WriteJSON (_resp, playersJsResult);
		}
	}
}