using System.Text;

namespace AllocsFixes.JSON {
	public class JSONNull : JSONValue {
		public override void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0) {
			_stringBuilder.Append ("null");
		}

		public static JSONNull Parse (string _json, ref int _offset) {
			//Log.Out ("ParseNull enter (" + offset + ")");

			if (!_json.Substring (_offset, 4).Equals ("null")) {
				throw new MalformedJSONException ("No valid null value found");
			}

			//Log.Out ("JSON:Parsed Null");
			_offset += 4;
			return new JSONNull ();
		}
	}
}