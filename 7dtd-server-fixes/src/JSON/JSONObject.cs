using System.Collections.Generic;
using System.Text;

namespace AllocsFixes.JSON {
	public class JSONObject : JSONNode {
		private readonly Dictionary<string, JSONNode> nodes = new Dictionary<string, JSONNode> ();

		public JSONNode this [string _name] {
			get { return nodes [_name]; }
			set { nodes [_name] = value; }
		}

		public int Count {
			get { return nodes.Count; }
		}

		public List<string> Keys {
			get { return new List<string> (nodes.Keys); }
		}

		public bool ContainsKey (string _name) {
			return nodes.ContainsKey (_name);
		}

		public void Add (string _name, JSONNode _node) {
			nodes.Add (_name, _node);
		}

		public override void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0) {
			_stringBuilder.Append ("{");
			if (_prettyPrint) {
				_stringBuilder.Append ('\n');
			}

			foreach (KeyValuePair<string, JSONNode> kvp in nodes) {
				if (_prettyPrint) {
					_stringBuilder.Append (new string ('\t', _currentLevel + 1));
				}

				_stringBuilder.Append (string.Format ("\"{0}\":", kvp.Key));
				if (_prettyPrint) {
					_stringBuilder.Append (" ");
				}

				kvp.Value.ToString (_stringBuilder, _prettyPrint, _currentLevel + 1);
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

			_stringBuilder.Append ("}");
		}

		public static JSONObject Parse (string _json, ref int _offset) {
			//Log.Out ("ParseObject enter (" + offset + ")");
			JSONObject obj = new JSONObject ();

			bool nextElemAllowed = true;
			_offset++;
			while (true) {
				Parser.SkipWhitespace (_json, ref _offset);
				switch (_json [_offset]) {
					case '"':
						if (nextElemAllowed) {
							JSONString key = JSONString.Parse (_json, ref _offset);
							Parser.SkipWhitespace (_json, ref _offset);
							if (_json [_offset] != ':') {
								throw new MalformedJSONException (
									"Could not parse object, missing colon (\":\") after key");
							}

							_offset++;
							JSONNode val = Parser.ParseInternal (_json, ref _offset);
							obj.Add (key.GetString (), val);
							nextElemAllowed = false;
						} else {
							throw new MalformedJSONException (
								"Could not parse object, found new key without a separating comma");
						}

						break;
					case ',':
						if (!nextElemAllowed) {
							nextElemAllowed = true;
							_offset++;
						} else {
							throw new MalformedJSONException (
								"Could not parse object, found a comma without a key/value pair first");
						}

						break;
					case '}':
						_offset++;

						//Log.Out ("JSON:Parsed Object: " + obj.ToString ());
						return obj;
					default:
						break;
				}
			}
		}
	}
}