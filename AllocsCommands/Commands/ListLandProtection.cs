using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace AllocsFixes.CustomCommands {
	public class ListLandProtection : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "lists all land protection blocks and owners";
		}

		public override string GetHelp () {
			return "Usage:\n" +
			       "  1. listlandprotection summary\n" +
			       "  2. listlandprotection <user id / player name / entity id> [parseable]\n" +
			       "  3. listlandprotection nearby [length]\n" +
			       "1. Lists only players that own claimstones, the number they own and the protection status\n" +
			       "2. Lists only the claims of the player given by his UserID / entity id / playername, including the individual claim positions.\n" +
			       "   If \"parseable\" is specified the output of the individual claims will be in a format better suited for programmatical readout.\n" +
			       "3. Lists claims in a square with edge length of 64 (or the optionally specified size) around the executing player\n";
		}

		public override string[] GetCommands () {
			return new[] {"listlandprotection", "llp"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count >= 1 && _params [0].EqualsCaseInsensitive ("nearby")) {
				if (_senderInfo.RemoteClientInfo != null) {
					_params.Add (_senderInfo.RemoteClientInfo.entityId.ToString ());
				} else if (_senderInfo.IsLocalGame && !GameManager.IsDedicatedServer) {
					_params.Add (GameManager.Instance.World.GetPrimaryPlayerId ().ToString ());
				}
			}

			World w = GameManager.Instance.World;
			PersistentPlayerList ppl = GameManager.Instance.GetPersistentPlayerList ();

			bool summaryOnly = false;
			PlatformUserIdentifierAbs userIdFilter = null;
			Vector3i closeTo = default (Vector3i);
			bool onlyCloseToPlayer = false;
			int closeToDistance = 32;
			bool parseableOutput = false;

			if (_params.Contains ("parseable")) {
				parseableOutput = true;
				_params.Remove ("parseable");
			}

			if (_params.Count == 1) {
				if (_params [0].EqualsCaseInsensitive ("summary")) {
					summaryOnly = true;
				} else if (PlatformUserIdentifierAbs.TryFromCombinedString (_params[0], out userIdFilter)) {
				} else {
					ClientInfo ci = ConsoleHelper.ParseParamIdOrName (_params [0]);
					if (ci != null) {
						userIdFilter = ci.InternalId;
					} else {
						SdtdConsole.Instance.Output ("Player name or entity id \"" + _params [0] + "\" not found.");
						return;
					}
				}
			} else if (_params.Count >= 2) {
				if (_params [0].EqualsCaseInsensitive ("nearby")) {
					try {
						if (_params.Count == 3) {
							if (!int.TryParse (_params [1], out closeToDistance)) {
								SdtdConsole.Instance.Output ("Given length is not an integer!");
								return;
							}

							closeToDistance /= 2;
						}

						int entityId = int.Parse (_params [_params.Count - 1]);
						EntityPlayer ep = w.Players.dict [entityId];
						closeTo = new Vector3i (ep.GetPosition ());
						onlyCloseToPlayer = true;
					} catch (Exception e) {
						SdtdConsole.Instance.Output ("Error getting current player's position");
						Log.Out ("Error in ListLandProtection.Run: " + e);
						return;
					}
				} else {
					SdtdConsole.Instance.Output ("Illegal parameter list");
					return;
				}
			}


			LandClaimList.OwnerFilter[] ownerFilters = null;
			if (userIdFilter != null) {
				ownerFilters = new[] {LandClaimList.UserIdFilter (userIdFilter)};
			}

			LandClaimList.PositionFilter[] posFilters = null;
			if (onlyCloseToPlayer) {
				posFilters = new[] {LandClaimList.CloseToFilter2dRect (closeTo, closeToDistance)};
			}

			Dictionary<Player, List<Vector3i>> claims = LandClaimList.GetLandClaims (ownerFilters, posFilters);

			foreach (KeyValuePair<Player, List<Vector3i>> kvp in claims) {
				SdtdConsole.Instance.Output (string.Format (
					"Player \"{0} ({1})\" owns {4} keystones (protected: {2}, current hardness multiplier: {3})",
					kvp.Key.Name,
					kvp.Key.PlatformId,
					kvp.Key.LandProtectionActive,
					kvp.Key.LandProtectionMultiplier,
					kvp.Value.Count));
				if (!summaryOnly) {
					foreach (Vector3i v in kvp.Value) {
						if (parseableOutput) {
							SdtdConsole.Instance.Output ("LandProtectionOf: id=" + kvp.Key.PlatformId +
							                             ", playerName=" + kvp.Key.Name + ", location=" + v);
						} else {
							SdtdConsole.Instance.Output ("   (" + v + ")");
						}
					}
				}
			}

			if (userIdFilter == null) {
				SdtdConsole.Instance.Output ("Total of " + ppl.m_lpBlockMap.Count + " keystones in the game");
			}
		}
	}
}