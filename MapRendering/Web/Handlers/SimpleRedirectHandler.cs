using System.Net;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class SimpleRedirectHandler : PathHandler {
		private readonly string target;

		public SimpleRedirectHandler (string _target, string _moduleName = null) : base (_moduleName) {
			target = _target;
		}

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			_resp.Redirect (target);
		}
	}
}