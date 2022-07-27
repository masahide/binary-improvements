using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace AllocsFixes.CustomCommands {
	public class ListKnownPlayers : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "lists all players that were ever online";
		}

		public override string GetHelp () {
			return "Usage:\n" +
			       "  1. listknownplayers\n" +
			       "  2. listknownplayers -online\n" +
			       "  3. listknownplayers -notbanned\n" +
			       "  4. listknownplayers <player name / userid>\n" +
			       "1. Lists all players that have ever been online\n" +
			       "2. Lists only the players that are currently online\n" +
			       "3. Lists only the players that are not banned\n" +
			       "4. Lists all players whose name contains the given string or matches the given UserID";
		}

		public override string[] GetCommands () {
			return new[] {"listknownplayers", "lkp"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			AdminTools admTools = GameManager.Instance.adminTools;

			bool onlineOnly = false;
			bool notBannedOnly = false;
			string nameFilter = string.Empty;
			PlatformUserIdentifierAbs userIdFilter = null;

			if (_params.Count == 1) {
				if (_params [0].EqualsCaseInsensitive ("-online")) {
					onlineOnly = true;
				} else if (_params [0].EqualsCaseInsensitive ("-notbanned")) {
					notBannedOnly = true;
				} else if (PlatformUserIdentifierAbs.TryFromCombinedString (_params [0], out userIdFilter)) {
					// if true nothing to do, set by the out parameter
				} else {
					nameFilter = _params [0];
				}
			}

			if (userIdFilter != null) {
				Player p = PersistentContainer.Instance.Players [userIdFilter, false];

				if (p != null) {
					SdtdConsole.Instance.Output (
						$"{0}. {p.Name}, id={p.EntityID}, steamid={_params [0]}, online={p.IsOnline}, ip={p.IP}, playtime={p.TotalPlayTime / 60} m, seen={p.LastOnline:yyyy-MM-dd HH:mm}"
					);
				} else {
					SdtdConsole.Instance.Output ($"SteamID {_params [0]} unknown!");
				}
			} else {
				int num = 0;
				foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in PersistentContainer.Instance.Players.Dict) {
					Player p = kvp.Value;

					if (
						(!onlineOnly || p.IsOnline)
						&& (!notBannedOnly || !admTools.IsBanned (kvp.Key, out _, out _))
						&& (nameFilter.Length == 0 || p.Name.ContainsCaseInsensitive (nameFilter))
					) {
						SdtdConsole.Instance.Output (
							$"{++num}. {p.Name}, id={p.EntityID}, steamid={kvp.Key}, online={p.IsOnline}, ip={p.IP}, playtime={p.TotalPlayTime / 60} m, seen={p.LastOnline:yyyy-MM-dd HH:mm}"
						);
					}
				}

				SdtdConsole.Instance.Output ($"Total of {PersistentContainer.Instance.Players.Count} known");
			}
		}
	}
}