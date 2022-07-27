using System;
using System.Text;

namespace AllocsFixes.JSON {
	public class JSONNumber : JSONValue {
		private readonly double value;

		public JSONNumber (double _value) {
			value = _value;
		}

		public double GetDouble () {
			return value;
		}

		public int GetInt () {
			return (int) Math.Round (value);
		}

		public override void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0) {
			_stringBuilder.Append (value.ToCultureInvariantString ());
		}

		public static JSONNumber Parse (string _json, ref int _offset) {
			//Log.Out ("ParseNumber enter (" + offset + ")");
			StringBuilder sbNum = new StringBuilder ();
			StringBuilder sbExp = null;
			bool hasDec = false;
			bool hasExp = false;
			while (_offset < _json.Length) {
				if (_json [_offset] >= '0' && _json [_offset] <= '9') {
					if (hasExp) {
						sbExp.Append (_json [_offset]);
					} else {
						sbNum.Append (_json [_offset]);
					}
				} else if (_json [_offset] == '.') {
					if (hasExp) {
						throw new MalformedJSONException ("Decimal separator in exponent");
					}

					if (hasDec) {
						throw new MalformedJSONException ("Multiple decimal separators in number found");
					}

					if (sbNum.Length == 0) {
						throw new MalformedJSONException ("No leading digits before decimal separator found");
					}

					sbNum.Append ('.');
					hasDec = true;
				} else if (_json [_offset] == '-') {
					if (hasExp) {
						if (sbExp.Length > 0) {
							throw new MalformedJSONException ("Negative sign in exponent after digits");
						}

						sbExp.Append (_json [_offset]);
					} else {
						if (sbNum.Length > 0) {
							throw new MalformedJSONException ("Negative sign in mantissa after digits");
						}

						sbNum.Append (_json [_offset]);
					}
				} else if (_json [_offset] == 'e' || _json [_offset] == 'E') {
					if (hasExp) {
						throw new MalformedJSONException ("Multiple exponential markers in number found");
					}

					if (sbNum.Length == 0) {
						throw new MalformedJSONException ("No leading digits before exponential marker found");
					}

					sbExp = new StringBuilder ();
					hasExp = true;
				} else if (_json [_offset] == '+') {
					if (hasExp) {
						if (sbExp.Length > 0) {
							throw new MalformedJSONException ("Positive sign in exponent after digits");
						}

						sbExp.Append (_json [_offset]);
					} else {
						throw new MalformedJSONException ("Positive sign in mantissa found");
					}
				} else {
					double number;
					if (!StringParsers.TryParseDouble (sbNum.ToString (), out number)) {
						throw new MalformedJSONException ("Mantissa is not a valid decimal (\"" + sbNum + "\")");
					}

					if (hasExp) {
						int exp;
						if (!int.TryParse (sbExp.ToString (), out exp)) {
							throw new MalformedJSONException ("Exponent is not a valid integer (\"" + sbExp + "\")");
						}

						number = number * Math.Pow (10, exp);
					}

					//Log.Out ("JSON:Parsed Number: " + number.ToString ());
					return new JSONNumber (number);
				}

				_offset++;
			}

			throw new MalformedJSONException ("End of JSON reached before parsing number finished");
		}
	}
}