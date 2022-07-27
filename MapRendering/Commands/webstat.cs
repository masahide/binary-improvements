using System;
using System.Collections.Generic;
using AllocsFixes.NetConnections.Servers.Web;

namespace AllocsFixes.CustomCommands {
	public class webstat : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "DEBUG PURPOSES ONLY";
		}

		public override string[] GetCommands () {
			return new[] {"webstat"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			int curHandlers = Web.currentHandlers;
			int totalHandlers = Web.handlingCount;
			long totalTime = Web.totalHandlingTime;
			SdtdConsole.Instance.Output ("Current Web handlers: " + curHandlers + " - total: " + totalHandlers);
			SdtdConsole.Instance.Output (" - Total time: " + totalTime + " µs - average time: " +
			                             totalTime / totalHandlers + " µs");

			curHandlers = WebCommandResult.currentHandlers;
			totalHandlers = WebCommandResult.handlingCount;
			totalTime = WebCommandResult.totalHandlingTime;
			SdtdConsole.Instance.Output ("Current Web command handlers: " + curHandlers + " - total: " +
			                             totalHandlers);
			SdtdConsole.Instance.Output (" - Total time: " + totalTime + " µs" +
			                             (totalHandlers > 0
				                             ? " - average time: " + totalTime / totalHandlers + " µs"
				                             : ""));
		}
	}
}