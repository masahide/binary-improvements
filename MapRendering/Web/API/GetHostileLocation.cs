using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.LiveData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	internal class GetHostileLocation : WebAPI {
		private readonly List<EntityEnemy> enemies = new List<EntityEnemy> ();

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONArray hostilesJsResult = new JSONArray ();

			Hostiles.Instance.Get (enemies);
			for (int i = 0; i < enemies.Count; i++) {
				EntityEnemy entity = enemies [i];
				Vector3i position = new Vector3i (entity.GetPosition ());

				JSONObject jsonPOS = new JSONObject ();
				jsonPOS.Add ("x", new JSONNumber (position.x));
				jsonPOS.Add ("y", new JSONNumber (position.y));
				jsonPOS.Add ("z", new JSONNumber (position.z));

				JSONObject pJson = new JSONObject ();
				pJson.Add ("id", new JSONNumber (entity.entityId));

				if (!string.IsNullOrEmpty (entity.EntityName)) {
					pJson.Add ("name", new JSONString (entity.EntityName));
				} else {
					pJson.Add ("name", new JSONString ("enemy class #" + entity.entityClass));
				}

				pJson.Add ("position", jsonPOS);

				hostilesJsResult.Add (pJson);
			}

			WriteJSON (_resp, hostilesJsResult);
		}
	}
}