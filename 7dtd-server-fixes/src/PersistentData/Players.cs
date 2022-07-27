using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Platform.Steam;

namespace AllocsFixes.PersistentData {
	[Serializable]
	public class Players {
		public readonly Dictionary<PlatformUserIdentifierAbs, Player> Dict = new Dictionary<PlatformUserIdentifierAbs, Player> ();

		public Player this [PlatformUserIdentifierAbs _platformId, bool _create] {
			get {
				if (_platformId == null) {
					return null;
				}

				if (Dict.TryGetValue (_platformId, out Player pOld)) {
					return pOld;
				}

				if (!_create) {
					return null;
				}

				Log.Out ("Created new player entry for ID: " + _platformId);
				Player p = new Player (_platformId);
				Dict.Add (_platformId, p);
				return p;
			}
		}

		public int Count => Dict.Count;

		public PlatformUserIdentifierAbs GetSteamID (string _nameOrId, bool _ignoreColorCodes) {
			if (string.IsNullOrEmpty (_nameOrId)) {
				return null;
			}

			if (PlatformUserIdentifierAbs.TryFromCombinedString (_nameOrId, out PlatformUserIdentifierAbs userId)) {
				return userId;
			}

			if (int.TryParse (_nameOrId, out int entityId)) {
				foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in Dict) {
					if (kvp.Value.IsOnline && kvp.Value.EntityID == entityId) {
						return kvp.Key;
					}
				}
			}

			foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in Dict) {
				string name = kvp.Value.Name;
				if (_ignoreColorCodes) {
					name = Regex.Replace (name, "\\[[0-9a-fA-F]{6}\\]", "");
				}

				if (kvp.Value.IsOnline && name.EqualsCaseInsensitive (_nameOrId)) {
					return kvp.Key;
				}
			}

			return null;
		}
	}
}