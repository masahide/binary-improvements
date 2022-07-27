using System.Net;
using AllocsFixes.JSON;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetAllowedCommands : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONObject result = new JSONObject ();
			JSONArray entries = new JSONArray ();
			foreach (IConsoleCommand cc in SdtdConsole.Instance.GetCommands ()) {
				int commandPermissionLevel = GameManager.Instance.adminTools.GetCommandPermissionLevel (cc.GetCommands ());
				if (_permissionLevel <= commandPermissionLevel) {
					string cmd = string.Empty;
					foreach (string s in cc.GetCommands ()) {
						if (s.Length > cmd.Length) {
							cmd = s;
						}
					}

					JSONObject cmdObj = new JSONObject ();
					cmdObj.Add ("command", new JSONString (cmd));
					cmdObj.Add ("description", new JSONString (cc.GetDescription ()));
					cmdObj.Add ("help", new JSONString (cc.GetHelp ()));
					entries.Add (cmdObj);
				}
			}

			result.Add ("commands", entries);

			WriteJSON (_resp, result);
		}

		public override int DefaultPermissionLevel () {
			return 2000;
		}
	}
}