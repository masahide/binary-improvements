using System;
using System.Collections.Generic;
using System.IO;

namespace AllocsFixes.FileCache {
	// Caching all files, useful for completely static folders only
	public class SimpleCache : AbstractCache {
		private readonly Dictionary<string, byte[]> fileCache = new Dictionary<string, byte[]> ();

		public override byte[] GetFileContent (string _filename) {
			try {
				lock (fileCache) {
					if (!fileCache.ContainsKey (_filename)) {
						if (!File.Exists (_filename)) {
							return null;
						}

						fileCache.Add (_filename, File.ReadAllBytes (_filename));
					}

					return fileCache [_filename];
				}
			} catch (Exception e) {
				Log.Out ("Error in SimpleCache.GetFileContent: " + e);
			}

			return null;
		}
	}
}