using System.Text;

namespace AllocsFixes.JSON {
	public abstract class JSONNode {
		public abstract void ToString (StringBuilder _stringBuilder, bool _prettyPrint = false, int _currentLevel = 0);

		public override string ToString () {
			StringBuilder sb = new StringBuilder ();
			ToString (sb);
			return sb.ToString ();
		}
	}
}