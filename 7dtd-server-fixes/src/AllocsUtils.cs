using UnityEngine;

namespace AllocsFixes {
	public static class AllocsUtils {
		public static string ColorToHex (Color _color) {
			return string.Format ("{0:X02}{1:X02}{2:X02}", (int) (_color.r * 255), (int) (_color.g * 255),
				(int) (_color.b * 255));
		}
	}
}