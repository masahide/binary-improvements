using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace AllocsFixes.FileCache {
	// Special "cache" for map tile folder as both map rendering and webserver access files in there.
	// Only map rendering tiles are cached. Writing is done by WriteThrough.
	public class MapTileCache : AbstractCache {
		private readonly byte[] transparentTile;
		private CurrentZoomFile[] cache;

		public MapTileCache (int _tileSize) {
			Texture2D tex = new Texture2D (_tileSize, _tileSize);
			Color nullColor = new Color (0, 0, 0, 0);
			for (int x = 0; x < _tileSize; x++) {
				for (int y = 0; y < _tileSize; y++) {
					tex.SetPixel (x, y, nullColor);
				}
			}

			transparentTile = tex.EncodeToPNG ();
			Object.Destroy (tex);
		}

		public void SetZoomCount (int _count) {
			cache = new CurrentZoomFile[_count];
			for (int i = 0; i < cache.Length; i++) {
				cache [i] = new CurrentZoomFile ();
			}
		}

		public byte[] LoadTile (int _zoomlevel, string _filename) {
			try {
				lock (cache) {
					CurrentZoomFile cacheEntry = cache [_zoomlevel];
					
					if (cacheEntry.filename == null || !cacheEntry.filename.Equals (_filename)) {
						cacheEntry.filename = _filename;

						if (!File.Exists (_filename)) {
							cacheEntry.pngData = null;
							return null;
						}

						Profiler.BeginSample ("ReadPng");
						cacheEntry.pngData = ReadAllBytes (_filename);
						Profiler.EndSample ();
					}

					return cacheEntry.pngData;
				}
			} catch (Exception e) {
				Log.Warning ("Error in MapTileCache.LoadTile: " + e);
			}

			return null;
		}

		public void SaveTile (int _zoomlevel, byte[] _contentPng) {
			try {
				lock (cache) {
					CurrentZoomFile cacheEntry = cache [_zoomlevel];

					string file = cacheEntry.filename;
					if (string.IsNullOrEmpty (file)) {
						return;
					}
					
					cacheEntry.pngData = _contentPng;

					Profiler.BeginSample ("WritePng");
					using (Stream stream = new FileStream (file, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
						4096)) {
						stream.Write (_contentPng, 0, _contentPng.Length);
					}
					Profiler.EndSample ();
				}
			} catch (Exception e) {
				Log.Warning ("Error in MapTileCache.SaveTile: " + e);
			}
		}

		public void ResetTile (int _zoomlevel) {
			try {
				lock (cache) {
					cache [_zoomlevel].filename = null;
					cache [_zoomlevel].pngData = null;
				}
			} catch (Exception e) {
				Log.Warning ("Error in MapTileCache.ResetTile: " + e);
			}
		}

		public override byte[] GetFileContent (string _filename) {
			try {
				lock (cache) {
					foreach (CurrentZoomFile czf in cache) {
						if (czf.filename != null && czf.filename.Equals (_filename)) {
							return czf.pngData;
						}
					}

					if (!File.Exists (_filename)) {
						return transparentTile;
					}

					return ReadAllBytes (_filename);
				}
			} catch (Exception e) {
				Log.Warning ("Error in MapTileCache.GetFileContent: " + e);
			}

			return null;
		}

		private static byte[] ReadAllBytes (string _path) {
			using (FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096)) {
				int bytesRead = 0;
				int bytesLeft = (int) fileStream.Length;
				byte[] result = new byte[bytesLeft];
				while (bytesLeft > 0) {
					int readThisTime = fileStream.Read (result, bytesRead, bytesLeft);
					if (readThisTime == 0) {
						throw new IOException ("Unexpected end of stream");
					}

					bytesRead += readThisTime;
					bytesLeft -= readThisTime;
				}

				return result;
			}
		}


		private class CurrentZoomFile {
			public string filename;
			public byte[] pngData;
		}
	}
}