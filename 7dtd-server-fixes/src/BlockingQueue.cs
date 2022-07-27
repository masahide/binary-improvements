using System.Collections.Generic;
using System.Threading;

namespace AllocsFixes {
	public class BlockingQueue<T> {
		private readonly Queue<T> queue = new Queue<T> ();
		private bool closing;

		public void Enqueue (T _item) {
			lock (queue) {
				queue.Enqueue (_item);
				Monitor.PulseAll (queue);
			}
		}

		public T Dequeue () {
			lock (queue) {
				while (queue.Count == 0) {
					if (closing) {
						return default (T);
					}

					Monitor.Wait (queue);
				}

				return queue.Dequeue ();
			}
		}

		public void Close () {
			lock (queue) {
				closing = true;
				Monitor.PulseAll (queue);
			}
		}
	}
}