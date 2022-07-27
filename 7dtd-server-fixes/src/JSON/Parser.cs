namespace AllocsFixes.JSON {
	public class Parser {
		public static JSONNode Parse (string _json) {
			int offset = 0;
			return ParseInternal (_json, ref offset);
		}

		public static JSONNode ParseInternal (string _json, ref int _offset) {
			SkipWhitespace (_json, ref _offset);

			//Log.Out ("ParseInternal (" + offset + "): Decide on: '" + json [offset] + "'");
			switch (_json [_offset]) {
				case '[':
					return JSONArray.Parse (_json, ref _offset);
				case '{':
					return JSONObject.Parse (_json, ref _offset);
				case '"':
					return JSONString.Parse (_json, ref _offset);
				case 't':
				case 'f':
					return JSONBoolean.Parse (_json, ref _offset);
				case 'n':
					return JSONNull.Parse (_json, ref _offset);
				default:
					return JSONNumber.Parse (_json, ref _offset);
			}
		}

		public static void SkipWhitespace (string _json, ref int _offset) {
			//Log.Out ("SkipWhitespace (" + offset + "): '" + json [offset] + "'");
			while (_offset < _json.Length) {
				switch (_json [_offset]) {
					case ' ':
					case '\t':
					case '\r':
					case '\n':
						_offset++;
						break;
					default:
						return;
				}
			}

			throw new MalformedJSONException ("End of JSON reached before parsing finished");
		}
	}
}