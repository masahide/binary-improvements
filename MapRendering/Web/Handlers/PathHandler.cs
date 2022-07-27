using System.Net;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public abstract class PathHandler {
		protected readonly string moduleName;
		protected string urlBasePath;
		protected Web parent;

		protected PathHandler (string _moduleName, int _defaultPermissionLevel = 0) {
			moduleName = _moduleName;
			WebPermissions.Instance.AddKnownModule (_moduleName, _defaultPermissionLevel);
		}

		public string ModuleName => moduleName;

		public abstract void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel);

		public virtual bool IsAuthorizedForHandler (WebConnection _user, int _permissionLevel) {
			return moduleName == null || WebPermissions.Instance.ModuleAllowedWithLevel (moduleName, _permissionLevel);
		}

		public virtual void Shutdown () {
		}

		public virtual void SetBasePathAndParent (Web _parent, string _relativePath) {
			parent = _parent;
			urlBasePath = _relativePath;
		}
	}
}