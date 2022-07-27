using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetPlayersOnline : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONArray players = new JSONArray ();

			World w = GameManager.Instance.World;
			foreach (KeyValuePair<int, EntityPlayer> current in w.Players.dict) {
				ClientInfo ci = ConnectionManager.Instance.Clients.ForEntityId (current.Key);
				Player player = PersistentContainer.Instance.Players [ci.InternalId, false];

				JSONObject pos = new JSONObject ();
				pos.Add ("x", new JSONNumber ((int) current.Value.GetPosition ().x));
				pos.Add ("y", new JSONNumber ((int) current.Value.GetPosition ().y));
				pos.Add ("z", new JSONNumber ((int) current.Value.GetPosition ().z));

				JSONObject p = new JSONObject ();
				p.Add ("steamid", new JSONString (ci.PlatformId.CombinedString));
				p.Add ("entityid", new JSONNumber (ci.entityId));
				p.Add ("ip", new JSONString (ci.ip));
				p.Add ("name", new JSONString (current.Value.EntityName));
				p.Add ("online", new JSONBoolean (true));
				p.Add ("position", pos);

				p.Add ("level", new JSONNumber (player?.Level ?? -1));
				p.Add ("health", new JSONNumber (current.Value.Health));
				p.Add ("stamina", new JSONNumber (current.Value.Stamina));
				p.Add ("zombiekills", new JSONNumber (current.Value.KilledZombies));
				p.Add ("playerkills", new JSONNumber (current.Value.KilledPlayers));
				p.Add ("playerdeaths", new JSONNumber (current.Value.Died));
				p.Add ("score", new JSONNumber (current.Value.Score));

				p.Add ("totalplaytime", new JSONNumber (player?.TotalPlayTime ?? -1));
				p.Add ("lastonline", new JSONString (player != null ? player.LastOnline.ToString ("s") : string.Empty));
				p.Add ("ping", new JSONNumber (ci.ping));

				players.Add (p);
			}

			WriteJSON (_resp, players);
		}
	}
}