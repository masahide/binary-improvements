using System;
using System.Collections.Generic;
using UnityEngine;

namespace AllocsFixes.NetConnections.Servers.Web {
	public class LogBuffer {
		private const int MAX_ENTRIES = 3000;
		private static LogBuffer instance;

		private readonly List<LogEntry> logEntries = new List<LogEntry> ();

		private int listOffset;

		public static void Init () {
			if (instance == null) {
				instance = new LogBuffer ();
			}
		}

		private LogBuffer () {
			Log.LogCallbacksExtended += LogCallback;
		}

		public static LogBuffer Instance {
			get {
				if (instance == null) {
					instance = new LogBuffer ();
				}

				return instance;
			}
		}

		public int OldestLine {
			get {
				lock (logEntries) {
					return listOffset;
				}
			}
		}

		public int LatestLine {
			get {
				lock (logEntries) {
					return listOffset + logEntries.Count - 1;
				}
			}
		}

		public int StoredLines {
			get {
				lock (logEntries) {
					return logEntries.Count;
				}
			}
		}

		public LogEntry this [int _index] {
			get {
				lock (logEntries) {
					if (_index >= listOffset && _index < listOffset + logEntries.Count) {
						return logEntries [_index];
					}
				}

				return null;
			}
		}

		private void LogCallback (string _formattedMsg, string _plainMsg, string _trace, LogType _type, DateTime _timestamp, long _uptime) {
			LogEntry le = new LogEntry ();

			le.date = $"{_timestamp.Year:0000}-{_timestamp.Month:00}-{_timestamp.Day:00}";
			le.time = $"{_timestamp.Hour:00}:{_timestamp.Minute:00}:{_timestamp.Second:00}";
			le.uptime = _uptime.ToString ();
			le.message = _plainMsg;
			le.trace = _trace;
			le.type = _type;

			lock (logEntries) {
				logEntries.Add (le);
				if (logEntries.Count > MAX_ENTRIES) {
					listOffset += logEntries.Count - MAX_ENTRIES;
					logEntries.RemoveRange (0, logEntries.Count - MAX_ENTRIES);
				}
			}
		}

		private readonly List<LogEntry> emptyList = new List<LogEntry> ();

		public List<LogEntry> GetRange (ref int _start, int _count, out int _end) {
			lock (logEntries) {
				int index;
				
				if (_count < 0) {
					_count = -_count;
					
					if (_start >= listOffset + logEntries.Count) {
						_start = listOffset + logEntries.Count - 1;
					}

					_end = _start;

					if (_start < listOffset) {
						return emptyList;
					}
					
					_start -= _count - 1;

					if (_start < listOffset) {
						_start = listOffset;
					}

					index = _start - listOffset;
					_end += 1;
					_count = _end - _start;
				} else {
					if (_start < listOffset) {
						_start = listOffset;
					}

					if (_start >= listOffset + logEntries.Count) {
						_end = _start;
						return emptyList;
					}

					index = _start - listOffset;

					if (index + _count > logEntries.Count) {
						_count = logEntries.Count - index;
					}

					_end = _start + _count;
				}

				return logEntries.GetRange (index, _count);
			}
		}


		public class LogEntry {
			public string date;
			public string message;
			public string time;
			public string trace;
			public LogType type;
			public string uptime;
		}
	}
}