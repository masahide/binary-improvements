using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace AllocsFixes.NetConnections.Servers.Web {
	public static class OpenID {
		private const string STEAM_LOGIN = "https://steamcommunity.com/openid/login";

		private static readonly Regex steamIdUrlMatcher =
			new Regex (@"^https?:\/\/steamcommunity\.com\/openid\/id\/([0-9]{17,18})");

		private static readonly X509Certificate2 caCert =
			new X509Certificate2 (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location) +
			                      "/steam-rootca.cer");

		private static readonly X509Certificate2 caIntermediateCert =
			new X509Certificate2 (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location) +
			                      "/steam-intermediate.cer");

		private const bool verboseSsl = false;
		public static bool debugOpenId;

		static OpenID () {
			for (int i = 0; i < Environment.GetCommandLineArgs ().Length; i++) {
				if (Environment.GetCommandLineArgs () [i].EqualsCaseInsensitive ("-debugopenid")) {
					debugOpenId = true;
				}
			}

			ServicePointManager.ServerCertificateValidationCallback = (_srvPoint, _certificate, _chain, _errors) => {
				if (_errors == SslPolicyErrors.None) {
					if (verboseSsl) {
						Log.Out ("Steam certificate: No error (1)");
					}

					return true;
				}

				X509Chain privateChain = new X509Chain ();
				privateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

				privateChain.ChainPolicy.ExtraStore.Add (caCert);
				privateChain.ChainPolicy.ExtraStore.Add (caIntermediateCert);

				if (privateChain.Build (new X509Certificate2 (_certificate))) {
					// No errors, immediately return
					privateChain.Reset ();
					if (verboseSsl) {
						Log.Out ("Steam certificate: No error (2)");
					}

					return true;
				}

				if (privateChain.ChainStatus.Length == 0) {
					// No errors, immediately return
					privateChain.Reset ();
					if (verboseSsl) {
						Log.Out ("Steam certificate: No error (3)");
					}

					return true;
				}

				// Iterate all chain elements
				foreach (X509ChainElement chainEl in privateChain.ChainElements) {
					if (verboseSsl) {
						Log.Out ("Validating cert: " + chainEl.Certificate.Subject);
					}

					// Iterate all status flags of the current cert
					foreach (X509ChainStatus chainStatus in chainEl.ChainElementStatus) {
						if (verboseSsl) {
							Log.Out ("   Status: " + chainStatus.Status);
						}

						if (chainStatus.Status == X509ChainStatusFlags.NoError) {
							// This status is not an error, skip
							continue;
						}

						if (chainStatus.Status == X509ChainStatusFlags.UntrustedRoot && chainEl.Certificate.Equals (caCert)) {
							// This status is about the cert being an untrusted root certificate but the certificate is one of those we added, ignore
							continue;
						}

						// This status is an error, print information
						Log.Warning ("Steam certificate error: " + chainEl.Certificate.Subject + " ### Error: " +
						             chainStatus.Status);
						privateChain.Reset ();
						return false;
					}
				}

				foreach (X509ChainStatus chainStatus in privateChain.ChainStatus) {
					if (chainStatus.Status != X509ChainStatusFlags.NoError &&
					    chainStatus.Status != X509ChainStatusFlags.UntrustedRoot) {
						Log.Warning ("Steam certificate error: " + chainStatus.Status);
						privateChain.Reset ();
						return false;
					}
				}

				// We didn't find any errors, chain is valid
				privateChain.Reset ();
				if (verboseSsl) {
					Log.Out ("Steam certificate: No error (4)");
				}

				return true;
			};
		}

		public static string GetOpenIdLoginUrl (string _returnHost, string _returnUrl) {
			Dictionary<string, string> queryParams = new Dictionary<string, string> ();

			queryParams.Add ("openid.ns", "http://specs.openid.net/auth/2.0");
			queryParams.Add ("openid.mode", "checkid_setup");
			queryParams.Add ("openid.return_to", _returnUrl);
			queryParams.Add ("openid.realm", _returnHost);
			queryParams.Add ("openid.identity", "http://specs.openid.net/auth/2.0/identifier_select");
			queryParams.Add ("openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select");

			return STEAM_LOGIN + '?' + buildUrlParams (queryParams);
		}

		public static ulong Validate (HttpListenerRequest _req) {
			string mode = getValue (_req, "openid.mode");
			if (mode == "cancel") {
				Log.Warning ("Steam OpenID login canceled");
				return 0;
			}

			if (mode == "error") {
				Log.Warning ("Steam OpenID login error: " + getValue (_req, "openid.error"));
				if (debugOpenId) {
					PrintOpenIdResponse (_req);
				}

				return 0;
			}

			string steamIdString = getValue (_req, "openid.claimed_id");
			ulong steamId;
			Match steamIdMatch = steamIdUrlMatcher.Match (steamIdString);
			if (steamIdMatch.Success) {
				steamId = ulong.Parse (steamIdMatch.Groups [1].Value);
			} else {
				Log.Warning ("Steam OpenID login result did not give a valid SteamID");
				if (debugOpenId) {
					PrintOpenIdResponse (_req);
				}

				return 0;
			}

			Dictionary<string, string> queryParams = new Dictionary<string, string> ();

			queryParams.Add ("openid.ns", "http://specs.openid.net/auth/2.0");

			queryParams.Add ("openid.assoc_handle", getValue (_req, "openid.assoc_handle"));
			queryParams.Add ("openid.signed", getValue (_req, "openid.signed"));
			queryParams.Add ("openid.sig", getValue (_req, "openid.sig"));
			queryParams.Add ("openid.identity", "http://specs.openid.net/auth/2.0/identifier_select");
			queryParams.Add ("openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select");

			string[] signeds = getValue (_req, "openid.signed").Split (',');
			foreach (string s in signeds) {
				queryParams ["openid." + s] = getValue (_req, "openid." + s);
			}

			queryParams.Add ("openid.mode", "check_authentication");

			byte[] postData = Encoding.ASCII.GetBytes (buildUrlParams (queryParams));
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (STEAM_LOGIN);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = postData.Length;
			request.Headers.Add (HttpRequestHeader.AcceptLanguage, "en");
			using (Stream st = request.GetRequestStream ()) {
				st.Write (postData, 0, postData.Length);
			}

			HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
			string responseString;
			using (Stream st = response.GetResponseStream ()) {
				using (StreamReader str = new StreamReader (st)) {
					responseString = str.ReadToEnd ();
				}
			}

			if (responseString.ContainsCaseInsensitive ("is_valid:true")) {
				return steamId;
			}

			Log.Warning ("Steam OpenID login failed: {0}", responseString);
			return 0;
		}

		private static string buildUrlParams (Dictionary<string, string> _queryParams) {
			string[] paramsArr = new string[_queryParams.Count];
			int i = 0;
			foreach (KeyValuePair<string, string> kvp in _queryParams) {
				paramsArr [i++] = kvp.Key + "=" + Uri.EscapeDataString (kvp.Value);
			}

			return string.Join ("&", paramsArr);
		}

		private static string getValue (HttpListenerRequest _req, string _name) {
			NameValueCollection nvc = _req.QueryString;
			if (nvc [_name] == null) {
				throw new MissingMemberException ("OpenID parameter \"" + _name + "\" missing");
			}

			return nvc [_name];
		}

		private static void PrintOpenIdResponse (HttpListenerRequest _req) {
			NameValueCollection nvc = _req.QueryString;
			for (int i = 0; i < nvc.Count; i++) {
				Log.Out ("   " + nvc.GetKey (i) + " = " + nvc [i]);
			}
		}
	}
}