using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AllocsFixes.NetConnections.Servers.Web.Handlers {
	public class ItemIconHandler : PathHandler {
		private readonly Dictionary<string, byte[]> icons = new Dictionary<string, byte[]> ();
		private readonly bool logMissingFiles;

		private bool loaded;

		static ItemIconHandler () {
			Instance = null;
		}

		public ItemIconHandler (bool _logMissingFiles, string _moduleName = null) : base (_moduleName) {
			logMissingFiles = _logMissingFiles;
			Instance = this;
		}

		public static ItemIconHandler Instance { get; private set; }

		public override void HandleRequest (HttpListenerRequest _req, HttpListenerResponse _resp, WebConnection _user,
			int _permissionLevel) {
			if (!loaded) {
				_resp.StatusCode = (int) HttpStatusCode.InternalServerError;
				Log.Out ("Web:IconHandler: Icons not loaded");
				return;
			}

			string requestFileName = _req.Url.AbsolutePath.Remove (0, urlBasePath.Length);
			requestFileName = requestFileName.Remove (requestFileName.LastIndexOf ('.'));

			if (icons.ContainsKey (requestFileName) && _req.Url.AbsolutePath.EndsWith (".png", StringComparison.OrdinalIgnoreCase)) {
				_resp.ContentType = MimeType.GetMimeType (".png");

				byte[] itemIconData = icons [requestFileName];

				_resp.ContentLength64 = itemIconData.Length;
				_resp.OutputStream.Write (itemIconData, 0, itemIconData.Length);
			} else {
				_resp.StatusCode = (int) HttpStatusCode.NotFound;
				if (logMissingFiles) {
					Log.Out ("Web:IconHandler:FileNotFound: \"" + _req.Url.AbsolutePath + "\" ");
				}
			}
		}

		public bool LoadIcons () {
			
			lock (icons) {
				if (loaded) {
					return true;
				}

				MicroStopwatch microStopwatch = new MicroStopwatch ();

				// Get list of used tints for all items
				Dictionary<string, List<Color>> tintedIcons = new Dictionary<string, List<Color>> ();
				foreach (ItemClass ic in ItemClass.list) {
					if (ic != null) {
						Color tintColor = ic.GetIconTint ();
						if (tintColor != Color.white) {
							string name = ic.GetIconName ();
							if (!tintedIcons.ContainsKey (name)) {
								tintedIcons.Add (name, new List<Color> ());
							}

							List<Color> list = tintedIcons [name];
							list.Add (tintColor);
						}
					}
				}

				try {
					loadIconsFromFolder (GameIO.GetGameDir ("Data/ItemIcons"), tintedIcons);
				} catch (Exception e) {
					Log.Error ("Failed loading icons from base game");
					Log.Exception (e);
				}

				// Load icons from mods
				foreach (Mod mod in ModManager.GetLoadedMods ()) {
					try {
						string modIconsPath = mod.Path + "/ItemIcons";
						loadIconsFromFolder (modIconsPath, tintedIcons);
					} catch (Exception e) {
						Log.Error ("Failed loading icons from mod " + mod.ModInfo.Name.Value);
						Log.Exception (e);
					}
				}

				loaded = true;
				Log.Out ("Web:IconHandler: Icons loaded - {0} ms", microStopwatch.ElapsedMilliseconds);

				return true;
			}
		}

		private void loadIconsFromFolder (string _path, Dictionary<string, List<Color>> _tintedIcons) {
			if (Directory.Exists (_path)) {
				foreach (string file in Directory.GetFiles (_path)) {
					try {
						if (file.EndsWith (".png", StringComparison.OrdinalIgnoreCase)) {
							string name = Path.GetFileNameWithoutExtension (file);
							Texture2D tex = new Texture2D (1, 1, TextureFormat.ARGB32, false);
							if (tex.LoadImage (File.ReadAllBytes (file))) {
								AddIcon (name, tex, _tintedIcons);

								Object.Destroy (tex);
							}
						}
					} catch (Exception e) {
						Log.Exception (e);
					}
				}
			}
		}

		private void AddIcon (string _name, Texture2D _tex, Dictionary<string, List<Color>> _tintedIcons) {
			icons [_name + "__FFFFFF"] = _tex.EncodeToPNG ();

			if (_tintedIcons.ContainsKey (_name)) {
				foreach (Color c in _tintedIcons [_name]) {
					string tintedName = _name + "__" + AllocsUtils.ColorToHex (c);
					if (!icons.ContainsKey (tintedName)) {
						Texture2D tintedTex = new Texture2D (_tex.width, _tex.height, TextureFormat.ARGB32, false);

						for (int x = 0; x < _tex.width; x++) {
							for (int y = 0; y < _tex.height; y++) {
								tintedTex.SetPixel (x, y, _tex.GetPixel (x, y) * c);
							}
						}

						icons [tintedName] = tintedTex.EncodeToPNG ();

						Object.Destroy (tintedTex);
					}
				}
			}
		}
	}
}