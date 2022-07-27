using System;
using System.Collections.Generic;

namespace AllocsFixes.CustomCommands {
	public class ListItems : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "lists all items that contain the given substring";
		}

		public override string[] GetCommands () {
			return new[] {"listitems", "li"};
		}

		public override string GetHelp () {
			return "List all available item names\n" +
			       "Usage:\n" +
			       "   1. listitems <searchString>\n" +
			       "   2. listitems *\n" +
			       "1. List only names that contain the given string.\n" +
			       "2. List all names.";
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count != 1 || _params [0].Length == 0) {
				SdtdConsole.Instance.Output ("Usage: listitems <searchString>");
				return;
			}

			int count = ItemClass.ItemNames.Count;
			bool showAll = _params [0].Trim ().Equals ("*");

			int listed = 0;
			for (int i = 0; i < count; i++) {
				string s = ItemClass.ItemNames [i];
				if (showAll || s.IndexOf (_params [0], StringComparison.OrdinalIgnoreCase) >= 0) {
					SdtdConsole.Instance.Output ("    " + s);
					listed++;
				}
			}

			SdtdConsole.Instance.Output ("Listed " + listed + " matching items.");
		}
	}
}