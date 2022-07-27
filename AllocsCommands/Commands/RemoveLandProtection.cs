using System;
using System.Collections.Generic;
using AllocsFixes.PersistentData;

namespace AllocsFixes.CustomCommands {
	public class RemoveLandProtection : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "removes the association of a land protection block to the owner";
		}

		public override string GetHelp () {
			return "Usage:" +
			       "  1. removelandprotection <userid>\n" +
			       "  2. removelandprotection <x> <y> <z>\n" +
			       "  3. removelandprotection nearby [length]\n" +
			       "1. Remove all land claims owned by the user with the given UserID\n" +
			       "2. Remove only the claim block on the exactly given block position\n" +
			       "3. Remove all claims in a square with edge length of 64 (or the optionally specified size) around the executing player";
		}

		public override string[] GetCommands () {
			return new[] {"removelandprotection", "rlp"};
		}

		private void removeById (string _id) {
			try {
				if (!PlatformUserIdentifierAbs.TryFromCombinedString (_id, out PlatformUserIdentifierAbs userId)) {
					SdtdConsole.Instance.Output (
						"Not a valid Steam ID or user has never logged on. Use \"listlandprotection\" to get a list of keystones.");
					return;
				}
				
				PersistentPlayerList ppl = GameManager.Instance.GetPersistentPlayerList ();

				if (ppl.Players [userId].LPBlocks == null || ppl.Players [userId].LPBlocks.Count == 0) {
					SdtdConsole.Instance.Output (
						"Player does not own any keystones. Use \"listlandprotection\" to get a list of keystones.");
					return;
				}

				List<BlockChangeInfo> changes = new List<BlockChangeInfo> ();
				foreach (Vector3i pos in ppl.Players [userId].LPBlocks) {
					BlockChangeInfo bci = new BlockChangeInfo (pos, new BlockValue (0), true, false);
					changes.Add (bci);
				}

				GameManager.Instance.SetBlocksRPC (changes);

				SdtdConsole.Instance.Output ("Tried to remove #" + changes.Count +
				                             " land protection blocks for player \"" + _id + "\". Note " +
				                             "that only blocks in chunks that are currently loaded (close to any player) could be removed. " +
				                             "Please check for remaining blocks by running:");
				SdtdConsole.Instance.Output ("  listlandprotection " + _id);
			} catch (Exception e) {
				Log.Out ("Error in RemoveLandProtection.removeById: " + e);
			}
		}

		private void removeByPosition (List<string> _coords) {
			int.TryParse (_coords [0], out int x);
			int.TryParse (_coords [1], out int y);
			int.TryParse (_coords [2], out int z);

			if (x == int.MinValue || y == int.MinValue || z == int.MinValue) {
				SdtdConsole.Instance.Output ("At least one of the given coordinates is not a valid integer");
				return;
			}

			Vector3i v = new Vector3i (x, y, z);

			PersistentPlayerList ppl = GameManager.Instance.GetPersistentPlayerList ();

			Dictionary<Vector3i, PersistentPlayerData> d = ppl.m_lpBlockMap;
			if (d == null || !d.ContainsKey (v)) {
				SdtdConsole.Instance.Output (
					"No land protection block at the given position or not a valid position. Use \"listlandprotection\" to get a list of keystones.");
				return;
			}

			BlockChangeInfo bci = new BlockChangeInfo (v, new BlockValue (0), true, false);

			List<BlockChangeInfo> changes = new List<BlockChangeInfo> {bci};

			GameManager.Instance.SetBlocksRPC (changes);

			SdtdConsole.Instance.Output ("Land protection block at (" + v + ") removed");
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count > 0 && _params [0].EqualsCaseInsensitive ("nearby")) {
				if (_senderInfo.RemoteClientInfo != null) {
					_params.Add (_senderInfo.RemoteClientInfo.entityId.ToString ());
				} else if (_senderInfo.IsLocalGame && !GameManager.IsDedicatedServer) {
					_params.Add (GameManager.Instance.World.GetPrimaryPlayerId ().ToString ());
				}

				try {
					int closeToDistance = 32;
					if (_params.Count == 3) {
						if (!int.TryParse (_params [1], out closeToDistance)) {
							SdtdConsole.Instance.Output ("Given length is not an integer!");
							return;
						}

						closeToDistance /= 2;
					}

					int entityId = int.Parse (_params [_params.Count - 1]);
					EntityPlayer ep = GameManager.Instance.World.Players.dict [entityId];
					Vector3i closeTo = new Vector3i (ep.GetPosition ());
					LandClaimList.PositionFilter[] posFilters =
						{LandClaimList.CloseToFilter2dRect (closeTo, closeToDistance)};
					Dictionary<Player, List<Vector3i>> claims = LandClaimList.GetLandClaims (null, posFilters);

					try {
						List<BlockChangeInfo> changes = new List<BlockChangeInfo> ();
						foreach (KeyValuePair<Player, List<Vector3i>> kvp in claims) {
							foreach (Vector3i v in kvp.Value) {
								BlockChangeInfo bci = new BlockChangeInfo (v, new BlockValue (0), true, false);
								changes.Add (bci);
							}
						}

						GameManager.Instance.SetBlocksRPC (changes);
					} catch (Exception e) {
						SdtdConsole.Instance.Output ("Error removing claims");
						Log.Out ("Error in RemoveLandProtection.Run: " + e);
					}
				} catch (Exception e) {
					SdtdConsole.Instance.Output ("Error getting current player's position");
					Log.Out ("Error in RemoveLandProtection.Run: " + e);
				}
			} else if (_params.Count == 1) {
				removeById (_params [0]);
			} else if (_params.Count == 3) {
				removeByPosition (_params);
			} else {
				SdtdConsole.Instance.Output ("Illegal parameters");
			}
		}
	}
}