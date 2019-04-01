using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class BingoBoardEventArgs {
		public int[] Board;

		public BingoBoardEventArgs(int[] board) {
			Board = board;
		}
	}
}
