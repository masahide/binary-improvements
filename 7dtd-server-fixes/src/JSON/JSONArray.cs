using System.Collections.Generic;
using System.Text;

namespace AllocsFixes.JSON {
	public class JSONArray : JSONNode {
		private readonly List<JSONNode> nodes = new List<JSONNode> ();

		public JSONNode this [int _index] {
			get { return nodes [_index]; }
			set { nodes [_index] = value; }
		}

		public int Count {
			get { return nodes.Count; }
		}

		public void Add (JSONNode _node) {
			nodes.Add (_node);
		}

		public override void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0) {
			_stringBuilder.Append ("[");
			if (_prettyPrint) {
				_stringBuilder.Append ('\n');
			}

			foreach (JSONNode n in nodes) {
				if (_prettyPrint) {
					_stringBuilder.Append (new string ('\t', _currentLevel + 1));
				}

				n.ToString (_stringBuilder, _prettyPrint, _currentLevel + 1);
				_stringBuilder.Append (",");
				if (_prettyPrint) {
					_stringBuilder.Append ('\n');
				}
			}

			if (nodes.Count > 0) {
				_stringBuilder.Remove (_stringBuilder.Length - (_prettyPrint ? 2 : 1), 1);
			}

			if (_prettyPrint) {
				_stringBuilder.Append (new string ('\t', _currentLevel));
			}

			_stringBuilder.Append ("]");
		}

		public static JSONArray Parse (string _json, ref int _offset) {
			//Log.Out ("ParseArray enter (" + offset + ")");
			JSONArray arr = new JSONArray ();

			bool nextElemAllowed = true;
			_offset++;
			while (true) {
				Parser.SkipWhitespace (_json, ref _offset);

				switch (_json [_offset]) {
					case ',':
						if (!nextElemAllowed) {
							nextElemAllowed = true;
							_offset++;
						} else {
							throw new MalformedJSONException (
								"Could not parse array, found a comma without a value first");
						}

						break;
					case ']':
						_offset++;

						//Log.Out ("JSON:Parsed Array: " + arr.ToString ());
						return arr;
					default:
						arr.Add (Parser.ParseInternal (_json, ref _offset));
						nextElemAllowed = false;
						break;
				}
			}
		}
	}
}