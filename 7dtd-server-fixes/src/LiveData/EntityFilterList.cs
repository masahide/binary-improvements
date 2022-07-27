using System;
using System.Collections.Generic;

namespace AllocsFixes.LiveData {
	public abstract class EntityFilterList<T> where T : Entity {
		public void Get (List<T> _list) {
			_list.Clear ();
			try {
				List<Entity> entities = GameManager.Instance.World.Entities.list;
				for (int i = 0; i < entities.Count; i++) {
					Entity entity = entities [i];

					T element = predicate (entity);
					if (element != null) {
						_list.Add (element);
					}
				}
			} catch (Exception e) {
				Log.Exception (e);
			}
		}

		public int GetCount () {
			int count = 0;
			try {
				List<Entity> entities = GameManager.Instance.World.Entities.list;
				for (int i = 0; i < entities.Count; i++) {
					Entity entity = entities [i];

					if (predicate (entity) != null) {
						count++;
					}
				}
			} catch (Exception e) {
				Log.Exception (e);
			}

			return count;
		}

		protected abstract T predicate (Entity _e);
	}
}