using System;
using System.Collections.Generic;
using System.Net;
using Platform.Steam;

namespace AllocsFixes.NetConnections.Servers.Web {
	public class ConnectionHandler {
		private readonly Dictionary<string, WebConnection> connections = new Dictionary<string, WebConnection> ();

		public WebConnection IsLoggedIn (string _sessionId, IPAddress _ip) {
			if (!connections.ContainsKey (_sessionId)) {
				return null;
			}

			WebConnection con = connections [_sessionId];

//			if (con.Age.TotalMinutes > parent.sessionTimeoutMinutes) {
//				connections.Remove (_sessionId);
//				return null;
//			}

			if (!Equals (con.Endpoint, _ip)) {
				// Fixed: Allow different clients from same NAT network
//				connections.Remove (_sessionId);
				return null;
			}

			con.UpdateUsage ();

			return con;
		}

		public void LogOut (string _sessionId) {
			connections.Remove (_sessionId);
		}

		public WebConnection LogIn (ulong _steamId, IPAddress _ip) {
			string sessionId = Guid.NewGuid ().ToString ();
			PlatformUserIdentifierAbs userId = new UserIdentifierSteam (_steamId);
			WebConnection con = new WebConnection (sessionId, _ip, userId);
			connections.Add (sessionId, con);
			return con;
		}

		public void SendLine (string _line) {
			foreach (KeyValuePair<string, WebConnection> kvp in connections) {
				kvp.Value.SendLine (_line);
			}
		}
	}
}