using System;
using AllocsFixes.JSON;
using UnityEngine;

namespace AllocsFixes.NetConnections.Servers.Web.SSE {
	public class EventLog : EventBase {
		public EventLog (SseHandler _parent) : base (_parent, _name: "log") {
			Log.LogCallbacksExtended += LogCallback;
		}

		private void LogCallback (string _formattedMsg, string _plainMsg, string _trace, LogType _type, DateTime _timestamp, long _uptime) {
			string date = $"{_timestamp.Year:0000}-{_timestamp.Month:00}-{_timestamp.Day:00}";
			string time = $"{_timestamp.Hour:00}:{_timestamp.Minute:00}:{_timestamp.Second:00}";
			string uptime = _uptime.ToString ();
			string message = _plainMsg;

			JSONObject data = new JSONObject ();
			data.Add ("msg", new JSONString (message));
			data.Add ("type", new JSONString (_type.ToStringCached ()));
			data.Add ("trace", new JSONString (_trace));
			data.Add ("date", new JSONString (date));
			data.Add ("time", new JSONString (time));
			data.Add ("uptime", new JSONString (uptime));

			SendData ("logLine", data);
		}
	}
}