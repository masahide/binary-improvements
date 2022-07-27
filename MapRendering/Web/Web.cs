using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using AllocsFixes.FileCache;
using AllocsFixes.NetConnections.Servers.Web.Handlers;
using AllocsFixes.NetConnections.Servers.Web.SSE;
using UnityEngine;

namespace AllocsFixes.NetConnections.Servers.Web {
	public class Web : IConsoleServer {
		private const int GUEST_PERMISSION_LEVEL = 2000;
		public static int handlingCount;
		public static int currentHandlers;
		public static long totalHandlingTime = 0;
		private readonly HttpListener listener = new HttpListener ();
		private readonly Dictionary<string, PathHandler> handlers = new CaseInsensitiveStringDictionary<PathHandler> ();

		public readonly ConnectionHandler connectionHandler;

		public Web () {
			try {
				int webPort = GamePrefs.GetInt (EnumUtils.Parse<EnumGamePrefs> ("ControlPanelPort"));
				if (webPort < 1 || webPort > 65533) {
					Log.Out ("Webserver not started (ControlPanelPort not within 1-65533)");
					return;
				}

				if (!Directory.Exists (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location) +
				                       "/webserver")) {
					Log.Out ("Webserver not started (folder \"webserver\" not found in WebInterface mod folder)");
					return;
				}

				// TODO: Read from config
				bool useStaticCache = false;

				string dataFolder = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location) + "/webserver";

				if (!HttpListener.IsSupported) {
					Log.Out ("Webserver not started (needs Windows XP SP2, Server 2003 or later or Mono)");
					return;
				}

				
				RegisterPathHandler ("/index.htm", new SimpleRedirectHandler ("/static/index.html"));
				RegisterPathHandler ("/favicon.ico", new SimpleRedirectHandler ("/static/favicon.ico"));
				RegisterPathHandler ("/session/", new SessionHandler (dataFolder));
				RegisterPathHandler ("/userstatus", new UserStatusHandler ());
				RegisterPathHandler ("/static/", new StaticHandler (
						dataFolder,
						useStaticCache ? (AbstractCache) new SimpleCache () : new DirectAccess (),
						false)
				);
				RegisterPathHandler ("/itemicons/", new ItemIconHandler (true));
				RegisterPathHandler ("/map/", new StaticHandler (
						GameIO.GetSaveGameDir () + "/map",
						MapRendering.MapRendering.GetTileCache (),
						false,
						"web.map")
				);
				RegisterPathHandler ("/api/", new ApiHandler ());
				RegisterPathHandler ("/sse/", new SseHandler ());

				connectionHandler = new ConnectionHandler ();

				listener.Prefixes.Add ($"http://*:{webPort + 2}/");
				listener.Start ();

				SdtdConsole.Instance.RegisterServer (this);

				listener.BeginGetContext (HandleRequest, listener);

				Log.Out ("Started Webserver on " + (webPort + 2));
			} catch (Exception e) {
				Log.Out ("Error in Web.ctor: " + e);
			}
		}

		public void RegisterPathHandler (string _urlBasePath, PathHandler _handler) {
			if (handlers.ContainsKey (_urlBasePath)) {
				Log.Error ($"Web: Handler for relative path {_urlBasePath} already registerd.");
				return;
			}
			
			handlers.Add (_urlBasePath, _handler);
			_handler.SetBasePathAndParent (this, _urlBasePath);
		}

		public void Disconnect () {
			try {
				listener.Stop ();
				listener.Close ();
			} catch (Exception e) {
				Log.Out ("Error in Web.Disconnect: " + e);
			}
		}

		public void Shutdown () {
			foreach (KeyValuePair<string, PathHandler> kvp in handlers) {
				kvp.Value.Shutdown ();
			}
		}

		public void SendLine (string _line) {
			connectionHandler.SendLine (_line);
		}

		public void SendLog (string _formattedMessage, string _plainMessage, string _trace, LogType _type, DateTime _timestamp, long _uptime) {
			// Do nothing, handled by LogBuffer internally
		}

		public static bool IsSslRedirected (HttpListenerRequest _req) {
			string proto = _req.Headers ["X-Forwarded-Proto"];
			return !string.IsNullOrEmpty (proto) && proto.Equals ("https", StringComparison.OrdinalIgnoreCase);
		}
		
		private readonly Version HttpProtocolVersion = new Version(1, 1);
		
#if ENABLE_PROFILER
		private readonly UnityEngine.Profiling.CustomSampler authSampler = UnityEngine.Profiling.CustomSampler.Create ("Auth");
		private readonly UnityEngine.Profiling.CustomSampler handlerSampler = UnityEngine.Profiling.CustomSampler.Create ("Handler");
#endif

		private void HandleRequest (IAsyncResult _result) {
			if (!listener.IsListening) {
				return;
			}

			Interlocked.Increment (ref handlingCount);
			Interlocked.Increment (ref currentHandlers);

//				MicroStopwatch msw = new MicroStopwatch ();
#if ENABLE_PROFILER
			UnityEngine.Profiling.Profiler.BeginThreadProfiling ("AllocsMods", "WebRequest");
			HttpListenerContext ctx = _listener.EndGetContext (_result);
			try {
#else
			HttpListenerContext ctx = listener.EndGetContext (_result);
			listener.BeginGetContext (HandleRequest, listener);
#endif
			try {
				HttpListenerRequest request = ctx.Request;
				HttpListenerResponse response = ctx.Response;
				response.SendChunked = false;

				response.ProtocolVersion = HttpProtocolVersion;

#if ENABLE_PROFILER
				authSampler.Begin ();
#endif
				int permissionLevel = DoAuthentication (request, out WebConnection conn);
#if ENABLE_PROFILER
				authSampler.End ();
#endif


				//Log.Out ("Login status: conn!=null: {0}, permissionlevel: {1}", conn != null, permissionLevel);


				if (conn != null) {
					Cookie cookie = new Cookie ("sid", conn.SessionID, "/") {
						Expired = false,
						Expires = DateTime.MinValue,
						HttpOnly = true,
						Secure = false
					};
					response.AppendCookie (cookie);
				}

				// No game yet -> fail request
				if (GameManager.Instance.World == null) {
					response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
					return;
				}

				if (request.Url.AbsolutePath.Length < 2) {
					handlers ["/index.htm"].HandleRequest (request, response, conn, permissionLevel);
					return;
				} else {
					foreach (KeyValuePair<string, PathHandler> kvp in handlers) {
						if (request.Url.AbsolutePath.StartsWith (kvp.Key)) {
							if (!kvp.Value.IsAuthorizedForHandler (conn, permissionLevel)) {
								response.StatusCode = (int) HttpStatusCode.Forbidden;
								if (conn != null) {
									//Log.Out ("Web.HandleRequest: user '{0}' not allowed to access '{1}'", conn.SteamID, kvp.Value.ModuleName);
								}
							} else {
#if ENABLE_PROFILER
								handlerSampler.Begin ();
#endif
								kvp.Value.HandleRequest (request, response, conn, permissionLevel);
#if ENABLE_PROFILER
								handlerSampler.End ();
#endif
							}

							return;
						}
					}
				}

				// Not really relevant for non-debugging purposes:
				//Log.Out ("Error in Web.HandleRequest(): No handler found for path \"" + request.Url.AbsolutePath + "\"");
				response.StatusCode = (int) HttpStatusCode.NotFound;
			} catch (IOException e) {
				if (e.InnerException is SocketException) {
					Log.Out ("Error in Web.HandleRequest(): Remote host closed connection: " +
					         e.InnerException.Message);
				} else {
					Log.Out ("Error (IO) in Web.HandleRequest(): " + e);
				}
			} catch (Exception e) {
				Log.Error ("Error in Web.HandleRequest(): ");
				Log.Exception (e);
			} finally {
				if (ctx != null && !ctx.Response.SendChunked) {
					ctx.Response.Close ();
				}

//					msw.Stop ();
//					totalHandlingTime += msw.ElapsedMicroseconds;
//					Log.Out ("Web.HandleRequest(): Took {0} µs", msw.ElapsedMicroseconds);
				Interlocked.Decrement (ref currentHandlers);
			}
#if ENABLE_PROFILER
			} finally {
				_listener.BeginGetContext (HandleRequest, _listener);
				UnityEngine.Profiling.Profiler.EndThreadProfiling ();
			}
#endif
		}

		private int DoAuthentication (HttpListenerRequest _req, out WebConnection _con) {
			_con = null;

			string sessionId = null;
			if (_req.Cookies ["sid"] != null) {
				sessionId = _req.Cookies ["sid"].Value;
			}

			if (!string.IsNullOrEmpty (sessionId)) {
				WebConnection con = connectionHandler.IsLoggedIn (sessionId, _req.RemoteEndPoint.Address);
				if (con != null) {
					_con = con;
					return GameManager.Instance.adminTools.GetUserPermissionLevel (_con.UserId);
				}
			}

			string remoteEndpointString = _req.RemoteEndPoint.ToString ();

			if (_req.QueryString ["adminuser"] != null && _req.QueryString ["admintoken"] != null) {
				WebPermissions.AdminToken admin = WebPermissions.Instance.GetWebAdmin (_req.QueryString ["adminuser"],
					_req.QueryString ["admintoken"]);
				if (admin != null) {
					return admin.permissionLevel;
				}

				Log.Warning ("Invalid Admintoken used from " + remoteEndpointString);
			}

			if (_req.Url.AbsolutePath.StartsWith ("/session/verify", StringComparison.OrdinalIgnoreCase)) {
				try {
					ulong id = OpenID.Validate (_req);
					if (id > 0) {
						WebConnection con = connectionHandler.LogIn (id, _req.RemoteEndPoint.Address);
						_con = con;
						int level = GameManager.Instance.adminTools.GetUserPermissionLevel (con.UserId);
						Log.Out ("Steam OpenID login from {0} with ID {1}, permission level {2}",
							remoteEndpointString, con.UserId, level);
						return level;
					}

					Log.Out ("Steam OpenID login failed from {0}", remoteEndpointString);
				} catch (Exception e) {
					Log.Error ("Error validating login:");
					Log.Exception (e);
				}
			}

			return GUEST_PERMISSION_LEVEL;
		}

		public static void SetResponseTextContent (HttpListenerResponse _resp, string _text) {
			byte[] buf = Encoding.UTF8.GetBytes (_text);
			_resp.ContentLength64 = buf.Length;
			_resp.ContentType = "text/html";
			_resp.ContentEncoding = Encoding.UTF8;
			_resp.OutputStream.Write (buf, 0, buf.Length);
		}
	}
}