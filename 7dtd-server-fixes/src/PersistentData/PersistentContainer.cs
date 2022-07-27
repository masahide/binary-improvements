using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace AllocsFixes.PersistentData {
	[Serializable]
	public class PersistentContainer {
		private Players players;
		[OptionalField] private Attributes attributes;

		public Players Players {
			get {
				if (players == null) {
					players = new Players ();
				}

				return players;
			}
		}

		public Attributes Attributes {
			get {
				if (attributes == null) {
					attributes = new Attributes ();
				}

				return attributes;
			}
		}

		private static PersistentContainer instance;

		public static PersistentContainer Instance {
			get {
				if (instance == null) {
					instance = new PersistentContainer ();
				}

				return instance;
			}
		}

		private PersistentContainer () {
		}

		public void Save () {
			Stream stream = File.Open (GameIO.GetSaveGameDir () + "/AllocsPeristentData.bin", FileMode.Create);
			BinaryFormatter bFormatter = new BinaryFormatter ();
			bFormatter.Serialize (stream, this);
			stream.Close ();
		}

		public static bool Load () {
			if (!File.Exists (GameIO.GetSaveGameDir () + "/AllocsPeristentData.bin")) {
				return false;
			}

			try {
				PersistentContainer obj;
				Stream stream = File.Open (GameIO.GetSaveGameDir () + "/AllocsPeristentData.bin", FileMode.Open);
				BinaryFormatter bFormatter = new BinaryFormatter ();
				obj = (PersistentContainer) bFormatter.Deserialize (stream);
				stream.Close ();
				instance = obj;
				return true;
			} catch (Exception e) {
				Log.Error ("Exception in PersistentContainer.Load");
				Log.Exception (e);
			}

			return false;
		}
	}
}