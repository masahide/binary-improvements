using System;
using UnityEngine;

namespace AllocsFixes.PersistentData {
	[Serializable]
	public class Player {
		private readonly PlatformUserIdentifierAbs platformId;
		private int entityId;
		private string name;
		private string ip;
		private long totalPlayTime;

		private DateTime lastOnline;

		private Inventory inventory;

		private int lastPositionX, lastPositionY, lastPositionZ;

		private bool chatMuted;
		private int maxChatLength;
		private string chatColor;
		private bool chatName;
		private uint expToNextLevel;
		private int level;

		[NonSerialized] private ClientInfo clientInfo;

		public PlatformUserIdentifierAbs PlatformId => platformId;

		public int EntityID => entityId;

		public string Name => name ?? string.Empty;

		public string IP => ip ?? string.Empty;

		public Inventory Inventory => inventory ?? (inventory = new Inventory ());

		public bool IsOnline => clientInfo != null;

		public ClientInfo ClientInfo => clientInfo;

		public EntityPlayer Entity => IsOnline ? GameManager.Instance.World.Players.dict [clientInfo.entityId] : null;

		public long TotalPlayTime {
			get {
				if (IsOnline) {
					return totalPlayTime + (long) (DateTime.Now - lastOnline).TotalSeconds;
				}

				return totalPlayTime;
			}
		}

		public DateTime LastOnline => IsOnline ? DateTime.Now : lastOnline;

		public Vector3i LastPosition => IsOnline ? new Vector3i (Entity.GetPosition ()) : new Vector3i (lastPositionX, lastPositionY, lastPositionZ);

		public bool LandProtectionActive =>
			GameManager.Instance.World.IsLandProtectionValidForPlayer (GameManager.Instance
				.GetPersistentPlayerList ().GetPlayerData (PlatformId));

		public float LandProtectionMultiplier =>
			GameManager.Instance.World.GetLandProtectionHardnessModifierForPlayer (GameManager.Instance
				.GetPersistentPlayerList ().GetPlayerData (PlatformId));

		public float Level {
			get {
				float expForNextLevel =
					(int) Math.Min (Progression.BaseExpToLevel * Mathf.Pow (Progression.ExpMultiplier, level + 1),
						int.MaxValue);
				float fLevel = level + 1f - expToNextLevel / expForNextLevel;
				return fLevel;
			}
		}

		public bool IsChatMuted {
			get => chatMuted;
			set => chatMuted = value;
		}

		public int MaxChatLength {
			get {
				if (maxChatLength == 0) {
					maxChatLength = 255;
				}

				return maxChatLength;
			}
			set => maxChatLength = value;
		}

		public string ChatColor {
			get {
				if (string.IsNullOrEmpty (chatColor)) {
					chatColor = "";
				}

				return chatColor;
			}

			set => chatColor = value;
		}

		public bool ChatName {
			get => chatName;
			set => chatName = value;
		}

		public Player (PlatformUserIdentifierAbs _platformId) {
			platformId = _platformId;
			inventory = new Inventory ();
		}

		public void SetOffline () {
			if (clientInfo == null) {
				return;
			}

			Log.Out ("Player set to offline: " + platformId);
			lastOnline = DateTime.Now;
			try {
				Vector3i lastPos = new Vector3i (Entity.GetPosition ());
				lastPositionX = lastPos.x;
				lastPositionY = lastPos.y;
				lastPositionZ = lastPos.z;
				totalPlayTime += (long) (Time.timeSinceLevelLoad - Entity.CreationTimeSinceLevelLoad);
			} catch (NullReferenceException) {
				Log.Out ("Entity not available. Something seems to be wrong here...");
			}

			clientInfo = null;
		}

		public void SetOnline (ClientInfo _ci) {
			Log.Out ("Player set to online: " + platformId);
			clientInfo = _ci;
            entityId = _ci.entityId;
			name = _ci.playerName;
			ip = _ci.ip;
			lastOnline = DateTime.Now;
		}

		public void Update (PlayerDataFile _pdf) {
			UpdateProgression (_pdf);
			inventory.Update (_pdf);
		}

		private void UpdateProgression (PlayerDataFile _pdf) {
			if (_pdf.progressionData.Length <= 0) {
				return;
			}

			using (PooledBinaryReader pbr = MemoryPools.poolBinaryReader.AllocSync (false)) {
				pbr.SetBaseStream (_pdf.progressionData);
				long posBefore = pbr.BaseStream.Position;
				pbr.BaseStream.Position = 0;
				Progression p = Progression.Read (pbr, null);
				pbr.BaseStream.Position = posBefore;
				expToNextLevel = (uint) p.ExpToNextLevel;
				level = p.Level;
			}
		}

	}
}