using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using AllocsFixes.JSON;
using AllocsFixes.NetConnections.Servers.Web.API;
using UnityEngine;

namespace AllocsFixes.NetConnections.Servers.Web {
	public class WebCommandResult : IConsoleConnection {
		public enum ResultType {
			Full,
			ResultOnly,
			Raw
		}

		public static int handlingCount;
		public static int currentHandlers;
		public static long totalHandlingTime;
		private readonly string command;
		private readonly string parameters;

		private readonly HttpListenerResponse response;
		private readonly ResultType responseType;

		public WebCommandResult (string _command, string _parameters, ResultType _responseType,
			HttpListenerResponse _response) {
			Interlocked.Increment (ref handlingCount);
			Interlocked.Increment (ref currentHandlers);

			response = _response;
			command = _command;
			parameters = _parameters;
			responseType = _responseType;
		}

		public void SendLines (List<string> _output) {
//			MicroStopwatch msw = new MicroStopwatch ();

			StringBuilder sb = new StringBuilder ();
			foreach (string line in _output) {
				sb.AppendLine (line);
			}

			try {
				response.SendChunked = false;

				if (responseType == ResultType.Raw) {
					WebAPI.WriteText (response, sb.ToString ());
				} else {
					JSONNode result;
					if (responseType == ResultType.ResultOnly) {
						result = new JSONString (sb.ToString ());
					} else {
						JSONObject resultObj = new JSONObject ();

						resultObj.Add ("command", new JSONString (command));
						resultObj.Add ("parameters", new JSONString (parameters));
						resultObj.Add ("result", new JSONString (sb.ToString ()));

						result = resultObj;
					}

					WebAPI.WriteJSON (response, result);
				}
			} catch (IOException e) {
				if (e.InnerException is SocketException) {
					Log.Out ("Error in WebCommandResult.SendLines(): Remote host closed connection: " +
					         e.InnerException.Message);
				} else {
					Log.Out ("Error (IO) in WebCommandResult.SendLines(): " + e);
				}
			} catch (Exception e) {
				Log.Out ("Error in WebCommandResult.SendLines(): " + e);
			} finally {
				if (response != null) {
					response.Close ();
				}

//				msw.Stop ();
//				if (GamePrefs.GetInt (EnumGamePrefs.HideCommandExecutionLog) < 1) {
//					totalHandlingTime += msw.ElapsedMicroseconds;
//					Log.Out ("WebCommandResult.SendLines(): Took {0} µs", msw.ElapsedMicroseconds);
//				}

				Interlocked.Decrement (ref currentHandlers);
			}
		}

		public void SendLine (string _text) {
			//throw new NotImplementedException ();
		}

		public void SendLog (string _formattedMessage, string _plainMessage, string _trace, LogType _type, DateTime _timestamp, long _uptime) {
			//throw new NotImplementedException ();
		}

		public void EnableLogLevel (LogType _type, bool _enable) {
			//throw new NotImplementedException ();
		}

		public string GetDescription () {
			return "WebCommandResult_for_" + command;
		}
	}
}