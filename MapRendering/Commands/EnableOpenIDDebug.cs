using System;
using System.Collections.Generic;
using AllocsFixes.NetConnections.Servers.Web;

namespace AllocsFixes.CustomCommands {
	public class EnableOpenIDDebug : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "enable/disable OpenID debugging";
		}

		public override string[] GetCommands () {
			return new[] {"openiddebug"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count != 1) {
				SdtdConsole.Instance.Output ("Current state: " + OpenID.debugOpenId);
				return;
			}

			OpenID.debugOpenId = _params [0].Equals ("1");
			SdtdConsole.Instance.Output ("Set OpenID debugging to " + _params [0].Equals ("1"));
		}
	}
}