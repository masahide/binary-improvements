using System;
using System.Collections.Generic;

namespace AllocsFixes.CustomCommands {
	public class EnableRendering : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "enable/disable live map rendering";
		}

		public override string[] GetCommands () {
			return new[] {"enablerendering"};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count != 1) {
				SdtdConsole.Instance.Output ("Current state: " + MapRendering.MapRendering.renderingEnabled);
				return;
			}

			MapRendering.MapRendering.renderingEnabled = _params [0].Equals ("1");
			SdtdConsole.Instance.Output ("Set live map rendering to " + _params [0].Equals ("1"));
		}
	}
}