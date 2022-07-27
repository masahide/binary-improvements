using System.Collections.Generic;
using AllocsFixes.PersistentData;
using Platform.Steam;

namespace AllocsFixes {
	public class API : IModApi {
		public void InitMod (Mod _modInstance) {
			ModEvents.GameStartDone.RegisterHandler (GameAwake);
			ModEvents.GameShutdown.RegisterHandler (GameShutdown);
			ModEvents.SavePlayerData.RegisterHandler (SavePlayerData);
			ModEvents.PlayerSpawning.RegisterHandler (PlayerSpawning);
			ModEvents.PlayerDisconnected.RegisterHandler (PlayerDisconnected);
			ModEvents.PlayerSpawnedInWorld.RegisterHandler (PlayerSpawned);
			ModEvents.ChatMessage.RegisterHandler (ChatMessage);
		}

		public void GameAwake () {
			PersistentContainer.Load ();
		}

		public void GameShutdown () {
		}

		public void SavePlayerData (ClientInfo _cInfo, PlayerDataFile _playerDataFile) {
			PersistentContainer.Instance.Players [_cInfo.InternalId, true].Update (_playerDataFile);
		}

		public void PlayerSpawning (ClientInfo _cInfo, int _chunkViewDim, PlayerProfile _playerProfile) {
			string owner = null;
			if (_cInfo.PlatformId is UserIdentifierSteam identifierSteam) {
				owner = identifierSteam.OwnerId.ToString ();
			}

			Log.Out ("Player connected" +
			         ", entityid=" + _cInfo.entityId +
			         ", name=" + _cInfo.playerName +
			         ", pltfmid=" + (_cInfo.PlatformId?.CombinedString ?? "<unknown>") +
			         ", crossid=" + (_cInfo.CrossplatformId?.CombinedString ?? "<unknown/none>") +
			         ", steamOwner=" + (owner ?? "<unknown/none>") +
			         ", ip=" + _cInfo.ip
			);
		}

		public void PlayerDisconnected (ClientInfo _cInfo, bool _bShutdown) {
			Player p = PersistentContainer.Instance.Players [_cInfo.InternalId, false];
			if (p != null) {
				p.SetOffline ();
			} else {
				Log.Out ("Disconnected player not found in client list...");
			}

			PersistentContainer.Instance.Save ();
		}

		public void PlayerSpawned (ClientInfo _cInfo, RespawnType _respawnReason, Vector3i _spawnPos) {
			PersistentContainer.Instance.Players [_cInfo.InternalId, true].SetOnline (_cInfo);
			PersistentContainer.Instance.Save ();
		}

		private const string ANSWER =
			"     [ff0000]I[-] [ff7f00]W[-][ffff00]A[-][80ff00]S[-] [00ffff]H[-][0080ff]E[-][0000ff]R[-][8b00ff]E[-]";

		public bool ChatMessage (ClientInfo _cInfo, EChatType _type, int _senderId, string _msg, string _mainName,
			bool _localizeMain, List<int> _recipientEntityIds) {
			if (string.IsNullOrEmpty (_msg) || !_msg.EqualsCaseInsensitive ("/alloc")) {
				return true;
			}

			if (_cInfo != null) {
				Log.Out ("Sent chat hook reply to {0}", _cInfo.InternalId);
				_cInfo.SendPackage (NetPackageManager.GetPackage<NetPackageChat> ().Setup (EChatType.Whisper, -1, ANSWER, "", false, null));
			} else {
				Log.Error ("ChatHookExample: Argument _cInfo null on message: {0}", _msg);
			}

			return false;
		}
	}
}