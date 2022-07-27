using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using AllocsFixes.NetConnections.Servers.Web.API;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class ApiHandler : PathHandler {
		private readonly Dictionary<string, WebAPI> apis = new CaseInsensitiveStringDictionary<WebAPI> ();

		public ApiHandler (string _moduleName = null) : base (_moduleName) {

			foreach (Type t in Assembly.GetExecutingAssembly ().GetTypes ()) {
				if (!t.IsAbstract && t.IsSubclassOf (typeof (WebAPI))) {
					ConstructorInfo ctor = t.GetConstructor (new Type [0]);
					if (ctor != null) {
						WebAPI apiInstance = (WebAPI) ctor.Invoke (new object [0]);
						addApi (apiInstance);
					}
				}
			}

			// Permissions that don't map to a real API
			addApi (new Null ("viewallclaims"));
			addApi (new Null ("viewallplayers"));
		}

		private void addApi (WebAPI _api) {
			apis.Add (_api.Name, _api);
			WebPermissions.Instance.AddKnownModule ("webapi." + _api.Name, _api.DefaultPermissionLevel ());
		}

#if ENABLE_PROFILER
		private static readonly UnityEngine.Profiling.CustomSampler apiHandlerSampler = UnityEngine.Profiling.CustomSampler.Create ("API_Handler");
#endif

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			string apiName = _req.Url.AbsolutePath.Remove (0, urlBasePath.Length);

			if (!apis.TryGetValue (apiName, out WebAPI api)) {
				Log.Out ($"Error in {nameof(ApiHandler)}.HandleRequest(): No handler found for API \"{apiName}\"");
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				return;
			}

			if (!AuthorizeForApi (apiName, _permissionLevel)) {
				_resp.StatusCode = (int) HttpStatusCode.Forbidden;
				if (_user != null) {
					//Log.Out ($"{nameof(ApiHandler)}: user '{user.SteamID}' not allowed to execute '{apiName}'");
				}

				return;
			}

			try {
#if ENABLE_PROFILER
				apiHandlerSampler.Begin ();
#endif
				api.HandleRequest (_req, _resp, _user, _permissionLevel);
#if ENABLE_PROFILER
				apiHandlerSampler.End ();
#endif
			} catch (Exception e) {
				Log.Error ($"Error in {nameof(ApiHandler)}.HandleRequest(): Handler {api.Name} threw an exception:");
				Log.Exception (e);
				_resp.StatusCode = (int) HttpStatusCode.InternalServerError;
			}
		}

		private bool AuthorizeForApi (string _apiName, int _permissionLevel) {
			return WebPermissions.Instance.ModuleAllowedWithLevel ("webapi." + _apiName, _permissionLevel);
		}
	}
}