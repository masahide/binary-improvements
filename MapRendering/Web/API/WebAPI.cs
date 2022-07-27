using System.Net;
using System.Text;
using AllocsFixes.JSON;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public abstract class WebAPI {
		public readonly string Name;

		protected WebAPI (string _name = null) {
			Name = _name ?? GetType ().Name;
		}

#if ENABLE_PROFILER
		private static readonly UnityEngine.Profiling.CustomSampler jsonSerializeSampler = UnityEngine.Profiling.CustomSampler.Create ("JSON_Serialize");
		private static readonly UnityEngine.Profiling.CustomSampler netWriteSampler = UnityEngine.Profiling.CustomSampler.Create ("JSON_Write");
#endif

		public static void WriteJSON (HttpListenerResponse _resp, JSONNode _root) {
#if ENABLE_PROFILER
			jsonSerializeSampler.Begin ();
#endif
			StringBuilder sb = new StringBuilder ();
			_root.ToString (sb);
#if ENABLE_PROFILER
			jsonSerializeSampler.End ();
			netWriteSampler.Begin ();
#endif
			byte[] buf = Encoding.UTF8.GetBytes (sb.ToString ());
			_resp.ContentLength64 = buf.Length;
			_resp.ContentType = "application/json";
			_resp.ContentEncoding = Encoding.UTF8;
			_resp.OutputStream.Write (buf, 0, buf.Length);
#if ENABLE_PROFILER
			netWriteSampler.End ();
#endif
		}

		public static void WriteText (HttpListenerResponse _resp, string _text) {
			byte[] buf = Encoding.UTF8.GetBytes (_text);
			_resp.ContentLength64 = buf.Length;
			_resp.ContentType = "text/plain";
			_resp.ContentEncoding = Encoding.UTF8;
			_resp.OutputStream.Write (buf, 0, buf.Length);
		}

		public abstract void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel);

		public virtual int DefaultPermissionLevel () {
			return 0;
		}
	}
}