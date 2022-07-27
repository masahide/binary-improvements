namespace AllocsFixes.CustomCommands {
	public class Chat {
		public static void SendMessage (ClientInfo _receiver, ClientInfo _sender, string _message) {
			string senderName;
			if (_sender != null) {
				PrivateMessageConnections.SetLastPMSender (_sender, _receiver);
				senderName = _sender.playerName;
			} else {
				senderName = "Server";
			}

			_receiver.SendPackage (NetPackageManager.GetPackage<NetPackageChat> ().Setup (EChatType.Whisper, -1, _message, senderName + " (PM)", false, null));
			string receiverName = _receiver.playerName;
			SdtdConsole.Instance.Output ("Message to player " +
			                             (receiverName != null ? "\"" + receiverName + "\"" : "unknownName") +
			                             " sent with sender \"" + senderName + "\"");
		}
	}
}