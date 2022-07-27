using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AllocsFixes.JSON;

namespace AllocsFixes.NetConnections.Servers.Web.SSE {
	public abstract class EventBase {
		private const int EncodingBufferSize = 1024 * 1024;

		private readonly SseHandler Parent;
		public readonly string Name;

		private readonly byte[] encodingBuffer;
		private readonly StringBuilder stringBuilder = new StringBuilder ();

		private readonly List<HttpListenerResponse> openStreams = new List<HttpListenerResponse> ();

		private readonly global::BlockingQueue<(string _eventName, object _data)> sendQueue =
			new global::BlockingQueue<(string _eventName, object _data)> ();

		private int currentlyOpen;
		private int totalOpened;
		private int totalClosed;

		protected EventBase (SseHandler _parent, bool _reuseEncodingBuffer = true, string _name = null) {
			Name = _name ?? GetType ().Name;
			Parent = _parent;
			if (_reuseEncodingBuffer) {
				encodingBuffer = new byte[EncodingBufferSize];
			}
		}

		public virtual void AddListener (HttpListenerResponse _resp) {
			totalOpened++;
			currentlyOpen++;

			openStreams.Add (_resp);
		}

		protected void SendData (string _eventName, object _data) {
			sendQueue.Enqueue ((_eventName, _data));
			Parent.SignalSendQueue ();
		}


		public void ProcessSendQueue () {
			while (sendQueue.HasData ()) {
				(string eventName, object data) = sendQueue.Dequeue ();
				
				stringBuilder.Append ("event: ");
				stringBuilder.AppendLine (eventName);
				stringBuilder.Append ("data: ");
				
				switch (data) {
					case string dataString:
						stringBuilder.AppendLine (dataString);
						break;
					case JSONNode dataJson:
						dataJson.ToString (stringBuilder);
						stringBuilder.AppendLine ("");
						break;
					default:
						logError ("Data is neither string nor JSON.", false);
						continue;
				}
				
				stringBuilder.AppendLine ("");
				string output = stringBuilder.ToString ();
				stringBuilder.Clear ();

				byte[] buf;
				int bytesToSend;
				if (encodingBuffer != null) {
					buf = encodingBuffer;
					try {
						bytesToSend = Encoding.UTF8.GetBytes (output, 0, output.Length, buf, 0);
					} catch (ArgumentException e) {
						logError ("Exception while encoding data for output, most likely exceeding buffer size:", false);
						Log.Exception (e);
						return;
					}
				} else {
					buf = Encoding.UTF8.GetBytes (output);
					bytesToSend = buf.Length;
				}

				for (int i = openStreams.Count - 1; i >= 0; i--) {
					HttpListenerResponse resp = openStreams [i];
					try {
						if (resp.OutputStream.CanWrite) {
							resp.OutputStream.Write (buf, 0, bytesToSend);
							resp.OutputStream.Flush ();
						} else {
							currentlyOpen--;
							totalClosed++;

							logError ("Can not write to endpoint, closing", true);
							openStreams.RemoveAt (i);
							resp.Close ();
						}
					} catch (IOException e) {
						currentlyOpen--;
						totalClosed++;

						openStreams.RemoveAt (i);

						if (e.InnerException is SocketException se) {
							if (se.SocketErrorCode != SocketError.ConnectionAborted && se.SocketErrorCode != SocketError.Shutdown) {
								logError ($"SocketError ({se.SocketErrorCode}) while trying to write", true);
							}
						} else {
							logError ("IOException while trying to write:", true);
							Log.Exception (e);
						}

						resp.Close ();
					} catch (Exception e) {
						currentlyOpen--;
						totalClosed++;

						openStreams.RemoveAt (i);
						logError ("Exception while trying to write:", true);
						Log.Exception (e);
						resp.Close ();
					}
				}
			}
		}

		protected void logError (string _message, bool _printConnections) {
			Log.Error (_printConnections
				? $"SSE ({Name}): {_message} (Left open: {currentlyOpen}, total opened: {totalOpened}, closed: {totalClosed})"
				: $"SSE ({Name}): {_message}");
		}

		public virtual int DefaultPermissionLevel () {
			return 0;
		}
	}
}