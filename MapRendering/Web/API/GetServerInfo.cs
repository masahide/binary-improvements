using System;
using System.Net;
using AllocsFixes.JSON;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetServerInfo : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONObject serverInfo = new JSONObject ();

			GameServerInfo gsi = ConnectionManager.Instance.LocalServerInfo;

			foreach (string stringGamePref in Enum.GetNames (typeof (GameInfoString))) {
				string value = gsi.GetValue ((GameInfoString) Enum.Parse (typeof (GameInfoString), stringGamePref));

				JSONObject singleStat = new JSONObject ();
				singleStat.Add ("type", new JSONString ("string"));
				singleStat.Add ("value", new JSONString (value));

				serverInfo.Add (stringGamePref, singleStat);
			}

			foreach (string intGamePref in Enum.GetNames (typeof (GameInfoInt))) {
				int value = gsi.GetValue ((GameInfoInt) Enum.Parse (typeof (GameInfoInt), intGamePref));

				JSONObject singleStat = new JSONObject ();
				singleStat.Add ("type", new JSONString ("int"));
				singleStat.Add ("value", new JSONNumber (value));

				serverInfo.Add (intGamePref, singleStat);
			}

			foreach (string boolGamePref in Enum.GetNames (typeof (GameInfoBool))) {
				bool value = gsi.GetValue ((GameInfoBool) Enum.Parse (typeof (GameInfoBool), boolGamePref));

				JSONObject singleStat = new JSONObject ();
				singleStat.Add ("type", new JSONString ("bool"));
				singleStat.Add ("value", new JSONBoolean (value));

				serverInfo.Add (boolGamePref, singleStat);
			}


			WriteJSON (_resp, serverInfo);
		}
	}
}