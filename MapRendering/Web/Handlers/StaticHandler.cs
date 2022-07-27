using System.IO;
using System.Net;
using AllocsFixes.FileCache;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class StaticHandler : PathHandler {
		private readonly AbstractCache cache;
		private readonly string datapath;
		private readonly bool logMissingFiles;

		public StaticHandler (string _filePath, AbstractCache _cache, bool _logMissingFiles,
			string _moduleName = null) : base (_moduleName) {
			datapath = _filePath + (_filePath [_filePath.Length - 1] == '/' ? "" : "/");
			cache = _cache;
			logMissingFiles = _logMissingFiles;
		}

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			string fn = _req.Url.AbsolutePath.Remove (0, urlBasePath.Length);

			byte[] content = cache.GetFileContent (datapath + fn);

			if (content != null) {
				_resp.ContentType = MimeType.GetMimeType (Path.GetExtension (fn));
				_resp.ContentLength64 = content.Length;
				_resp.OutputStream.Write (content, 0, content.Length);
			} else {
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				if (logMissingFiles) {
					Log.Out ("Web:Static:FileNotFound: \"" + _req.Url.AbsolutePath + "\" @ \"" + datapath + fn + "\"");
				}
			}
		}
	}
}