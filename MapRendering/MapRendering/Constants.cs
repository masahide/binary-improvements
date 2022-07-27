using UnityEngine;

namespace AllocsFixes.MapRendering {
	public class Constants {
		public static readonly TextureFormat DEFAULT_TEX_FORMAT = TextureFormat.ARGB32;
		public static int MAP_BLOCK_SIZE = 128;
		public const int MAP_CHUNK_SIZE = 16;
		public const int MAP_REGION_SIZE = 512;
		public static int ZOOMLEVELS = 5;
		public static string MAP_DIRECTORY = string.Empty;

		public static int MAP_BLOCK_TO_CHUNK_DIV {
			get { return MAP_BLOCK_SIZE / MAP_CHUNK_SIZE; }
		}

		public static int MAP_REGION_TO_CHUNK_DIV {
			get { return MAP_REGION_SIZE / MAP_CHUNK_SIZE; }
		}
	}
}