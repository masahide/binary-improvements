using System;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using AllocsFixes.NetConnections.Servers.Web.Handlers;

// Implemented following HTML spec
// https://html.spec.whatwg.org/multipage/server-sent-events.html

namespace AllocsFixes.NetConnections.Servers.Web.SSE {
	public class SseHandler : PathHandler {
		private readonly Dictionary<string, EventBase> events = new CaseInsensitiveStringDictionary<EventBase> ();
		
		private ThreadManager.ThreadInfo queueThead;
		private readonly AutoResetEvent evSendRequest = new AutoResetEvent (false);
		private bool shutdown;

		public SseHandler (string _moduleName = null) : base (_moduleName) {
			Type[] ctorTypes = {typeof (SseHandler)};
			object[] ctorParams = {this};

			foreach (Type t in Assembly.GetExecutingAssembly ().GetTypes ()) {
				if (!t.IsAbstract && t.IsSubclassOf (typeof (EventBase))) {
					ConstructorInfo ctor = t.GetConstructor (ctorTypes);
					if (ctor != null) {
						EventBase apiInstance = (EventBase) ctor.Invoke (ctorParams);
						addEvent (apiInstance.Name, apiInstance);
					}
				}
			}
		}

		public override void SetBasePathAndParent (Web _parent, string _relativePath) {
			base.SetBasePathAndParent (_parent, _relativePath);
			
			queueThead = ThreadManager.StartThread ("SSE-Processing_" + urlBasePath, QueueProcessThread, ThreadPriority.BelowNormal,
				_useRealThread: true);
		}

		public override void Shutdown () {
			base.Shutdown ();
			shutdown = true;
			SignalSendQueue ();
		}

		private void addEvent (string _eventName, EventBase _eventInstance) {
			events.Add (_eventName, _eventInstance);
			WebPermissions.Instance.AddKnownModule ("webevent." + _eventName, _eventInstance.DefaultPermissionLevel ());
		}

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			string eventName = _req.Url.AbsolutePath.Remove (0, urlBasePath.Length);

			if (!events.TryGetValue (eventName, out EventBase eventInstance)) {
				Log.Out ($"Error in {nameof (SseHandler)}.HandleRequest(): No handler found for event \"{eventName}\"");
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				return;
			}

			if (!AuthorizeForEvent (eventName, _permissionLevel)) {
				_resp.StatusCode = (int) HttpStatusCode.Forbidden;
				if (_user != null) {
					//Log.Out ($"{nameof(SseHandler)}: user '{user.SteamID}' not allowed to access '{eventName}'");
				}

				return;
			}

			try {
				eventInstance.AddListener (_resp);

				// Keep the request open
				_resp.SendChunked = true;

				_resp.AddHeader ("Content-Type", "text/event-stream");
				_resp.OutputStream.Flush ();
			} catch (Exception e) {
				Log.Error ($"Error in {nameof (SseHandler)}.HandleRequest(): Handler {eventInstance.Name} threw an exception:");
				Log.Exception (e);
				_resp.StatusCode = (int) HttpStatusCode.InternalServerError;
			}
		}

		private bool AuthorizeForEvent (string _eventName, int _permissionLevel) {
			return WebPermissions.Instance.ModuleAllowedWithLevel ("webevent." + _eventName, _permissionLevel);
		}

		private void QueueProcessThread (ThreadManager.ThreadInfo _threadInfo) {
			try {
				while (!shutdown && !_threadInfo.TerminationRequested ()) {
					evSendRequest.WaitOne (500);

					foreach (KeyValuePair<string, EventBase> kvp in events) {
						kvp.Value.ProcessSendQueue ();
					}
				}
			} catch (Exception e) {
				Log.Error ("SSE: Error processing send queue");
				Log.Exception (e);
			}
		}

		public void SignalSendQueue () {
			evSendRequest.Set ();
		}
	}
}