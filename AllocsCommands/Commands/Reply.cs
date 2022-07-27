using System.Collections.Generic;

namespace AllocsFixes.CustomCommands {
	public class Reply : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "send a message to  the player who last sent you a PM";
		}

		public override string GetHelp () {
			return "Usage:\n" +
			       "   reply <message>\n" +
			       "Send the given message to the user you last received a PM from.";
		}

		public override string[] GetCommands () {
			return new[] {"reply", "re"};
		}

		private void RunInternal (ClientInfo _sender, List<string> _params) {
			if (_params.Count < 1) {
				SdtdConsole.Instance.Output ("Usage: reply <message>");
				return;
			}

			string message = _params [0];

			ClientInfo receiver = PrivateMessageConnections.GetLastPMSenderForPlayer (_sender);
			if (receiver != null) {
				Chat.SendMessage (receiver, _sender, message);
			} else {
				SdtdConsole.Instance.Output (
					"You have not received a PM so far or sender of last received PM is no longer online.");
			}
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_senderInfo.RemoteClientInfo == null) {
				Log.Out ("Command \"reply\" can only be used on clients!");
			} else {
				RunInternal (_senderInfo.RemoteClientInfo, _params);
			}
		}
	}
}