using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.NetConnections.Servers.Web.API;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class UserStatusHandler : PathHandler {
		public UserStatusHandler (string _moduleName = null) : base (_moduleName) {
		}

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			JSONObject result = new JSONObject ();

			result.Add ("loggedin", new JSONBoolean (_user != null));
			result.Add ("username", new JSONString (_user != null ? _user.UserId.ToString () : string.Empty));

			JSONArray perms = new JSONArray ();
			foreach (WebPermissions.WebModulePermission perm in WebPermissions.Instance.GetModules ()) {
				JSONObject permObj = new JSONObject ();
				permObj.Add ("module", new JSONString (perm.module));
				permObj.Add ("allowed", new JSONBoolean (perm.permissionLevel >= _permissionLevel));
				perms.Add (permObj);
			}

			result.Add ("permissions", perms);

			WebAPI.WriteJSON (_resp, result);
		}
	}
}