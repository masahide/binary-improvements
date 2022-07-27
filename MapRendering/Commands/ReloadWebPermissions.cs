using System;
using System.Collections.Generic;
using AllocsFixes.NetConnections.Servers.Web;

namespace AllocsFixes.CustomCommands {
	public class ReloadWebPermissions : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "force reload of web permissions file";
		}

		public override string[] GetCommands () {
			return new[] {"reloadwebpermissions"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			WebPermissions.Instance.Load ();
			SdtdConsole.Instance.Output ("Web permissions file reloaded");
		}
	}
}