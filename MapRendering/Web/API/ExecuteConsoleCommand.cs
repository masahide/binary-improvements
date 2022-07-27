using System;
using System.Net;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class ExecuteConsoleCommand : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			if (string.IsNullOrEmpty (_req.QueryString ["command"])) {
				_resp.StatusCode = (int) HttpStatusCode.BadRequest;
				Web.SetResponseTextContent (_resp, "No command given");
				return;
			}

			WebCommandResult.ResultType responseType =
				_req.QueryString ["raw"] != null
					? WebCommandResult.ResultType.Raw
					: (_req.QueryString ["simple"] != null
						? WebCommandResult.ResultType.ResultOnly
						: WebCommandResult.ResultType.Full);

			string commandline = _req.QueryString ["command"];
			string commandPart = commandline.Split (' ') [0];
			string argumentsPart = commandline.Substring (Math.Min (commandline.Length, commandPart.Length + 1));

			IConsoleCommand command = SdtdConsole.Instance.GetCommand (commandline);

			if (command == null) {
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				Web.SetResponseTextContent (_resp, "Unknown command");
				return;
			}

			int commandPermissionLevel = GameManager.Instance.adminTools.GetCommandPermissionLevel (command.GetCommands ());

			if (_permissionLevel > commandPermissionLevel) {
				_resp.StatusCode = (int) HttpStatusCode.Forbidden;
				Web.SetResponseTextContent (_resp, "You are not allowed to execute this command");
				return;
			}

			_resp.SendChunked = true;
			WebCommandResult wcr = new WebCommandResult (commandPart, argumentsPart, responseType, _resp);
			SdtdConsole.Instance.ExecuteAsync (commandline, wcr);
		}

		public override int DefaultPermissionLevel () {
			return 2000;
		}
	}
}