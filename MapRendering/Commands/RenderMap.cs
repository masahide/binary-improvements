using System;
using System.Collections.Generic;

namespace AllocsFixes.CustomCommands {
	public class RenderMap : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "render the current map to a file";
		}

		public override string[] GetCommands () {
			return new[] {"rendermap"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			MapRendering.MapRendering.Instance.RenderFullMap ();

			SdtdConsole.Instance.Output ("Render map done");
		}
	}
}