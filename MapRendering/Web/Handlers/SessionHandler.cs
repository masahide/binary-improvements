using System.IO;
using System.Net;
using System.Text;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class SessionHandler : PathHandler {
		private readonly string footer = "";
		private readonly string header = "";

		public SessionHandler (string _dataFolder, string _moduleName = null) : base (_moduleName) {
			if (File.Exists (_dataFolder + "/sessionheader.tmpl")) {
				header = File.ReadAllText (_dataFolder + "/sessionheader.tmpl");
			}

			if (File.Exists (_dataFolder + "/sessionfooter.tmpl")) {
				footer = File.ReadAllText (_dataFolder + "/sessionfooter.tmpl");
			}
		}

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			string subpath = _req.Url.AbsolutePath.Remove (0, urlBasePath.Length);

			StringBuilder result = new StringBuilder ();
			result.Append (header);

			if (subpath.StartsWith ("verify")) {
				if (_user != null) {
					_resp.Redirect ("/static/index.html");
					return;
				}

				result.Append (
					"<h1>Login failed, <a href=\"/static/index.html\">click to return to main page</a>.</h1>");
			} else if (subpath.StartsWith ("logout")) {
				if (_user != null) {
					parent.connectionHandler.LogOut (_user.SessionID);
					Cookie cookie = new Cookie ("sid", "", "/");
					cookie.Expired = true;
					_resp.AppendCookie (cookie);
					_resp.Redirect ("/static/index.html");
					return;
				}

				result.Append (
					"<h1>Not logged in, <a href=\"/static/index.html\">click to return to main page</a>.</h1>");
			} else if (subpath.StartsWith ("login")) {
				string host = (Web.IsSslRedirected (_req) ? "https://" : "http://") + _req.UserHostName;
				string url = OpenID.GetOpenIdLoginUrl (host, host + "/session/verify");
				_resp.Redirect (url);
				return;
			} else {
				result.Append (
					"<h1>Unknown command, <a href=\"/static/index.html\">click to return to main page</a>.</h1>");
			}

			result.Append (footer);

			_resp.ContentType = MimeType.GetMimeType (".html");
			_resp.ContentEncoding = Encoding.UTF8;
			byte[] buf = Encoding.UTF8.GetBytes (result.ToString ());
			_resp.ContentLength64 = buf.Length;
			_resp.OutputStream.Write (buf, 0, buf.Length);
		}
	}
}