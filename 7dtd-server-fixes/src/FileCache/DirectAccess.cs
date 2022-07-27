using System;
using System.IO;

namespace AllocsFixes.FileCache {
	// Not caching at all, simply reading from disk on each request
	public class DirectAccess : AbstractCache {
		public override byte[] GetFileContent (string _filename) {
			try {
				if (!File.Exists (_filename)) {
					return null;
				}

				return File.ReadAllBytes (_filename);
			} catch (Exception e) {
				Log.Out ("Error in DirectAccess.GetFileContent: " + e);
			}

			return null;
		}
	}
}