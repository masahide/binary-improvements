using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetLog : WebAPI {
		private const int MAX_COUNT = 1000;
		
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			int count, firstLine, lastLine;

			if (_req.QueryString ["count"] == null || !int.TryParse (_req.QueryString ["count"], out count)) {
				count = 50;
			}

			if (count == 0) {
				count = 1;
			}

			if (count > MAX_COUNT) {
				count = MAX_COUNT;
			}

			if (count < -MAX_COUNT) {
				count = -MAX_COUNT;
			}

			if (_req.QueryString ["firstLine"] == null || !int.TryParse (_req.QueryString ["firstLine"], out firstLine)) {
				if (count > 0) {
					firstLine = LogBuffer.Instance.OldestLine;
				} else {
					firstLine = LogBuffer.Instance.LatestLine;
				}
			}

			JSONObject result = new JSONObject ();

			List<LogBuffer.LogEntry> logEntries = LogBuffer.Instance.GetRange (ref firstLine, count, out lastLine);

			JSONArray entries = new JSONArray ();
			foreach (LogBuffer.LogEntry logEntry in logEntries) {
				JSONObject entry = new JSONObject ();
				entry.Add ("date", new JSONString (logEntry.date));
				entry.Add ("time", new JSONString (logEntry.time));
				entry.Add ("uptime", new JSONString (logEntry.uptime));
				entry.Add ("msg", new JSONString (logEntry.message));
				entry.Add ("trace", new JSONString (logEntry.trace));
				entry.Add ("type", new JSONString (logEntry.type.ToStringCached ()));
				entries.Add (entry);
			}

			result.Add ("firstLine", new JSONNumber (firstLine));
			result.Add ("lastLine", new JSONNumber (lastLine));
			result.Add ("entries", entries);

			WriteJSON (_resp, result);
		}
	}
}