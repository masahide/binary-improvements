using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using AllocsFixes.FileCache;
using AllocsFixes.JSON;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace AllocsFixes.MapRendering {
	public class MapRendering {
		private static MapRendering instance;

		private static readonly object lockObject = new object ();
		public static bool renderingEnabled = true;
		private readonly MapTileCache cache = new MapTileCache (Constants.MAP_BLOCK_SIZE);
		private readonly Dictionary<Vector2i, Color32[]> dirtyChunks = new Dictionary<Vector2i, Color32[]> ();
		private readonly MicroStopwatch msw = new MicroStopwatch ();
		private readonly MapRenderBlockBuffer[] zoomLevelBuffers;
		private Coroutine renderCoroutineRef;
		private bool renderingFullMap;
		private float renderTimeout = float.MaxValue;

		private MapRendering () {
			Constants.MAP_DIRECTORY = GameIO.GetSaveGameDir () + "/map";

			lock (lockObject) {
				if (!LoadMapInfo ()) {
					WriteMapInfo ();
				}
			}

			cache.SetZoomCount (Constants.ZOOMLEVELS);

			zoomLevelBuffers = new MapRenderBlockBuffer[Constants.ZOOMLEVELS];
			for (int i = 0; i < Constants.ZOOMLEVELS; i++) {
				zoomLevelBuffers [i] = new MapRenderBlockBuffer (i, cache);
			}

			renderCoroutineRef = ThreadManager.StartCoroutine (renderCoroutine ());
		}

		public static MapRendering Instance {
			get {
				if (instance == null) {
					instance = new MapRendering ();
				}

				return instance;
			}
		}

		public static MapTileCache GetTileCache () {
			return Instance.cache;
		}

		public static void Shutdown () {
			if (Instance?.renderCoroutineRef != null) {
				ThreadManager.StopCoroutine (Instance.renderCoroutineRef);
				Instance.renderCoroutineRef = null;
			}
		}

		public static void RenderSingleChunk (Chunk _chunk) {
			if (renderingEnabled && Instance != null) {
				// TODO: Replace with regular thread and a blocking queue / set
				ThreadPool.UnsafeQueueUserWorkItem (_o => {
					try {
						if (!Instance.renderingFullMap) {
							lock (lockObject) {
								Chunk c = (Chunk) _o;
								Vector3i cPos = c.GetWorldPos ();
								Vector2i cPos2 = new Vector2i (cPos.x / Constants.MAP_CHUNK_SIZE,
									cPos.z / Constants.MAP_CHUNK_SIZE);

								ushort[] mapColors = c.GetMapColors ();
								if (mapColors != null) {
									Color32[] realColors =
										new Color32[Constants.MAP_CHUNK_SIZE * Constants.MAP_CHUNK_SIZE];
									for (int i_colors = 0; i_colors < mapColors.Length; i_colors++) {
										realColors [i_colors] = shortColorToColor32 (mapColors [i_colors]);
									}

									Instance.dirtyChunks [cPos2] = realColors;

									//Log.Out ("Add Dirty: " + cPos2);
								}
							}
						}
					} catch (Exception e) {
						Log.Out ("Exception in MapRendering.RenderSingleChunk(): " + e);
					}
				}, _chunk);
			}
		}

		public void RenderFullMap () {
			MicroStopwatch microStopwatch = new MicroStopwatch ();

			string regionSaveDir = GameIO.GetSaveGameRegionDir ();
			RegionFileManager rfm = new RegionFileManager (regionSaveDir, regionSaveDir, 0, false);
			Texture2D fullMapTexture = null;

			Vector2i minChunk, maxChunk;
			Vector2i minPos, maxPos;
			int widthChunks, heightChunks, widthPix, heightPix;
			getWorldExtent (rfm, out minChunk, out maxChunk, out minPos, out maxPos, out widthChunks, out heightChunks,
				out widthPix, out heightPix);

			Log.Out (string.Format (
				"RenderMap: min: {0}, max: {1}, minPos: {2}, maxPos: {3}, w/h: {4}/{5}, wP/hP: {6}/{7}",
				minChunk.ToString (), maxChunk.ToString (),
				minPos.ToString (), maxPos.ToString (),
				widthChunks, heightChunks,
				widthPix, heightPix)
			);

			lock (lockObject) {
				for (int i = 0; i < Constants.ZOOMLEVELS; i++) {
					zoomLevelBuffers [i].ResetBlock ();
				}

				if (Directory.Exists (Constants.MAP_DIRECTORY)) {
					Directory.Delete (Constants.MAP_DIRECTORY, true);
				}

				WriteMapInfo ();

				renderingFullMap = true;

				if (widthPix <= 8192 && heightPix <= 8192) {
					fullMapTexture = new Texture2D (widthPix, heightPix);
				}

				Vector2i curFullMapPos = default (Vector2i);
				Vector2i curChunkPos = default (Vector2i);
				for (curFullMapPos.x = 0; curFullMapPos.x < widthPix; curFullMapPos.x += Constants.MAP_CHUNK_SIZE) {
					for (curFullMapPos.y = 0;
						curFullMapPos.y < heightPix;
						curFullMapPos.y += Constants.MAP_CHUNK_SIZE) {
						curChunkPos.x = curFullMapPos.x / Constants.MAP_CHUNK_SIZE + minChunk.x;
						curChunkPos.y = curFullMapPos.y / Constants.MAP_CHUNK_SIZE + minChunk.y;

						try {
							long chunkKey = WorldChunkCache.MakeChunkKey (curChunkPos.x, curChunkPos.y);
							if (rfm.ContainsChunkSync (chunkKey)) {
								Chunk c = rfm.GetChunkSync (chunkKey);
								ushort[] mapColors = c.GetMapColors ();
								if (mapColors != null) {
									Color32[] realColors =
										new Color32[Constants.MAP_CHUNK_SIZE * Constants.MAP_CHUNK_SIZE];
									for (int i_colors = 0; i_colors < mapColors.Length; i_colors++) {
										realColors [i_colors] = shortColorToColor32 (mapColors [i_colors]);
									}

									dirtyChunks [curChunkPos] = realColors;
									if (fullMapTexture != null) {
										fullMapTexture.SetPixels32 (curFullMapPos.x, curFullMapPos.y,
											Constants.MAP_CHUNK_SIZE, Constants.MAP_CHUNK_SIZE, realColors);
									}
								}
							}
						} catch (Exception e) {
							Log.Out ("Exception: " + e);
						}
					}

					while (dirtyChunks.Count > 0) {
						RenderDirtyChunks ();
					}

					Log.Out (string.Format ("RenderMap: {0}/{1} ({2}%)", curFullMapPos.x, widthPix,
						(int) ((float) curFullMapPos.x / widthPix * 100)));
				}
			}
			
			rfm.Cleanup ();

			if (fullMapTexture != null) {
				byte[] array = fullMapTexture.EncodeToPNG ();
				File.WriteAllBytes (Constants.MAP_DIRECTORY + "/map.png", array);
				Object.Destroy (fullMapTexture);
			}

			renderingFullMap = false;

			Log.Out ("Generating map took: " + microStopwatch.ElapsedMilliseconds + " ms");
			Log.Out ("World extent: " + minPos + " - " + maxPos);
		}

		private void SaveAllBlockMaps () {
			for (int i = 0; i < Constants.ZOOMLEVELS; i++) {
				zoomLevelBuffers [i].SaveBlock ();
			}
		}
		
		private readonly WaitForSeconds coroutineDelay = new WaitForSeconds (0.2f);

		private IEnumerator renderCoroutine () {
			while (true) {
				lock (lockObject) {
					if (dirtyChunks.Count > 0 && renderTimeout == float.MaxValue) {
						renderTimeout = Time.time + 0.5f;
					}

					if (Time.time > renderTimeout || dirtyChunks.Count > 200) {
						Profiler.BeginSample ("RenderDirtyChunks");
						RenderDirtyChunks ();
						Profiler.EndSample ();
					}
				}

				yield return coroutineDelay;
			}
		}

		private readonly List<Vector2i> chunksToRender = new List<Vector2i> ();
		private readonly List<Vector2i> chunksRendered = new List<Vector2i> ();

		private void RenderDirtyChunks () {
			msw.ResetAndRestart ();

			if (dirtyChunks.Count <= 0) {
				return;
			}

			Profiler.BeginSample ("RenderDirtyChunks.Prepare");
			chunksToRender.Clear ();
			chunksRendered.Clear ();

			dirtyChunks.CopyKeysTo (chunksToRender);

			Vector2i chunkPos = chunksToRender [0];
			chunksRendered.Add (chunkPos);

			//Log.Out ("Start Dirty: " + chunkPos);

			Vector2i block, blockOffset;
			getBlockNumber (chunkPos, out block, out blockOffset, Constants.MAP_BLOCK_TO_CHUNK_DIV,
				Constants.MAP_CHUNK_SIZE);

			zoomLevelBuffers [Constants.ZOOMLEVELS - 1].LoadBlock (block);
			Profiler.EndSample ();

			Profiler.BeginSample ("RenderDirtyChunks.Work");
			// Write all chunks that are in the same image tile of the highest zoom level 
			Vector2i v_block, v_blockOffset;
			foreach (Vector2i v in chunksToRender) {
				getBlockNumber (v, out v_block, out v_blockOffset, Constants.MAP_BLOCK_TO_CHUNK_DIV,
					Constants.MAP_CHUNK_SIZE);
				if (v_block.Equals (block)) {
					//Log.Out ("Dirty: " + v + " render: true");
					chunksRendered.Add (v);
					if (dirtyChunks [v].Length != Constants.MAP_CHUNK_SIZE * Constants.MAP_CHUNK_SIZE) {
						Log.Error (string.Format ("Rendering chunk has incorrect data size of {0} instead of {1}",
							dirtyChunks [v].Length, Constants.MAP_CHUNK_SIZE * Constants.MAP_CHUNK_SIZE));
					}

					zoomLevelBuffers [Constants.ZOOMLEVELS - 1]
						.SetPart (v_blockOffset, Constants.MAP_CHUNK_SIZE, dirtyChunks [v]);
				}
			}
			Profiler.EndSample ();

			foreach (Vector2i v in chunksRendered) {
				dirtyChunks.Remove (v);
			}

			// Update lower zoom levels affected by the change of the highest one
			RenderZoomLevel (block);

			Profiler.BeginSample ("RenderDirtyChunks.SaveAll");
			SaveAllBlockMaps ();
			Profiler.EndSample ();
		}

		private void RenderZoomLevel (Vector2i _innerBlock) {
			Profiler.BeginSample ("RenderZoomLevel");
			int level = Constants.ZOOMLEVELS - 1;
			while (level > 0) {
				Vector2i block, blockOffset;
				getBlockNumber (_innerBlock, out block, out blockOffset, 2, Constants.MAP_BLOCK_SIZE / 2);

				zoomLevelBuffers [level - 1].LoadBlock (block);

				Profiler.BeginSample ("RenderZoomLevel.Transfer");
				if ((zoomLevelBuffers [level].FormatSelf == TextureFormat.ARGB32 ||
				     zoomLevelBuffers [level].FormatSelf == TextureFormat.RGBA32) &&
				    zoomLevelBuffers [level].FormatSelf == zoomLevelBuffers [level - 1].FormatSelf) {
					zoomLevelBuffers [level - 1].SetPartNative (blockOffset, Constants.MAP_BLOCK_SIZE / 2, zoomLevelBuffers [level].GetHalfScaledNative ());
				} else {
					zoomLevelBuffers [level - 1].SetPart (blockOffset, Constants.MAP_BLOCK_SIZE / 2, zoomLevelBuffers [level].GetHalfScaled ());
				}
				Profiler.EndSample ();

				level--;
				_innerBlock = block;
			}
			Profiler.EndSample ();
		}

		private void getBlockNumber (Vector2i _innerPos, out Vector2i _block, out Vector2i _blockOffset, int _scaleFactor,
			int _offsetSize) {
			_block = default (Vector2i);
			_blockOffset = default (Vector2i);
			_block.x = (_innerPos.x + 16777216) / _scaleFactor - 16777216 / _scaleFactor;
			_block.y = (_innerPos.y + 16777216) / _scaleFactor - 16777216 / _scaleFactor;
			_blockOffset.x = (_innerPos.x + 16777216) % _scaleFactor * _offsetSize;
			_blockOffset.y = (_innerPos.y + 16777216) % _scaleFactor * _offsetSize;
		}

		private void WriteMapInfo () {
			JSONObject mapInfo = new JSONObject ();
			mapInfo.Add ("blockSize", new JSONNumber (Constants.MAP_BLOCK_SIZE));
			mapInfo.Add ("maxZoom", new JSONNumber (Constants.ZOOMLEVELS - 1));

			Directory.CreateDirectory (Constants.MAP_DIRECTORY);
			File.WriteAllText (Constants.MAP_DIRECTORY + "/mapinfo.json", mapInfo.ToString (), Encoding.UTF8);
		}

		private bool LoadMapInfo () {
			if (!File.Exists (Constants.MAP_DIRECTORY + "/mapinfo.json")) {
				return false;
			}

			string json = File.ReadAllText (Constants.MAP_DIRECTORY + "/mapinfo.json", Encoding.UTF8);
			try {
				JSONNode node = Parser.Parse (json);
				if (node is JSONObject) {
					JSONObject jo = (JSONObject) node;
					if (jo.ContainsKey ("blockSize")) {
						Constants.MAP_BLOCK_SIZE = ((JSONNumber) jo ["blockSize"]).GetInt ();
					}

					if (jo.ContainsKey ("maxZoom")) {
						Constants.ZOOMLEVELS = ((JSONNumber) jo ["maxZoom"]).GetInt () + 1;
					}

					return true;
				}
			} catch (MalformedJSONException e) {
				Log.Out ("Exception in LoadMapInfo: " + e);
			} catch (InvalidCastException e) {
				Log.Out ("Exception in LoadMapInfo: " + e);
			}

			return false;
		}

		private void getWorldExtent (RegionFileManager _rfm, out Vector2i _minChunk, out Vector2i _maxChunk,
			out Vector2i _minPos, out Vector2i _maxPos,
			out int _widthChunks, out int _heightChunks,
			out int _widthPix, out int _heightPix) {
			_minChunk = default (Vector2i);
			_maxChunk = default (Vector2i);
			_minPos = default (Vector2i);
			_maxPos = default (Vector2i);

			long[] keys = _rfm.GetAllChunkKeys ();
			int minX = int.MaxValue;
			int minY = int.MaxValue;
			int maxX = int.MinValue;
			int maxY = int.MinValue;
			foreach (long key in keys) {
				int x = WorldChunkCache.extractX (key);
				int y = WorldChunkCache.extractZ (key);

				if (x < minX) {
					minX = x;
				}

				if (x > maxX) {
					maxX = x;
				}

				if (y < minY) {
					minY = y;
				}

				if (y > maxY) {
					maxY = y;
				}
			}

			_minChunk.x = minX;
			_minChunk.y = minY;

			_maxChunk.x = maxX;
			_maxChunk.y = maxY;

			_minPos.x = minX * Constants.MAP_CHUNK_SIZE;
			_minPos.y = minY * Constants.MAP_CHUNK_SIZE;

			_maxPos.x = maxX * Constants.MAP_CHUNK_SIZE;
			_maxPos.y = maxY * Constants.MAP_CHUNK_SIZE;

			_widthChunks = maxX - minX + 1;
			_heightChunks = maxY - minY + 1;

			_widthPix = _widthChunks * Constants.MAP_CHUNK_SIZE;
			_heightPix = _heightChunks * Constants.MAP_CHUNK_SIZE;
		}

		private static Color32 shortColorToColor32 (ushort _col) {
			byte r = (byte) (256 * ((_col >> 10) & 31) / 32);
			byte g = (byte) (256 * ((_col >> 5) & 31) / 32);
			byte b = (byte) (256 * (_col & 31) / 32);
			const byte a = 255;
			return new Color32 (r, g, b, a);
		}
	}
}