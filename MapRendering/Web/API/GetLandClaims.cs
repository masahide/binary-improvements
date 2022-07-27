using System.Collections.Generic;
using System.Net;
using AllocsFixes.JSON;
using AllocsFixes.PersistentData;

namespace AllocsFixes.NetConnections.Servers.Web.API {
	public class GetLandClaims : WebAPI {
		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			PlatformUserIdentifierAbs requestedUserId = null;
			if (_req.QueryString ["userid"] != null) {
				if (!PlatformUserIdentifierAbs.TryFromCombinedString (_req.QueryString ["userid"], out requestedUserId)) {
					_resp.StatusCode = (int) HttpStatusCode.BadRequest;
					Web.SetResponseTextContent (_resp, "Invalid user id given");
					return;
				}
			}

			// default user, cheap way to avoid 'null reference exception'
			PlatformUserIdentifierAbs userId = _user?.UserId;

			bool bViewAll = WebConnection.CanViewAllClaims (_permissionLevel);

			JSONObject result = new JSONObject ();
			result.Add ("claimsize", new JSONNumber (GamePrefs.GetInt (EnumUtils.Parse<EnumGamePrefs> ("LandClaimSize"))));

			JSONArray claimOwners = new JSONArray ();
			result.Add ("claimowners", claimOwners);

			LandClaimList.OwnerFilter[] ownerFilters = null;
			if (requestedUserId != null || !bViewAll) {
				if (requestedUserId != null && !bViewAll) {
					ownerFilters = new[] {
						LandClaimList.UserIdFilter (userId),
						LandClaimList.UserIdFilter (requestedUserId)
					};
				} else if (!bViewAll) {
					ownerFilters = new[] {LandClaimList.UserIdFilter (userId)};
				} else {
					ownerFilters = new[] {LandClaimList.UserIdFilter (requestedUserId)};
				}
			}

			LandClaimList.PositionFilter[] posFilters = null;

			Dictionary<Player, List<Vector3i>> claims = LandClaimList.GetLandClaims (ownerFilters, posFilters);

			foreach (KeyValuePair<Player, List<Vector3i>> kvp in claims) {
				JSONObject owner = new JSONObject ();
				claimOwners.Add (owner);

				owner.Add ("steamid", new JSONString (kvp.Key.PlatformId.CombinedString));
				owner.Add ("claimactive", new JSONBoolean (kvp.Key.LandProtectionActive));

				if (kvp.Key.Name.Length > 0) {
					owner.Add ("playername", new JSONString (kvp.Key.Name));
				} else {
					owner.Add ("playername", new JSONNull ());
				}

				JSONArray claimsJson = new JSONArray ();
				owner.Add ("claims", claimsJson);

				foreach (Vector3i v in kvp.Value) {
					JSONObject claim = new JSONObject ();
					claim.Add ("x", new JSONNumber (v.x));
					claim.Add ("y", new JSONNumber (v.y));
					claim.Add ("z", new JSONNumber (v.z));

					claimsJson.Add (claim);
				}
			}

			WriteJSON (_resp, result);
		}
	}
}