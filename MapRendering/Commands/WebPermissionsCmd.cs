using System.Collections.Generic;
using AllocsFixes.NetConnections.Servers.Web;

namespace AllocsFixes.CustomCommands {
	public class WebPermissionsCmd : ConsoleCmdAbstract {
		public override string[] GetCommands () {
			return new[] {"webpermission"};
		}

		public override string GetDescription () {
			return "Manage web permission levels";
		}

		public override string GetHelp () {
			return "Set/get permission levels required to access a given web functionality. Default\n" +
			       "level required for functions that are not explicitly specified is 0.\n" +
			       "Usage:\n" +
			       "   webpermission add <webfunction> <level>\n" +
			       "   webpermission remove <webfunction>\n" +
			       "   webpermission list";
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
					SdtdConsole.Instance.Output ($"Invalid sub command \"{_params [0]}\".");
				}
			} else {
				SdtdConsole.Instance.Output ("No sub command given.");
			}
		}

		private void ExecuteAdd (List<string> _params) {
			if (_params.Count != 3) {
				SdtdConsole.Instance.Output ($"Wrong number of arguments, expected 3, found {_params.Count}.");
				return;
			}

			if (!WebPermissions.Instance.IsKnownModule (_params [1])) {
				SdtdConsole.Instance.Output ($"\"{_params [1]}\" is not a valid web function.");
				return;
			}

			int level;
			if (!int.TryParse (_params [2], out level)) {
				SdtdConsole.Instance.Output ($"\"{_params [2]}\" is not a valid integer.");
				return;
			}

			WebPermissions.Instance.AddModulePermission (_params [1], level);
			SdtdConsole.Instance.Output ($"{_params [1]} added with permission level of {level}.");
		}

		private void ExecuteRemove (List<string> _params) {
			if (_params.Count != 2) {
				SdtdConsole.Instance.Output ($"Wrong number of arguments, expected 2, found {_params.Count}.");
				return;
			}

			if (!WebPermissions.Instance.IsKnownModule (_params [1])) {
				SdtdConsole.Instance.Output ($"\"{_params [1]}\" is not a valid web function.");
				return;
			}

			WebPermissions.Instance.RemoveModulePermission (_params [1]);
			SdtdConsole.Instance.Output ($"{_params [1]} removed from permissions list.");
		}

		private void ExecuteList () {
			SdtdConsole.Instance.Output ("Defined web function permissions:");
			SdtdConsole.Instance.Output ("  Level: Web function");
			foreach (WebPermissions.WebModulePermission wmp in WebPermissions.Instance.GetModules ()) {
				SdtdConsole.Instance.Output ($"  {wmp.permissionLevel,5}: {wmp.module}");
			}
		}
	}
}