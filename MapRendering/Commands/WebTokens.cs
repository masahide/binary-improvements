using System.Collections.Generic;
using System.Text.RegularExpressions;
using AllocsFixes.NetConnections.Servers.Web;

namespace AllocsFixes.CustomCommands {
	public class WebTokens : ConsoleCmdAbstract {
		private static readonly Regex validNameTokenMatcher = new Regex (@"^\w+$");

		public override string[] GetCommands () {
			return new[] {"webtokens"};
		}

		public override string GetDescription () {
			return "Manage web tokens";
		}

		public override string GetHelp () {
			return "Set/get webtoken permission levels. A level of 0 is maximum permission.\n" +
			       "Usage:\n" +
			       "   webtokens add <username> <usertoken> <level>\n" +
			       "   webtokens remove <username>\n" +
			       "   webtokens list";
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count >= 1) {
				if (_params [0].EqualsCaseInsensitive ("add")) {
					ExecuteAdd (_params);
				} else if (_params [0].EqualsCaseInsensitive ("remove")) {
					ExecuteRemove (_params);
				} else if (_params [0].EqualsCaseInsensitive ("list")) {
					ExecuteList ();
				} else {
					SdtdConsole.Instance.Output ("Invalid sub command \"" + _params [0] + "\".");
				}
			} else {
				SdtdConsole.Instance.Output ("No sub command given.");
			}
		}

		private void ExecuteAdd (List<string> _params) {
			if (_params.Count != 4) {
				SdtdConsole.Instance.Output ("Wrong number of arguments, expected 4, found " + _params.Count + ".");
				return;
			}

			if (string.IsNullOrEmpty (_params [1])) {
				SdtdConsole.Instance.Output ("Argument 'username' is empty.");
				return;
			}

			if (!validNameTokenMatcher.IsMatch (_params [1])) {
				SdtdConsole.Instance.Output (
					"Argument 'username' may only contain characters (A-Z, a-z), digits (0-9) and underscores (_).");
				return;
			}

			if (string.IsNullOrEmpty (_params [2])) {
				SdtdConsole.Instance.Output ("Argument 'usertoken' is empty.");
				return;
			}

			if (!validNameTokenMatcher.IsMatch (_params [2])) {
				SdtdConsole.Instance.Output (
					"Argument 'usertoken' may only contain characters (A-Z, a-z), digits (0-9) and underscores (_).");
				return;
			}

			int level;
			if (!int.TryParse (_params [3], out level)) {
				SdtdConsole.Instance.Output ("Argument 'level' is not a valid integer.");
				return;
			}

			WebPermissions.Instance.AddAdmin (_params [1], _params [2], level);
			SdtdConsole.Instance.Output (string.Format (
				"Web user with name={0} and password={1} added with permission level of {2}.", _params [1], _params [2],
				level));
		}

		private void ExecuteRemove (List<string> _params) {
			if (_params.Count != 2) {
				SdtdConsole.Instance.Output ("Wrong number of arguments, expected 2, found " + _params.Count + ".");
				return;
			}

			if (string.IsNullOrEmpty (_params [1])) {
				SdtdConsole.Instance.Output ("Argument 'username' is empty.");
				return;
			}

			if (!validNameTokenMatcher.IsMatch (_params [1])) {
				SdtdConsole.Instance.Output (
					"Argument 'username' may only contain characters (A-Z, a-z), digits (0-9) and underscores (_).");
				return;
			}

			WebPermissions.Instance.RemoveAdmin (_params [1]);
			SdtdConsole.Instance.Output (string.Format ("{0} removed from web user permissions list.", _params [1]));
		}

		private void ExecuteList () {
			SdtdConsole.Instance.Output ("Defined webuser permissions:");
			SdtdConsole.Instance.Output ("  Level: Name / Token");
			foreach (WebPermissions.AdminToken at in WebPermissions.Instance.GetAdmins ()) {
				SdtdConsole.Instance.Output (
					string.Format ("  {0,5}: {1} / {2}", at.permissionLevel, at.name, at.token));
			}
		}
	}
}