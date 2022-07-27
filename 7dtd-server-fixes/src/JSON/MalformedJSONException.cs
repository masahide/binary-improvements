using System;
using System.Runtime.Serialization;

namespace AllocsFixes.JSON {
	public class MalformedJSONException : ApplicationException {
		public MalformedJSONException () {
		}

		public MalformedJSONException (string _message) : base (_message) {
		}

		public MalformedJSONException (string _message, Exception _inner) : base (_message, _inner) {
		}

		protected MalformedJSONException (SerializationInfo _info, StreamingContext _context) : base (_info, _context) {
		}
	}
}