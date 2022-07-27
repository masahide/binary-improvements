using System.Collections.Generic;
using Steamworks;

namespace AllocsFixes.CustomCommands {
	public class PrivateMessageConnections {
		private static readonly Dictionary<PlatformUserIdentifierAbs, PlatformUserIdentifierAbs> senderOfLastPM = new Dictionary<PlatformUserIdentifierAbs, PlatformUserIdentifierAbs> ();

		public static void SetLastPMSender (ClientInfo _sender, ClientInfo _receiver) {
			senderOfLastPM [_receiver.InternalId] = _sender.InternalId;
		}

		public static ClientInfo GetLastPMSenderForPlayer (ClientInfo _player) {
			if (!senderOfLastPM.TryGetValue (_player.InternalId, out PlatformUserIdentifierAbs recUserId)) {
				return null;
			}

			ClientInfo recInfo = ConnectionManager.Instance.Clients.ForUserId (recUserId);
			return recInfo;
		}
	}
}