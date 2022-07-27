using System;
using System.IO;
using AllocsFixes.FileCache;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace AllocsFixes.MapRendering {
	public class MapRenderBlockBuffer {
		private readonly Texture2D blockMap = new Texture2D (Constants.MAP_BLOCK_SIZE, Constants.MAP_BLOCK_SIZE, Constants.DEFAULT_TEX_FORMAT, false);
		private readonly MapTileCache cache;
		private readonly NativeArray<int> emptyImageData;
		private readonly Texture2D zoomBuffer = new Texture2D (Constants.MAP_BLOCK_SIZE / 2, Constants.MAP_BLOCK_SIZE / 2, Constants.DEFAULT_TEX_FORMAT, false);
		private readonly int zoomLevel;
		private readonly string folderBase;
		
		private Vector2i currentBlockMapPos = new Vector2i (Int32.MinValue, Int32.MinValue);
		private string currentBlockMapFolder = string.Empty;

		public MapRenderBlockBuffer (int _level, MapTileCache _cache) {
			zoomLevel = _level;
			cache = _cache;
			folderBase = Constants.MAP_DIRECTORY + "/" + zoomLevel + "/";

			{
				// Initialize empty tile data
				Color nullColor = new Color (0, 0, 0, 0);
				for (int x = 0; x < Constants.MAP_BLOCK_SIZE; x++) {
					for (int y = 0; y < Constants.MAP_BLOCK_SIZE; y++) {
						blockMap.SetPixel (x, y, nullColor);
					}
				}

				NativeArray<int> blockMapData = blockMap.GetRawTextureData<int> ();
				emptyImageData = new NativeArray<int> (blockMapData.Length, Allocator.Persistent,
					NativeArrayOptions.UninitializedMemory);
				blockMapData.CopyTo (emptyImageData);
			}
		}

		public TextureFormat FormatSelf {
			get { return blockMap.format; }
		}

		public void ResetBlock () {
			currentBlockMapFolder = string.Empty;
			currentBlockMapPos = new Vector2i (Int32.MinValue, Int32.MinValue);
			cache.ResetTile (zoomLevel);
		}

		public void SaveBlock () {
			Profiler.BeginSample ("SaveBlock");
			try {
				saveTextureToFile ();
			} catch (Exception e) {
				Log.Warning ("Exception in MapRenderBlockBuffer.SaveBlock(): " + e);
			}
			Profiler.EndSample ();
		}

		public bool LoadBlock (Vector2i _block) {
			Profiler.BeginSample ("LoadBlock");
			lock (blockMap) {
				if (currentBlockMapPos != _block) {
					Profiler.BeginSample ("LoadBlock.Strings");
					string folder;
					if (currentBlockMapPos.x != _block.x) {
						folder = folderBase + _block.x + '/';

						Profiler.BeginSample ("LoadBlock.Directory");
						Directory.CreateDirectory (folder);
						Profiler.EndSample ();
					} else {
						folder = currentBlockMapFolder;
					}

					string fileName = folder + _block.y + ".png";
					Profiler.EndSample ();
					
					SaveBlock ();
					loadTextureFromFile (fileName);

					currentBlockMapFolder = folder;
					currentBlockMapPos = _block;

					Profiler.EndSample ();
					return true;
				}
			}

			Profiler.EndSample ();
			return false;
		}

		public void SetPart (Vector2i _offset, int _partSize, Color32[] _pixels) {
			if (_offset.x + _partSize > Constants.MAP_BLOCK_SIZE || _offset.y + _partSize > Constants.MAP_BLOCK_SIZE) {
				Log.Error (string.Format ("MapBlockBuffer[{0}].SetPart ({1}, {2}, {3}) has blockMap.size ({4}/{5})",
					zoomLevel, _offset, _partSize, _pixels.Length, Constants.MAP_BLOCK_SIZE, Constants.MAP_BLOCK_SIZE));
				return;
			}

			Profiler.BeginSample ("SetPart");
			blockMap.SetPixels32 (_offset.x, _offset.y, _partSize, _partSize, _pixels);
			Profiler.EndSample ();
		}

		public Color32[] GetHalfScaled () {
			Profiler.BeginSample ("HalfScaled.ResizeBuffer");
			zoomBuffer.Resize (Constants.MAP_BLOCK_SIZE, Constants.MAP_BLOCK_SIZE);
			Profiler.EndSample ();

			Profiler.BeginSample ("HalfScaled.CopyPixels");
			if (blockMap.format == zoomBuffer.format) {
				Profiler.BeginSample ("Native");
				NativeArray<byte> dataSrc = blockMap.GetRawTextureData<byte> ();
				NativeArray<byte> dataZoom = zoomBuffer.GetRawTextureData<byte> ();
				dataSrc.CopyTo (dataZoom);
				Profiler.EndSample ();
			} else {
				Profiler.BeginSample ("GetSetPixels");
				zoomBuffer.SetPixels32 (blockMap.GetPixels32 ());
				Profiler.EndSample ();
			}
			Profiler.EndSample ();

			Profiler.BeginSample ("HalfScaled.Scale");
			TextureScale.Point (zoomBuffer, Constants.MAP_BLOCK_SIZE / 2, Constants.MAP_BLOCK_SIZE / 2);
			Profiler.EndSample ();

			Profiler.BeginSample ("HalfScaled.Return");
			Color32[] result = zoomBuffer.GetPixels32 ();
			Profiler.EndSample ();

			return result;
		}

		public void SetPartNative (Vector2i _offset, int _partSize, NativeArray<int> _pixels) {
			if (_offset.x + _partSize > Constants.MAP_BLOCK_SIZE || _offset.y + _partSize > Constants.MAP_BLOCK_SIZE) {
				Log.Error (string.Format ("MapBlockBuffer[{0}].SetPart ({1}, {2}, {3}) has blockMap.size ({4}/{5})",
					zoomLevel, _offset, _partSize, _pixels.Length, Constants.MAP_BLOCK_SIZE, Constants.MAP_BLOCK_SIZE));
				return;
			}

			Profiler.BeginSample ("SetPartNative");
			NativeArray<int> destData = blockMap.GetRawTextureData<int> ();
			
			for (int y = 0; y < _partSize; y++) {
				int srcLineStartIdx = _partSize * y;
				int destLineStartIdx = blockMap.width * (_offset.y + y) + _offset.x;
				for (int x = 0; x < _partSize; x++) {
					destData [destLineStartIdx + x] = _pixels [srcLineStartIdx + x];
				}
			}
			Profiler.EndSample ();
		}

		public NativeArray<int> GetHalfScaledNative () {
			Profiler.BeginSample ("HalfScaledNative.ResizeBuffer");
			if (zoomBuffer.format != blockMap.format || zoomBuffer.height != Constants.MAP_BLOCK_SIZE / 2 || zoomBuffer.width != Constants.MAP_BLOCK_SIZE / 2) {
				zoomBuffer.Resize (Constants.MAP_BLOCK_SIZE / 2, Constants.MAP_BLOCK_SIZE / 2, blockMap.format, false);
			}
			Profiler.EndSample ();

			Profiler.BeginSample ("HalfScaledNative.Scale");
			ScaleNative (blockMap, zoomBuffer);
			Profiler.EndSample ();

			return zoomBuffer.GetRawTextureData<int> ();
		}
		
		private static void ScaleNative (Texture2D _sourceTex, Texture2D _targetTex) {
			NativeArray<int> srcData = _sourceTex.GetRawTextureData<int> ();
			NativeArray<int> targetData = _targetTex.GetRawTextureData<int> ();
			
			int oldWidth = _sourceTex.width;
			int oldHeight = _sourceTex.height;
			int newWidth = _targetTex.width;
			int newHeight = _targetTex.height;
			
			float ratioX = ((float) oldWidth) / newWidth;
			float ratioY = ((float) oldHeight) / newHeight;

			for (var y = 0; y < newHeight; y++) {
				var oldLineStart = (int) (ratioY * y) * oldWidth;
				var newLineStart = y * newWidth;
				for (var x = 0; x < newWidth; x++) {
					targetData [newLineStart + x] = srcData [(int) (oldLineStart + ratioX * x)];
				}
			}
		}

		private void loadTextureFromFile (string _fileName) {
			Profiler.BeginSample ("LoadTexture");

			Profiler.BeginSample ("LoadFile");
			byte[] array = cache.LoadTile (zoomLevel, _fileName);
			Profiler.EndSample ();

			Profiler.BeginSample ("LoadImage");
			if (array != null && blockMap.LoadImage (array) && blockMap.height == Constants.MAP_BLOCK_SIZE &&
			    blockMap.width == Constants.MAP_BLOCK_SIZE) {
				Profiler.EndSample ();

				Profiler.EndSample ();
				return;
			}
			Profiler.EndSample ();

			if (array != null) {
				Log.Error ("Map image tile " + _fileName + " has been corrupted, recreating tile");
			}

			if (blockMap.format != Constants.DEFAULT_TEX_FORMAT || blockMap.height != Constants.MAP_BLOCK_SIZE ||
			    blockMap.width != Constants.MAP_BLOCK_SIZE) {
				blockMap.Resize (Constants.MAP_BLOCK_SIZE, Constants.MAP_BLOCK_SIZE, Constants.DEFAULT_TEX_FORMAT,
					false);
			}

			blockMap.LoadRawTextureData (emptyImageData);

			Profiler.EndSample ();
		}

		private void saveTextureToFile () {
			Profiler.BeginSample ("EncodePNG");
			byte[] array = blockMap.EncodeToPNG ();
			Profiler.EndSample ();

			cache.SaveTile (zoomLevel, array);
		}
	}
}