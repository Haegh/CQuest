using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCore {
	public static class NumberAssigner {
		static int _nextNumber = 0;

		public static int GetNextNumber() {
			_nextNumber = (_nextNumber + 1);

			return _nextNumber;
		}
	}
}
