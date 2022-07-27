using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetPlayerList : WebAPI {
		private static readonly Regex numberFilterMatcher =
			new Regex (@"^(>=|=>|>|<=|=<|<|==|=)?\s*([0-9]+(\.[0-9]*)?)$");

#if ENABLE_PROFILER
		private static readonly UnityEngine.Profiling.CustomSampler jsonSerializeSampler = UnityEngine.Profiling.CustomSampler.Create ("JSON_Build");
#endif

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			AdminTools admTools = GameManager.Instance.adminTools;
			PlatformUserIdentifierAbs userId = _user?.UserId;

			bool bViewAll = WebConnection.CanViewAllPlayers (_permissionLevel);

			// TODO: Sort (and filter?) prior to converting to JSON ... hard as how to get the correct column's data? (i.e. column name matches JSON object field names, not source data)

			int rowsPerPage = 25;
			if (_req.QueryString ["rowsperpage"] != null) {
				int.TryParse (_req.QueryString ["rowsperpage"], out rowsPerPage);
			}

			int page = 0;
			if (_req.QueryString ["page"] != null) {
				int.TryParse (_req.QueryString ["page"], out page);
			}

			int firstEntry = page * rowsPerPage;

			Players playersList = PersistentContainer.Instance.Players;

			
			List<JSONObject> playerList = new List<JSONObject> ();

#if ENABLE_PROFILER
			jsonSerializeSampler.Begin ();
#endif

			foreach (KeyValuePair<PlatformUserIdentifierAbs, Player> kvp in playersList.Dict) {
				Player p = kvp.Value;

				if (bViewAll || p.PlatformId.Equals (userId)) {
					JSONObject pos = new JSONObject ();
					pos.Add ("x", new JSONNumber (p.LastPosition.x));
					pos.Add ("y", new JSONNumber (p.LastPosition.y));
					pos.Add ("z", new JSONNumber (p.LastPosition.z));

					JSONObject pJson = new JSONObject ();
					pJson.Add ("steamid", new JSONString (kvp.Key.CombinedString));
					pJson.Add ("entityid", new JSONNumber (p.EntityID));
					pJson.Add ("ip", new JSONString (p.IP));
					pJson.Add ("name", new JSONString (p.Name));
					pJson.Add ("online", new JSONBoolean (p.IsOnline));
					pJson.Add ("position", pos);

					pJson.Add ("totalplaytime", new JSONNumber (p.TotalPlayTime));
					pJson.Add ("lastonline",
						new JSONString (p.LastOnline.ToUniversalTime ().ToString ("yyyy-MM-ddTHH:mm:ssZ")));
					pJson.Add ("ping", new JSONNumber (p.IsOnline ? p.ClientInfo.ping : -1));

					JSONBoolean banned = admTools != null ? new JSONBoolean (admTools.IsBanned (kvp.Key, out _, out _)) : new JSONBoolean (false);

					pJson.Add ("banned", banned);

					playerList.Add (pJson);
				}
			}

#if ENABLE_PROFILER
			jsonSerializeSampler.End ();
#endif

			IEnumerable<JSONObject> list = playerList;

			foreach (string key in _req.QueryString.AllKeys) {
				if (!string.IsNullOrEmpty (key) && key.StartsWith ("filter[")) {
					string filterCol = key.Substring (key.IndexOf ('[') + 1);
					filterCol = filterCol.Substring (0, filterCol.Length - 1);
					string filterVal = _req.QueryString.Get (key).Trim ();

					list = ExecuteFilter (list, filterCol, filterVal);
				}
			}

			int totalAfterFilter = list.Count ();

			foreach (string key in _req.QueryString.AllKeys) {
				if (!string.IsNullOrEmpty (key) && key.StartsWith ("sort[")) {
					string sortCol = key.Substring (key.IndexOf ('[') + 1);
					sortCol = sortCol.Substring (0, sortCol.Length - 1);
					string sortVal = _req.QueryString.Get (key);

					list = ExecuteSort (list, sortCol, sortVal == "0");
				}
			}

			list = list.Skip (firstEntry);
			list = list.Take (rowsPerPage);


			JSONArray playersJsResult = new JSONArray ();
			foreach (JSONObject jsO in list) {
				playersJsResult.Add (jsO);
			}

			JSONObject result = new JSONObject ();
			result.Add ("total", new JSONNumber (totalAfterFilter));
			result.Add ("totalUnfiltered", new JSONNumber (playerList.Count));
			result.Add ("firstResult", new JSONNumber (firstEntry));
			result.Add ("players", playersJsResult);

			WriteJSON (_resp, result);
		}

		private IEnumerable<JSONObject> ExecuteFilter (IEnumerable<JSONObject> _list, string _filterCol,
			string _filterVal) {
			if (!_list.Any()) {
				return _list;
			}

			if (_list.First ().ContainsKey (_filterCol)) {
				Type colType = _list.First () [_filterCol].GetType ();
				if (colType == typeof (JSONNumber)) {
					return ExecuteNumberFilter (_list, _filterCol, _filterVal);
				}

				if (colType == typeof (JSONBoolean)) {
					bool value = StringParsers.ParseBool (_filterVal);
					return _list.Where (_line => ((JSONBoolean) _line [_filterCol]).GetBool () == value);
				}

				if (colType == typeof (JSONString)) {
					// regex-match whole ^string$, replace * by .*, ? by .?, + by .+
					_filterVal = _filterVal.Replace ("*", ".*").Replace ("?", ".?").Replace ("+", ".+");
					_filterVal = "^" + _filterVal + "$";

					//Log.Out ("GetPlayerList: Filter on String with Regex '" + _filterVal + "'");
					Regex matcher = new Regex (_filterVal, RegexOptions.IgnoreCase);
					return _list.Where (_line => matcher.IsMatch (((JSONString) _line [_filterCol]).GetString ()));
				}
			}

			return _list;
		}


		private IEnumerable<JSONObject> ExecuteNumberFilter (IEnumerable<JSONObject> _list, string _filterCol,
			string _filterVal) {
			// allow value (exact match), =, ==, >=, >, <=, <
			Match filterMatch = numberFilterMatcher.Match (_filterVal);
			if (filterMatch.Success) {
				double value = StringParsers.ParseDouble (filterMatch.Groups [2].Value);
				NumberMatchType matchType;
				double epsilon = value / 100000;
				switch (filterMatch.Groups [1].Value) {
					case "":
					case "=":
					case "==":
						matchType = NumberMatchType.Equal;
						break;
					case ">":
						matchType = NumberMatchType.Greater;
						break;
					case ">=":
					case "=>":
						matchType = NumberMatchType.GreaterEqual;
						break;
					case "<":
						matchType = NumberMatchType.Lesser;
						break;
					case "<=":
					case "=<":
						matchType = NumberMatchType.LesserEqual;
						break;
					default:
						matchType = NumberMatchType.Equal;
						break;
				}

				return _list.Where (delegate (JSONObject _line) {
					double objVal = ((JSONNumber) _line [_filterCol]).GetDouble ();
					switch (matchType) {
						case NumberMatchType.Greater:
							return objVal > value;
						case NumberMatchType.GreaterEqual:
							return objVal >= value;
						case NumberMatchType.Lesser:
							return objVal < value;
						case NumberMatchType.LesserEqual:
							return objVal <= value;
						case NumberMatchType.Equal:
						default:
							return NearlyEqual (objVal, value, epsilon);
					}
				});
			}

			Log.Out ("GetPlayerList: ignoring invalid filter for number-column '{0}': '{1}'", _filterCol, _filterVal);
			return _list;
		}


		private IEnumerable<JSONObject> ExecuteSort (IEnumerable<JSONObject> _list, string _sortCol, bool _ascending) {
			if (_list.Count () == 0) {
				return _list;
			}

			if (_list.First ().ContainsKey (_sortCol)) {
				Type colType = _list.First () [_sortCol].GetType ();
				if (colType == typeof (JSONNumber)) {
					if (_ascending) {
						return _list.OrderBy (_line => ((JSONNumber) _line [_sortCol]).GetDouble ());
					}

					return _list.OrderByDescending (_line => ((JSONNumber) _line [_sortCol]).GetDouble ());
				}

				if (colType == typeof (JSONBoolean)) {
					if (_ascending) {
						return _list.OrderBy (_line => ((JSONBoolean) _line [_sortCol]).GetBool ());
					}

					return _list.OrderByDescending (_line => ((JSONBoolean) _line [_sortCol]).GetBool ());
				}

				if (_ascending) {
					return _list.OrderBy (_line => _line [_sortCol].ToString ());
				}

				return _list.OrderByDescending (_line => _line [_sortCol].ToString ());
			}

			return _list;
		}


		private bool NearlyEqual (double _a, double _b, double _epsilon) {
			double absA = Math.Abs (_a);
			double absB = Math.Abs (_b);
			double diff = Math.Abs (_a - _b);

			if (_a == _b) {
				return true;
			}

			if (_a == 0 || _b == 0 || diff < double.Epsilon) {
				return diff < _epsilon;
			}

			return diff / (absA + absB) < _epsilon;
		}

		private enum NumberMatchType {
			Equal,
			Greater,
			GreaterEqual,
			Lesser,
			LesserEqual
		}
	}
}