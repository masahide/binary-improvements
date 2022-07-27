using System;
using System.Collections.Generic;

namespace AllocsFixes.CustomCommands {
	public class SayToPlayer : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "send a message to a single player";
		}

		public override string GetHelp () {
			return "Usage:\n" +
			       "   pm <player name / steam id / entity id> <message>\n" +
			       "Send a PM to the player given by the player name or entity id (as given by e.g. \"lpi\").";
		}

		public override string[] GetCommands () {
			return new[] {"sayplayer", "pm"};
		}

		private void RunInternal (ClientInfo _sender, List<string> _params) {
			if (_params.Count < 2) {
				SdtdConsole.Instance.Output ("Usage: sayplayer <playername|entityid> <message>");
				return;
			}

			string message = _params [1];

			ClientInfo receiver = ConsoleHelper.ParseParamIdOrName (_params [0]);
			if (receiver != null) {
				Chat.SendMessage (receiver, _sender, message);
			} else {
				SdtdConsole.Instance.Output ("Playername or entity ID not found.");
			}
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_senderInfo.RemoteClientInfo != null) {
				RunInternal (_senderInfo.RemoteClientInfo, _params);
			} else {
				RunInternal (null, _params);
			}
		}
	}
}