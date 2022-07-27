using System;
using System.Collections.Generic;
using UnityEngine;

namespace AllocsFixes.CustomCommands {
	public class Give : ConsoleCmdAbstract {
		public override string GetDescription () {
			return "give an item to a player (entity id or name)";
		}

		public override string GetHelp () {
			return "Give an item to a player by dropping it in front of that player\n" +
			       "Usage:\n" +
			       "   give <name / entity id> <item name> <amount>\n" +
			       "   give <name / entity id> <item name> <amount> <quality>\n" +
			       "Either pass the full name of a player or his entity id (given by e.g. \"lpi\").\n" +
			       "Item name has to be the exact name of an item as listed by \"listitems\".\n" +
			       "Amount is the number of instances of this item to drop (as a single stack).\n" +
			       "Quality is the quality of the dropped items for items that have a quality.";
		}

		public override string[] GetCommands () {
			return new[] {"give", string.Empty};
		}

		public override void Execute (List<string> _params, CommandSenderInfo _senderInfo) {
			if (_params.Count != 3 && _params.Count != 4) {
				SdtdConsole.Instance.Output ("Wrong number of arguments, expected 3 or 4, found " + _params.Count +
				                             ".");
				return;
			}

			ClientInfo ci = ConsoleHelper.ParseParamIdOrName (_params [0]);

			if (ci == null) {
				SdtdConsole.Instance.Output ("Playername or entity id not found.");
				return;
			}

			ItemValue iv = ItemClass.GetItem (_params [1], true);
			if (iv.type == ItemValue.None.type) {
				SdtdConsole.Instance.Output ("Item not found.");
				return;
			}

			iv = new ItemValue (iv.type, true);

			int n;
			if (!int.TryParse (_params [2], out n) || n <= 0) {
				SdtdConsole.Instance.Output ("Amount is not an integer or not greater than zero.");
				return;
			}

			int quality = Constants.cItemMaxQuality;

			if (_params.Count == 4) {
				if (!int.TryParse (_params [3], out quality) || quality <= 0) {
					SdtdConsole.Instance.Output ("Quality is not an integer or not greater than zero.");
					return;
				}
			}

			if (ItemClass.list [iv.type].HasSubItems) {
				for (int i = 0; i < iv.Modifications.Length; i++) {
					ItemValue tmp = iv.Modifications [i];
					tmp.Quality = quality;
					iv.Modifications [i] = tmp;
				}
			} else if (ItemClass.list [iv.type].HasQuality) {
				iv.Quality = quality;
			}

			EntityPlayer p = GameManager.Instance.World.Players.dict [ci.entityId];

			ItemStack invField = new ItemStack (iv, n);

			GameManager.Instance.ItemDropServer (invField, p.GetPosition (), Vector3.zero);

			SdtdConsole.Instance.Output ("Dropped item");
		}
	}
}