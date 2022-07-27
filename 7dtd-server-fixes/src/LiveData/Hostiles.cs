namespace AllocsFixes.LiveData {
	public class Hostiles : EntityFilterList<EntityEnemy> {
		public static readonly Hostiles Instance = new Hostiles ();

		protected override EntityEnemy predicate (Entity _e) {
			EntityEnemy enemy = _e as EntityEnemy;
			if (enemy != null && enemy.IsAlive ()) {
				return enemy;
			}

			return null;
		}
	}
}