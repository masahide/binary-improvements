using System.Net;
using System.Text;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class Null : WebAPI {
		public Null (string _name) : base(_name) {
		}
		
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			_resp.ContentLength64 = 0;
			_resp.ContentType = "text/plain";
			_resp.ContentEncoding = Encoding.ASCII;
			_resp.OutputStream.Write (new byte[] { }, 0, 0);
		}
	}
}