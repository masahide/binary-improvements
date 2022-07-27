using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.LiveData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	internal class GetAnimalsLocation : WebAPI {
		private readonly List<EntityAnimal> animals = new List<EntityAnimal> ();

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONArray animalsJsResult = new JSONArray ();

			Animals.Instance.Get (animals);
			for (int i = 0; i < animals.Count; i++) {
				EntityAnimal entity = animals [i];
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
					pJson.Add ("name", new JSONString ("animal class #" + entity.entityClass));
				}

				pJson.Add ("position", jsonPOS);

				animalsJsResult.Add (pJson);
			}

			WriteJSON (_resp, animalsJsResult);
		}
	}
}