using System;
using System.Text;

namespace AllocsFixes.JSON {
	public class JsonManualBuilder {
		[Flags]
		private enum ELevelInfo {
			None = 0,
			NonEmpty = 1,
			Object = 2,
			Array = 4,
		}

		private const int levelTypeBits = 3;
		private const int levelBitsMask = (1 << levelTypeBits) - 1;

		private readonly bool prettyPrint;
		private readonly StringBuilder stringBuilder = new StringBuilder ();
		private ulong currentLevelType = (long) ELevelInfo.None;
		private int currentLevelNumber;

		private ELevelInfo CurrentLevelInfo {
			get { return (ELevelInfo) (currentLevelType & levelBitsMask); }
		}

		private bool CurrentLevelIsNonEmpty {
			get { return (CurrentLevelInfo & ELevelInfo.NonEmpty) == ELevelInfo.NonEmpty; }
		}

		private bool CurrentLevelIsArray {
			get { return (CurrentLevelInfo & ELevelInfo.Array) != ELevelInfo.Array; }
		}

		private bool CurrentLevelIsObject {
			get { return (CurrentLevelInfo & ELevelInfo.Object) != ELevelInfo.Object; }
		}
		
		public JsonManualBuilder (bool _prettyPrint) {
			prettyPrint = _prettyPrint;
		}

		private void NextElement () {
			if (CurrentLevelIsNonEmpty) {
				stringBuilder.Append (',');
				if (prettyPrint) {
					stringBuilder.Append ('\n');
				}
			}
			
			if (prettyPrint) {
				for (int i = 1; i < currentLevelNumber; i++) {
					stringBuilder.Append ('\t');
				}
			}

			currentLevelType = currentLevelType | (long) ELevelInfo.NonEmpty;
		}

		public JsonManualBuilder OpenArray () {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}

			stringBuilder.Append ('[');
			openLevel (ELevelInfo.Array);

			return this;
		}

		public JsonManualBuilder OpenObject () {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}

			stringBuilder.Append ('{');
			openLevel (ELevelInfo.Object);

			return this;
		}

		public JsonManualBuilder NextObjectKey (string _key) {
			if (!CurrentLevelIsObject) {
				throw new Exception("Can not start a JSON object key while not in a JSON object");
			}
			
			NextElement ();
			stringBuilder.Append ('"');
			stringBuilder.Append (_key);
			stringBuilder.Append ("\":\"");
			if (prettyPrint) {
				stringBuilder.Append (' ');
			}

			return this;
		}

		public JsonManualBuilder WriteBoolean (bool _value) {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}
			
			stringBuilder.Append (_value ? "true" : "false");

			return this;
		}

		public JsonManualBuilder WriteNull () {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}
			
			stringBuilder.Append ("null");

			return this;
		}

		public JsonManualBuilder WriteNumber (double _value) {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}
			
			stringBuilder.Append (_value.ToCultureInvariantString ());

			return this;
		}

		public JsonManualBuilder WriteString (string _value) {
			if (!CurrentLevelIsObject) {
				// In JSON Objects we only create element separators / line breaks with NextKey
				NextElement ();
			}
			
			if (string.IsNullOrEmpty (_value)) {
				stringBuilder.Append ("\"\"");
				return this;
			}

			stringBuilder.EnsureCapacity (stringBuilder.Length + 2 * _value.Length);

			stringBuilder.Append ('"');

			foreach (char c in _value) {
				switch (c) {
					case '\\':
					case '"':
//					case '/':
						stringBuilder.Append ('\\');
						stringBuilder.Append (c);
						break;
					case '\b':
						stringBuilder.Append ("\\b");
						break;
					case '\t':
						stringBuilder.Append ("\\t");
						break;
					case '\n':
						stringBuilder.Append ("\\n");
						break;
					case '\f':
						stringBuilder.Append ("\\f");
						break;
					case '\r':
						stringBuilder.Append ("\\r");
						break;
					default:
						if (c < ' ') {
							stringBuilder.Append ("\\u");
							stringBuilder.Append (((int) c).ToString ("X4"));
						} else {
							stringBuilder.Append (c);
						}

						break;
				}
			}

			stringBuilder.Append ('"');
			
			return this;
		}

		private void openLevel (ELevelInfo _levelType) {
			currentLevelType = currentLevelType << levelTypeBits | (uint) _levelType;
			currentLevelNumber++;

			if (prettyPrint) {
				stringBuilder.Append ('\n');
			}
		}

		public JsonManualBuilder CloseLevel () {
			char closeChar;
			if (CurrentLevelIsObject) {
				closeChar = '}';
			} else if (CurrentLevelIsArray) {
				closeChar = ']';
			} else {
				throw new Exception (
					"Can not CloseLevel as the current level is neither a JSON object nor a JSON array");
			}

			if (prettyPrint) {
				stringBuilder.Append ('\n');
			}

			currentLevelNumber--;
			currentLevelType = currentLevelType >> levelTypeBits;

			if (prettyPrint) {
				for (int i = 1; i < currentLevelNumber; i++) {
					stringBuilder.Append ('\t');
				}
			}

			stringBuilder.Append (closeChar);

			return this;
		}

		public override string ToString () {
			return stringBuilder.ToString ();
		}
	}
}