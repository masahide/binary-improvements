namespace AllocsFixes.LiveData {
	public class Animals : EntityFilterList<EntityAnimal> {
		public static readonly Animals Instance = new Animals ();

		protected override EntityAnimal predicate (Entity _e) {
			EntityAnimal ea = _e as EntityAnimal;
			if (ea != null && ea.IsAlive ()) {
				return ea;
			}

			return null;
		}
	}
}