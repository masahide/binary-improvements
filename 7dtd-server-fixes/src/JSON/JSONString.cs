using System.Text;

namespace AllocsFixes.JSON {
	public class JSONString : JSONValue {
		private readonly string value;

		public JSONString (string _value) {
			value = _value;
		}

		public string GetString () {
			return value;
		}

		public override void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0) {
			if (value == null || value.Length == 0) {
				_stringBuilder.Append ("\"\"");
				return;
			}

			int len = value.Length;

			_stringBuilder.EnsureCapacity (_stringBuilder.Length + 2 * len);

			_stringBuilder.Append ('"');

			foreach (char c in value) {
				switch (c) {
					case '\\':
					case '"':

//					case '/':
						_stringBuilder.Append ('\\');
						_stringBuilder.Append (c);
						break;
					case '\b':
						_stringBuilder.Append ("\\b");
						break;
					case '\t':
						_stringBuilder.Append ("\\t");
						break;
					case '\n':
						_stringBuilder.Append ("\\n");
						break;
					case '\f':
						_stringBuilder.Append ("\\f");
						break;
					case '\r':
						_stringBuilder.Append ("\\r");
						break;
					default:
						if (c < ' ') {
							_stringBuilder.Append ("\\u");
							_stringBuilder.Append (((int) c).ToString ("X4"));
						} else {
							_stringBuilder.Append (c);
						}

						break;
				}
			}

			_stringBuilder.Append ('"');
		}

		public static JSONString Parse (string _json, ref int _offset) {
			//Log.Out ("ParseString enter (" + offset + ")");
			StringBuilder sb = new StringBuilder ();
			_offset++;
			while (_offset < _json.Length) {
				switch (_json [_offset]) {
					case '\\':
						_offset++;
						switch (_json [_offset]) {
							case '\\':
							case '"':
							case '/':
								sb.Append (_json [_offset]);
								break;
							case 'b':
								sb.Append ('\b');
								break;
							case 't':
								sb.Append ('\t');
								break;
							case 'n':
								sb.Append ('\n');
								break;
							case 'f':
								sb.Append ('\f');
								break;
							case 'r':
								sb.Append ('\r');
								break;
							default:
								sb.Append (_json [_offset]);
								break;
						}

						_offset++;
						break;
					case '"':
						_offset++;

						//Log.Out ("JSON:Parsed String: " + sb.ToString ());
						return new JSONString (sb.ToString ());
					default:
						sb.Append (_json [_offset]);
						_offset++;
						break;
				}
			}

			throw new MalformedJSONException ("End of JSON reached before parsing string finished");
		}
	}
}